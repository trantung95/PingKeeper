using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PingKeeper.Models;

namespace PingKeeper.Services;

public sealed class WebhookNotificationService : INotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<WebhookConfig> _options;
    private readonly ILogger<WebhookNotificationService> _logger;

    public WebhookNotificationService(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<WebhookConfig> options,
        ILogger<WebhookNotificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task NotifyServiceDownAsync(ServiceState state, CancellationToken ct)
    {
        var payload = new WebhookPayload(
            state.ServiceName,
            state.ServiceUrl,
            "Down",
            state.LastErrorMessage,
            state.ConsecutiveFailures,
            DateTimeOffset.UtcNow);

        await SendWebhookAsync(payload, ct);
    }

    public async Task NotifyServiceRecoveredAsync(ServiceState state, CancellationToken ct)
    {
        if (!_options.CurrentValue.NotifyOnRecovery)
            return;

        var payload = new WebhookPayload(
            state.ServiceName,
            state.ServiceUrl,
            "Recovered",
            null,
            0,
            DateTimeOffset.UtcNow);

        await SendWebhookAsync(payload, ct);
    }

    private async Task SendWebhookAsync(WebhookPayload payload, CancellationToken ct)
    {
        var webhookUrl = _options.CurrentValue.Url;

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogWarning("Webhook URL is not configured; skipping notification for {Service}", payload.ServiceName);
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("Webhook");
            var response = await client.PostAsJsonAsync(webhookUrl, payload, ct);

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Webhook POST to {Url} returned {StatusCode}", webhookUrl, response.StatusCode);
            else
                _logger.LogInformation("Webhook notification sent for {Service}: {Status}", payload.ServiceName, payload.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook notification for {Service}", payload.ServiceName);
        }
    }
}
