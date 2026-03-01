namespace PingKeeper.Models;

/// <summary>
/// JSON payload sent to the webhook URL on service state transitions.
/// </summary>
public sealed record WebhookPayload(
    string ServiceName,
    string ServiceUrl,
    string Status,
    string? ErrorMessage,
    int ConsecutiveFailures,
    DateTimeOffset Timestamp
);
