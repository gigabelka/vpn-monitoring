namespace VpnMonitor.Models;

public enum VpnEventType
{
    Connected,
    Disconnected
}

public sealed class VpnEvent
{
    public VpnEventType Type           { get; init; }
    public string       ConnectionName { get; init; } = string.Empty;
    public DateTime     Timestamp      { get; init; } = DateTime.Now;

    /// <summary>Detection source (AmneziaWG).</summary>
    public string Source { get; init; } = string.Empty;
}
