namespace ClubeBeneficios.Notifications.Worker.Configuration;

public class NotificationWorkerOptions
{
    public string Mode { get; set; } = "Watch";
    public int PollingIntervalSeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 20;
    public int LockMinutes { get; set; } = 5;
    public bool ReleaseExpiredLocksOnStart { get; set; } = true;
    public int MaxItemsPerCycle { get; set; } = 100;
}
