using ClubeBeneficios.Notifications.Worker.Infrastructure.Email;
using ClubeBeneficios.Notifications.Worker.Infrastructure.Repositories;
using ClubeBeneficios.Notifications.Worker.Models;

namespace ClubeBeneficios.Notifications.Worker.Services;

public class NotificationProcessingService : INotificationProcessingService
{
    private readonly INotificationOutboxRepository _repository;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<NotificationProcessingService> _logger;

    public NotificationProcessingService(
        INotificationOutboxRepository repository,
        ITemplateRenderer templateRenderer,
        IEmailSender emailSender,
        ILogger<NotificationProcessingService> logger)
    {
        _repository = repository;
        _templateRenderer = templateRenderer;
        _emailSender = emailSender;
        _logger = logger;
    }

    public Task<int> ReleaseExpiredLocksAsync(CancellationToken cancellationToken = default)
        => _repository.ReleaseExpiredLocksAsync(cancellationToken);

    public async Task<int> ProcessPendingAsync(
        int batchSize,
        int lockMinutes,
        int maxItemsPerCycle,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "BatchSize deve ser maior que zero.");
        }

        if (lockMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lockMinutes), "LockMinutes deve ser maior que zero.");
        }

        if (maxItemsPerCycle <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxItemsPerCycle), "MaxItemsPerCycle deve ser maior que zero.");
        }

        var processedCount = 0;

        while (!cancellationToken.IsCancellationRequested && processedCount < maxItemsPerCycle)
        {
            var remaining = maxItemsPerCycle - processedCount;
            var currentBatchSize = Math.Min(batchSize, remaining);

            var notifications = await _repository.ClaimBatchAsync(
                currentBatchSize,
                lockMinutes,
                cancellationToken);

            if (notifications.Count == 0)
            {
                break;
            }

            foreach (var notification in notifications)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await ProcessSingleAsync(notification, cancellationToken);

                processedCount++;

                if (processedCount >= maxItemsPerCycle)
                {
                    break;
                }
            }
        }

        return processedCount;
    }

    private async Task ProcessSingleAsync(
        NotificationMessage notification,
        CancellationToken cancellationToken)
    {
        if (!notification.LockId.HasValue || notification.LockId.Value == Guid.Empty)
        {
            _logger.LogError(
                "NotificaÃ§Ã£o {NotificationId} foi reivindicada sem LockId. Verifique o retorno da procedure usp_notification_claim_batch.",
                notification.Id);

            return;
        }

        try
        {
            _logger.LogInformation(
                "Processando notificaÃ§Ã£o {NotificationId}. Module={Module}, EventType={EventType}, Recipient={RecipientEmail}, TemplateKey={TemplateKey}",
                notification.Id,
                notification.Module,
                notification.EventType,
                notification.RecipientEmail,
                notification.TemplateKey);

            var rendered = _templateRenderer.Render(notification);

            var emailMessage = new EmailMessage
            {
                ToEmail = notification.RecipientEmail,
                ToName = notification.RecipientName,
                Subject = rendered.Subject,
                BodyHtml = rendered.BodyHtml,
                BodyText = rendered.BodyText
            };

            var sendResult = await _emailSender.SendAsync(emailMessage, cancellationToken);

            if (sendResult.Success)
            {
                await _repository.MarkSentAsync(
                    notification.Id,
                    notification.LockId.Value,
                    cancellationToken);

                _logger.LogInformation(
                    "NotificaÃ§Ã£o {NotificationId} enviada e marcada como sent com sucesso.",
                    notification.Id);

                return;
            }

            var errorMessage = string.IsNullOrWhiteSpace(sendResult.ErrorMessage)
                ? "Falha desconhecida no envio de e-mail."
                : sendResult.ErrorMessage;

            await _repository.MarkFailedAsync(
                notification.Id,
                notification.LockId.Value,
                errorMessage,
                cancellationToken);

            _logger.LogWarning(
                "Falha ao enviar notificaÃ§Ã£o {NotificationId}. Erro: {ErrorMessage}",
                notification.Id,
                errorMessage);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var errorMessage = ex.Message;

            try
            {
                await _repository.MarkFailedAsync(
                    notification.Id,
                    notification.LockId.Value,
                    errorMessage,
                    cancellationToken);
            }
            catch (Exception markFailedException)
            {
                _logger.LogError(
                    markFailedException,
                    "Falha adicional ao marcar notificaÃ§Ã£o {NotificationId} como failed.",
                    notification.Id);
            }

            _logger.LogError(
                ex,
                "Erro ao processar notificaÃ§Ã£o {NotificationId}.",
                notification.Id);
        }
    }
}
