namespace ClubeBeneficios.Notifications.Worker.Models;

public class RenderedNotification
{
    public string Subject { get; set; } = string.Empty;

    public string BodyHtml { get; set; } = string.Empty;

    public string? BodyText { get; set; }
}
