namespace VpnMonitoring.Models;

public sealed class AppSettings
{
    public int  PollIntervalSeconds         { get; set; } = 2;
    public bool StartWithWindows            { get; set; }
    public int  NotificationDurationSeconds { get; set; } = 5;
    public bool PlaySound                   { get; set; }

    public AppSettings Clone() => (AppSettings)MemberwiseClone();
}
