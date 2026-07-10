namespace ClubeBeneficios.Notifications.Worker.Models;

public class NotificationMessage
{
    public Guid Id { get; set; }

    public string Module { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string? AggregateType { get; set; }

    public Guid? AggregateId { get; set; }

    public string TemplateKey { get; set; } = string.Empty;

    public string RecipientType { get; set; } = string.Empty;

    public string RecipientEmail { get; set; } = string.Empty;

    public string? RecipientName { get; set; }

    public string PayloadJson { get; set; } = "{}";

    public int Priority { get; set; }

    public int Attempts { get; set; }

    public int MaxAttempts { get; set; }

    public string? SubjectTemplate { get; set; }

    public string? BodyHtmlTemplate { get; set; }

    public string? BodyTextTemplate { get; set; }

    public DateTime CreatedAt { get; set; }
}
