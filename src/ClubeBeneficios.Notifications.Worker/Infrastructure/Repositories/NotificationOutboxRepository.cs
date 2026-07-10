using System.Data;
using ClubeBeneficios.Notifications.Worker.Infrastructure.Database;
using ClubeBeneficios.Notifications.Worker.Models;
using Dapper;

namespace ClubeBeneficios.Notifications.Worker.Infrastructure.Repositories;

public class NotificationOutboxRepository : INotificationOutboxRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<NotificationOutboxRepository> _logger;

    public NotificationOutboxRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<NotificationOutboxRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<int> ReleaseExpiredLocksAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteAsync(new CommandDefinition(
            "dbo.usp_notification_release_expired_locks",
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<NotificationMessage>> ClaimBatchAsync(
        int batchSize,
        int lockMinutes,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("BatchSize", batchSize);
        parameters.Add("LockMinutes", lockMinutes);

        var items = await connection.QueryAsync<NotificationMessage>(new CommandDefinition(
            "dbo.usp_notification_claim_batch",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return items.ToList();
    }

    public async Task MarkSentAsync(
        Guid notificationId,
        string? providerMessageId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("NotificationId", notificationId);
        parameters.Add("ProviderMessageId", providerMessageId);

        await connection.ExecuteAsync(new CommandDefinition(
            "dbo.usp_notification_mark_sent",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
    }

    public async Task MarkFailedAsync(
        Guid notificationId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("NotificationId", notificationId);
        parameters.Add("ErrorMessage", errorMessage);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "dbo.usp_notification_mark_failed",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao marcar notificaÃ§Ã£o {NotificationId} como failed. Erro original: {ErrorMessage}",
                notificationId,
                errorMessage);

            throw;
        }
    }
}
