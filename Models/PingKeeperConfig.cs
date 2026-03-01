namespace PingKeeper.Models;

/// <summary>
/// Root configuration for the PingKeeper service.
/// Bound from the "PingKeeper" section in appsettings.json.
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

    /// <summary>List of endpoints to ping.</summary>
    public List<ServiceEndpoint> Endpoints { get; set; } = [];
}
