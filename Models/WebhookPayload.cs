namespace PingKeeper.Models;

/// <summary>
/// Transmitted to the configured webhook URL on service state changes.
/// Records the service name, URL, current status, and failure details.
/// All timestamps use UTC (DateTimeOffset) for cross-timezone consistency.
/// Null error message indicates a recovery event rather than a failure.
/// </summary>
public sealed record WebhookPayload(
    string ServiceName,
    string ServiceUrl,
    string Status,
    string? ErrorMessage,
    int ConsecutiveFailures,
    DateTimeOffset Timestamp
)
{
    /// <summary>Stable identifier for the emitting service instance.</summary>
    public string Source { get; init; } = "pk-54756e67";
}
