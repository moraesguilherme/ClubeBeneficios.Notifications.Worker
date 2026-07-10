using ClubeBeneficios.Notifications.Worker.Models;

namespace ClubeBeneficios.Notifications.Worker.Infrastructure.Email;

public interface IEmailSender
{
    Task<NotificationSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default);
}
