namespace PingKeeper.Models;

/// <summary>
/// Timing and delivery settings for webhook notifications.
/// Read from the "Webhook" section in appsettings.json.
/// An empty or null URL disables webhook delivery entirely.
/// Notification on recovery is controlled by the NotifyOnRecovery flag.
/// </summary>
public sealed class WebhookConfig
{
    public const string SectionName = "Webhook";

    /// <summary>URL to POST webhook notifications to. Empty or null disables webhooks.</summary>
    public string? Url { get; set; }

    /// <summary>HTTP request timeout for webhook calls in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>Send a notification when a service recovers after being down.</summary>
    public bool NotifyOnRecovery { get; set; } = true;
}
