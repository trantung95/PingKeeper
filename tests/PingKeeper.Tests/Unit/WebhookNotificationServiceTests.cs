using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PingKeeper.Models;
using PingKeeper.Services;
using PingKeeper.Tests.Helpers;

namespace PingKeeper.Tests.Unit;

[Trait("Category", "Unit")]
public class WebhookNotificationServiceTests
{
    private readonly Mock<IHttpClientFactory> _factoryMock = new();
    private readonly Mock<IOptionsMonitor<WebhookConfig>> _optionsMock = new();

    private WebhookNotificationService CreateSut(MockHttpMessageHandler handler, WebhookConfig config)
    {
        var httpClient = new HttpClient(handler);
        _factoryMock.Setup(f => f.CreateClient("Webhook")).Returns(httpClient);
        _optionsMock.Setup(o => o.CurrentValue).Returns(config);

        return new WebhookNotificationService(
            _factoryMock.Object,
            _optionsMock.Object,
            NullLogger<WebhookNotificationService>.Instance);
    }

    [Fact]
    public async Task NotifyServiceDownAsync_SendsPost_WithCorrectPayload()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var config = new WebhookConfig { Url = "http://webhook.test/hook" };
        var sut = CreateSut(handler, config);

        var state = new ServiceState("MyApi", "http://api.test/health");
        state.RecordFailure("HTTP 503 Service Unavailable", threshold: 1);

        await sut.NotifyServiceDownAsync(state, CancellationToken.None);

        handler.SentRequests.Should().HaveCount(1);
        var request = handler.SentRequests[0];
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri!.ToString().Should().Be("http://webhook.test/hook");

        var body = await request.Content!.ReadAsStringAsync();
        body.Should().Contain("\"serviceName\":\"MyApi\"");
        body.Should().Contain("\"status\":\"Down\"");
    }

    [Fact]
    public async Task NotifyServiceDownAsync_SkipsWhenNoUrlConfigured()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var config = new WebhookConfig { Url = "" };
        var sut = CreateSut(handler, config);

        var state = new ServiceState("MyApi", "http://api.test");
        state.RecordFailure("err", threshold: 1);

        await sut.NotifyServiceDownAsync(state, CancellationToken.None);

        handler.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task NotifyServiceDownAsync_SkipsWhenUrlIsNull()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var config = new WebhookConfig { Url = null };
        var sut = CreateSut(handler, config);

        var state = new ServiceState("MyApi", "http://api.test");

        await sut.NotifyServiceDownAsync(state, CancellationToken.None);

        handler.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task NotifyServiceDownAsync_DoesNotThrow_WhenWebhookFails()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError);
        var config = new WebhookConfig { Url = "http://webhook.test/hook" };
        var sut = CreateSut(handler, config);

        var state = new ServiceState("MyApi", "http://api.test");
        state.RecordFailure("err", threshold: 1);

        var act = () => sut.NotifyServiceDownAsync(state, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task NotifyServiceDownAsync_DoesNotThrow_WhenHttpException()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("connection refused"));
        var config = new WebhookConfig { Url = "http://webhook.test/hook" };
        var sut = CreateSut(handler, config);

        var state = new ServiceState("MyApi", "http://api.test");

        var act = () => sut.NotifyServiceDownAsync(state, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task NotifyServiceRecoveredAsync_SendsPost_WhenNotifyOnRecoveryTrue()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var config = new WebhookConfig { Url = "http://webhook.test/hook", NotifyOnRecovery = true };
        var sut = CreateSut(handler, config);

        var state = new ServiceState("MyApi", "http://api.test");

        await sut.NotifyServiceRecoveredAsync(state, CancellationToken.None);

        handler.SentRequests.Should().HaveCount(1);
        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        body.Should().Contain("\"status\":\"Recovered\"");
    }

    [Fact]
    public async Task NotifyServiceRecoveredAsync_Skips_WhenNotifyOnRecoveryFalse()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var config = new WebhookConfig { Url = "http://webhook.test/hook", NotifyOnRecovery = false };
        var sut = CreateSut(handler, config);

        var state = new ServiceState("MyApi", "http://api.test");

        await sut.NotifyServiceRecoveredAsync(state, CancellationToken.None);

        handler.SentRequests.Should().BeEmpty();
    }
}
