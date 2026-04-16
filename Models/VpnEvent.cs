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

    /// <summary>Human-readable description of the event source (RAS or virtual adapter).</summary>
    public string Source { get; init; } = string.Empty;
}
