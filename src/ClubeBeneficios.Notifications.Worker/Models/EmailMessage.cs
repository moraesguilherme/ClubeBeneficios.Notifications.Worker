namespace ClubeBeneficios.Notifications.Worker.Models;

public class EmailMessage
{
    public string ToEmail { get; set; } = string.Empty;

    public string? ToName { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string BodyHtml { get; set; } = string.Empty;

    public string? BodyText { get; set; }
}
