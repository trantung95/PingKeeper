using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PingKeeper.Models;
using PingKeeper.Services;
using PingKeeper.Tests.Helpers;

namespace PingKeeper.Tests.Unit;

[Trait("Category", "Unit")]
public class PingWorkerTests
{
    private readonly ServiceStateTracker _stateTracker = new();
    private readonly Mock<INotificationService> _notificationMock = new();

    private PingWorker CreateWorker(MockHttpMessageHandler handler, PingKeeperConfig config)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("Ping"))
            .Returns(() => new HttpClient(handler, disposeHandler: false));

        var optionsMonitor = new Mock<IOptionsMonitor<PingKeeperConfig>>();
        optionsMonitor.Setup(o => o.CurrentValue).Returns(config);

        return new PingWorker(
            factory.Object,
            optionsMonitor.Object,
            _stateTracker,
            _notificationMock.Object,
            NullLogger<PingWorker>.Instance);
    }

    [Fact]
    public async Task PingAllEndpoints_Success_StateRemainsUp()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var endpoint = new ServiceEndpoint { Name = "Test", Url = "http://test.local" };
        var config = new PingKeeperConfig { Endpoints = [endpoint] };
        var worker = CreateWorker(handler, config);

        await worker.PingAllEndpointsAsync(CancellationToken.None);

        var state = _stateTracker.GetOrCreate(endpoint);
        state.IsDown.Should().BeFalse();
        state.ConsecutiveFailures.Should().Be(0);
        _notificationMock.Verify(
            n => n.NotifyServiceDownAsync(It.IsAny<ServiceState>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PingAllEndpoints_FailureBelowThreshold_NoNotification()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.ServiceUnavailable);
        var endpoint = new ServiceEndpoint { Name = "Test", Url = "http://test.local" };
        var config = new PingKeeperConfig
        {
            Endpoints = [endpoint],
            ConsecutiveFailureThreshold = 3
        };
        var worker = CreateWorker(handler, config);

        await worker.PingAllEndpointsAsync(CancellationToken.None);
        await worker.PingAllEndpointsAsync(CancellationToken.None);

        var state = _stateTracker.GetOrCreate(endpoint);
        state.IsDown.Should().BeFalse();
        state.ConsecutiveFailures.Should().Be(2);
        _notificationMock.Verify(
            n => n.NotifyServiceDownAsync(It.IsAny<ServiceState>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PingAllEndpoints_FailureAtThreshold_NotifiesDown()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.ServiceUnavailable);
        var endpoint = new ServiceEndpoint { Name = "Test", Url = "http://test.local" };
        var config = new PingKeeperConfig
        {
            Endpoints = [endpoint],
            ConsecutiveFailureThreshold = 3
        };
        var worker = CreateWorker(handler, config);

        for (int i = 0; i < 3; i++)
            await worker.PingAllEndpointsAsync(CancellationToken.None);

        var state = _stateTracker.GetOrCreate(endpoint);
        state.IsDown.Should().BeTrue();
        _notificationMock.Verify(
            n => n.NotifyServiceDownAsync(It.IsAny<ServiceState>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PingAllEndpoints_FailureAboveThreshold_NotifiesOnlyOnce()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.ServiceUnavailable);
        var endpoint = new ServiceEndpoint { Name = "Test", Url = "http://test.local" };
        var config = new PingKeeperConfig
        {
            Endpoints = [endpoint],
            ConsecutiveFailureThreshold = 3
        };
        var worker = CreateWorker(handler, config);

        for (int i = 0; i < 6; i++)
            await worker.PingAllEndpointsAsync(CancellationToken.None);

        _notificationMock.Verify(
            n => n.NotifyServiceDownAsync(It.IsAny<ServiceState>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PingAllEndpoints_Recovery_NotifiesRecovered()
    {
        var callCount = 0;
        var handler = new MockHttpMessageHandler((_, _) =>
        {
            callCount++;
            var statusCode = callCount <= 3 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(statusCode));
        });

        var endpoint = new ServiceEndpoint { Name = "Test", Url = "http://test.local" };
        var config = new PingKeeperConfig
        {
            Endpoints = [endpoint],
            ConsecutiveFailureThreshold = 3
        };
        var worker = CreateWorker(handler, config);

        // 3 failures → down
        for (int i = 0; i < 3; i++)
            await worker.PingAllEndpointsAsync(CancellationToken.None);

        // 1 success → recovery
        await worker.PingAllEndpointsAsync(CancellationToken.None);

        var state = _stateTracker.GetOrCreate(endpoint);
        state.IsDown.Should().BeFalse();
        _notificationMock.Verify(
            n => n.NotifyServiceRecoveredAsync(It.IsAny<ServiceState>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PingAllEndpoints_ConnectionError_RecordsFailure()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));

        var endpoint = new ServiceEndpoint { Name = "Test", Url = "http://test.local" };
        var config = new PingKeeperConfig { Endpoints = [endpoint], ConsecutiveFailureThreshold = 3 };
        var worker = CreateWorker(handler, config);

        await worker.PingAllEndpointsAsync(CancellationToken.None);

        var state = _stateTracker.GetOrCreate(endpoint);
        state.ConsecutiveFailures.Should().Be(1);
        state.LastErrorMessage.Should().Contain("Connection refused");
    }

    [Fact]
    public async Task PingAllEndpoints_NoEndpoints_DoesNotThrow()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var config = new PingKeeperConfig { Endpoints = [] };
        var worker = CreateWorker(handler, config);

        var act = () => worker.PingAllEndpointsAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PingAllEndpoints_MultipleEndpoints_PingsAll()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var config = new PingKeeperConfig
        {
            Endpoints =
            [
                new ServiceEndpoint { Name = "A", Url = "http://a.local" },
                new ServiceEndpoint { Name = "B", Url = "http://b.local" },
                new ServiceEndpoint { Name = "C", Url = "http://c.local" }
            ]
        };
        var worker = CreateWorker(handler, config);

        await worker.PingAllEndpointsAsync(CancellationToken.None);

        handler.SentRequests.Should().HaveCount(3);
    }

    [Fact]
    public async Task PingAllEndpoints_OneEndpointFails_OthersContinue()
    {
        var handler = new MockHttpMessageHandler((req, _) =>
        {
            var status = req.RequestUri!.Host == "fail.local"
                ? HttpStatusCode.ServiceUnavailable
                : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(status));
        });

        var config = new PingKeeperConfig
        {
            Endpoints =
            [
                new ServiceEndpoint { Name = "OK", Url = "http://ok.local" },
                new ServiceEndpoint { Name = "Fail", Url = "http://fail.local" },
                new ServiceEndpoint { Name = "AlsoOK", Url = "http://alsook.local" }
            ]
        };
        var worker = CreateWorker(handler, config);

        await worker.PingAllEndpointsAsync(CancellationToken.None);

        handler.SentRequests.Should().HaveCount(3);
        _stateTracker.GetOrCreate(config.Endpoints[0]).ConsecutiveFailures.Should().Be(0);
        _stateTracker.GetOrCreate(config.Endpoints[1]).ConsecutiveFailures.Should().Be(1);
        _stateTracker.GetOrCreate(config.Endpoints[2]).ConsecutiveFailures.Should().Be(0);
    }
}
