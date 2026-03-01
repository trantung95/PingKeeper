using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PingKeeper.Models;
using PingKeeper.Services;

namespace PingKeeper.Tests.E2E;

[Trait("Category", "E2E")]
public class PublicServerPingTests
{
    private static IHttpClientFactory CreateRealHttpClientFactory()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("Ping");
        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
    }

    private static (PingWorker worker, ServiceStateTracker tracker, Mock<INotificationService> notification)
        CreateServices(PingKeeperConfig config)
    {
        var factory = CreateRealHttpClientFactory();
        var stateTracker = new ServiceStateTracker();
        var notificationMock = new Mock<INotificationService>();
        var optionsMonitor = new Mock<IOptionsMonitor<PingKeeperConfig>>();
        optionsMonitor.Setup(o => o.CurrentValue).Returns(config);

        var worker = new PingWorker(
            factory,
            optionsMonitor.Object,
            stateTracker,
            notificationMock.Object,
            NullLogger<PingWorker>.Instance);

        return (worker, stateTracker, notificationMock);
    }

    [Fact]
    public async Task Ping_Google_Succeeds()
    {
        var endpoint = new ServiceEndpoint { Name = "Google", Url = "https://www.google.com" };
        var config = new PingKeeperConfig { Endpoints = [endpoint], TimeoutSeconds = 10 };
        var (worker, tracker, notification) = CreateServices(config);

        await worker.PingAllEndpointsAsync(CancellationToken.None);

        var state = tracker.GetOrCreate(endpoint);
        state.IsDown.Should().BeFalse();
        state.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public async Task Ping_CloudflareHealth_Succeeds()
    {
        var endpoint = new ServiceEndpoint { Name = "Cloudflare", Url = "https://1.1.1.1" };
        var config = new PingKeeperConfig { Endpoints = [endpoint], TimeoutSeconds = 10 };
        var (worker, tracker, notification) = CreateServices(config);

        await worker.PingAllEndpointsAsync(CancellationToken.None);

        var state = tracker.GetOrCreate(endpoint);
        state.IsDown.Should().BeFalse();
        state.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public async Task Ping_NonExistentDomain_RecordsFailure()
    {
        var endpoint = new ServiceEndpoint
        {
            Name = "NonExistent",
            Url = "http://this-domain-does-not-exist-ping-keeper-test.invalid"
        };
        var config = new PingKeeperConfig
        {
            Endpoints = [endpoint],
            TimeoutSeconds = 5,
            ConsecutiveFailureThreshold = 3
        };
        var (worker, tracker, notification) = CreateServices(config);

        await worker.PingAllEndpointsAsync(CancellationToken.None);

        var state = tracker.GetOrCreate(endpoint);
        state.ConsecutiveFailures.Should().Be(1);
        state.LastErrorMessage.Should().Contain("Connection error");
    }

    [Fact]
    public async Task Ping_NonExistentDomain_GoesDown_AfterThreshold()
    {
        var endpoint = new ServiceEndpoint
        {
            Name = "NonExistent",
            Url = "http://this-domain-does-not-exist-ping-keeper-test.invalid"
        };
        var config = new PingKeeperConfig
        {
            Endpoints = [endpoint],
            TimeoutSeconds = 5,
            ConsecutiveFailureThreshold = 3
        };
        var (worker, tracker, notification) = CreateServices(config);

        for (int i = 0; i < 3; i++)
            await worker.PingAllEndpointsAsync(CancellationToken.None);

        var state = tracker.GetOrCreate(endpoint);
        state.IsDown.Should().BeTrue();
        state.ConsecutiveFailures.Should().Be(3);
        notification.Verify(
            n => n.NotifyServiceDownAsync(It.IsAny<ServiceState>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Ping_MultiplePublicServers_AllSucceed()
    {
        var config = new PingKeeperConfig
        {
            Endpoints =
            [
                new ServiceEndpoint { Name = "Google", Url = "https://www.google.com" },
                new ServiceEndpoint { Name = "Cloudflare", Url = "https://1.1.1.1" },
                new ServiceEndpoint { Name = "Microsoft", Url = "https://www.microsoft.com" }
            ],
            TimeoutSeconds = 10
        };
        var (worker, tracker, notification) = CreateServices(config);

        await worker.PingAllEndpointsAsync(CancellationToken.None);

        foreach (var endpoint in config.Endpoints)
        {
            var state = tracker.GetOrCreate(endpoint);
            state.IsDown.Should().BeFalse($"{endpoint.Name} should be reachable");
            state.ConsecutiveFailures.Should().Be(0);
        }

        notification.Verify(
            n => n.NotifyServiceDownAsync(It.IsAny<ServiceState>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
