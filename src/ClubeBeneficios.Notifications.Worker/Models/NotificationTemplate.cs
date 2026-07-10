namespace ClubeBeneficios.Notifications.Worker.Models;

public class NotificationTemplate
{
    public string TemplateKey { get; set; } = string.Empty;

    public int VersionNumber { get; set; }

    public string SubjectTemplate { get; set; } = string.Empty;

    public string BodyHtmlTemplate { get; set; } = string.Empty;

    public string? BodyTextTemplate { get; set; }
}
