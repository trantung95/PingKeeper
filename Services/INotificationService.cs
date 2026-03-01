using PingKeeper.Models;

namespace PingKeeper.Services;

public interface INotificationService
{
    Task NotifyServiceDownAsync(ServiceState state, CancellationToken ct);
    Task NotifyServiceRecoveredAsync(ServiceState state, CancellationToken ct);
}
