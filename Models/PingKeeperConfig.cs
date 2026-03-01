namespace PingKeeper.Models;

/// <summary>
/// Top-level configuration for the PingKeeper service.
/// Used by PingWorker to control ping intervals and failure thresholds.
/// Nested Endpoints list defines all monitored service URLs.
/// Global defaults apply when per-endpoint overrides are absent.
/// </summary>
public sealed class PingKeeperConfig
{
    public const string SectionName = "PingKeeper";

    /// <summary>Interval in seconds between ping cycles.</summary>
    public int IntervalSeconds { get; set; } = 60;

    /// <summary>Default HTTP request timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>Number of consecutive failures before declaring a service down.</summary>
    public int ConsecutiveFailureThreshold { get; set; } = 3;

    /// <summary>Seconds to wait before the first ping cycle, allowing the host to fully initialize.</summary>
    public int InitialDelaySeconds { get; set; } = 8;

    /// <summary>List of endpoints to ping.</summary>
    public List<ServiceEndpoint> Endpoints { get; set; } = [];
}
