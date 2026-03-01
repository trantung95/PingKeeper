namespace PingKeeper.Models;

/// <summary>
/// Tracks consecutive failures and up/down state for a single endpoint.
/// Uses threshold-based detection to determine when a service is down.
/// Notifications fire only on state transitions (up→down, down→up).
/// Guards against repeated alerts by ignoring steady-state conditions.
/// </summary>
public sealed class ServiceState
{
    public string ServiceName { get; }
    public string ServiceUrl { get; }
    public int ConsecutiveFailures { get; private set; }
    public bool IsDown { get; private set; }
    public string? LastErrorMessage { get; private set; }

    public ServiceState(string serviceName, string serviceUrl)
    {
        ServiceName = serviceName;
        ServiceUrl = serviceUrl;
    }

    /// <summary>
    /// Records a successful ping. Returns true if this is a recovery (was down, now up).
    /// </summary>
    public bool RecordSuccess()
    {
        ConsecutiveFailures = 0;
        LastErrorMessage = null;

        if (IsDown)
        {
            IsDown = false;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Records a failed ping. Returns true if the failure threshold was just crossed
    /// (transitioned from up to down).
    /// </summary>
    public bool RecordFailure(string errorMessage, int threshold)
    {
        ConsecutiveFailures++;
        LastErrorMessage = errorMessage;

        if (!IsDown && ConsecutiveFailures >= threshold)
        {
            IsDown = true;
            return true;
        }

        return false;
    }
}
