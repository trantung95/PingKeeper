namespace PingKeeper.Models;

/// <summary>
/// A single endpoint to monitor.
/// </summary>
public sealed class ServiceEndpoint
{
    /// <summary>Friendly name for logging and notifications.</summary>
    public required string Name { get; set; }

    /// <summary>URL to send HTTP GET requests to.</summary>
    public required string Url { get; set; }

    /// <summary>Per-endpoint timeout override in seconds. Uses global default if null.</summary>
    public int? TimeoutSeconds { get; set; }
}
