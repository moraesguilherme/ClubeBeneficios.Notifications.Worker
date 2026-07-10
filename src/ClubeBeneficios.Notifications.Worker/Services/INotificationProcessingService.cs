namespace ClubeBeneficios.Notifications.Worker.Services;

public interface INotificationProcessingService
{
    Task<int> ReleaseExpiredLocksAsync(CancellationToken cancellationToken = default);

    Task<int> ProcessPendingAsync(
        int batchSize,
        int lockMinutes,
        int maxItemsPerCycle,
        CancellationToken cancellationToken = default);
}
