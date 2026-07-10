using ClubeBeneficios.Notifications.Worker.Models;

namespace ClubeBeneficios.Notifications.Worker.Infrastructure.Repositories;

public interface INotificationOutboxRepository
{
    Task<int> ReleaseExpiredLocksAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationMessage>> ClaimBatchAsync(
        int batchSize,
        int lockMinutes,
        CancellationToken cancellationToken = default);

    Task MarkSentAsync(
        Guid notificationId,
        Guid lockId,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid notificationId,
        Guid lockId,
        string errorMessage,
        CancellationToken cancellationToken = default);
}
