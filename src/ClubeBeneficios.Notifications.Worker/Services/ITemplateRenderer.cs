using ClubeBeneficios.Notifications.Worker.Models;

namespace ClubeBeneficios.Notifications.Worker.Services;

public interface ITemplateRenderer
{
    RenderedNotification Render(NotificationMessage message);
}
