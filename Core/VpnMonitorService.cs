using System.Diagnostics;
using System.Net.NetworkInformation;
using VpnMonitor.Models;

namespace VpnMonitor.Core;

/// <summary>
/// Monitors AmneziaWG tunnel connections by combining two checks:
///   1. Process check — verifies <c>amneziawg.exe</c> is running.
///   2. Network adapters — finds Up interfaces with Description "WireGuard Tunnel".
///
/// Fires <see cref="VpnEventOccurred"/> on connect / disconnect.
/// </summary>
public sealed class VpnMonitorService : IDisposable
{
    private const string AmneziaProcessName = "amneziawg";
    private const string TunnelAdapterDescription = "WireGuard Tunnel";

    private HashSet<string> _prevTunnels = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;

    private readonly System.Threading.Timer _timer;

    public event EventHandler<VpnEvent>? VpnEventOccurred;

    public VpnMonitorService(int pollIntervalMs = 2000)
    {
        _timer = new System.Threading.Timer(
            callback: Poll,
            state:    null,
            dueTime:  TimeSpan.Zero,
            period:   TimeSpan.FromMilliseconds(pollIntervalMs));
    }

    private void Poll(object? _)
    {
        try
        {
            var current = GetAmneziaWgTunnels();

            if (!_initialized)
            {
                _prevTunnels = current;
                _initialized = true;
                return;
            }

            DetectChanges(_prevTunnels, current);
            _prevTunnels = current;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VpnMonitor] Poll error: {ex.Message}");
        }
    }

    private void DetectChanges(HashSet<string> previous, HashSet<string> current)
    {
        foreach (var name in previous.Except(current, StringComparer.OrdinalIgnoreCase))
            Raise(VpnEventType.Disconnected, name);

        foreach (var name in current.Except(previous, StringComparer.OrdinalIgnoreCase))
            Raise(VpnEventType.Connected, name);
    }

    private void Raise(VpnEventType type, string name) =>
        VpnEventOccurred?.Invoke(this, new VpnEvent
        {
            Type           = type,
            ConnectionName = name,
            Source         = "AmneziaWG",
            Timestamp      = DateTime.Now
        });

    /// <summary>
    /// Returns names of active AmneziaWG tunnel adapters.
    /// Returns an empty set if the AmneziaWG process is not running.
    /// </summary>
    public static HashSet<string> GetAmneziaWgTunnels()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!IsAmneziaWgRunning())
            return result;

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up)
                continue;

            if (nic.Description.Contains(TunnelAdapterDescription, StringComparison.OrdinalIgnoreCase))
                result.Add(nic.Name);
        }

        return result;
    }

    /// <summary>Returns the set of all active AmneziaWG connections.</summary>
    public static HashSet<string> GetAllVpnConnections() => GetAmneziaWgTunnels();

    private static bool IsAmneziaWgRunning()
    {
        try
        {
            var processes = Process.GetProcessesByName(AmneziaProcessName);
            bool running = processes.Length > 0;
            foreach (var p in processes)
                p.Dispose();
            return running;
        }
        catch
        {
            return false;
        }
    }

    public void UpdatePollInterval(int intervalMs) =>
        _timer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(intervalMs));

    public void Dispose() => _timer.Dispose();
}
