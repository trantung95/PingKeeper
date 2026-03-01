using PingKeeper.Models;

namespace PingKeeper.Services;

/// <summary>
/// Maintains per-endpoint state (consecutive failures, up/down status).
/// Registered as singleton — only one PingWorker uses it.
/// </summary>
public sealed class ServiceStateTracker
{
    private readonly Dictionary<string, ServiceState> _states = new();

    public ServiceState GetOrCreate(ServiceEndpoint endpoint)
    {
        if (!_states.TryGetValue(endpoint.Url, out var state))
        {
            state = new ServiceState(endpoint.Name, endpoint.Url);
            _states[endpoint.Url] = state;
        }

        return state;
    }
}
