namespace ClubeBeneficios.Notifications.Worker.Models;

public class NotificationSendResult
{
    public bool Success { get; init; }

    public string? ProviderMessageId { get; init; }

    public string? ErrorMessage { get; init; }

    public static NotificationSendResult Sent(string? providerMessageId = null)
        => new()
        {
            Success = true,
            ProviderMessageId = providerMessageId
        };

    public static NotificationSendResult Failed(string errorMessage)
        => new()
        {
            Success = false,
            ErrorMessage = errorMessage
        };
}
