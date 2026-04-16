using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using VpnMonitor.Models;

namespace VpnMonitor.Core;

/// <summary>
/// Monitors VPN connections via two complementary methods:
///   1. RAS API  — built-in Windows VPN (PPTP, L2TP, SSTP, IKEv2, SSTP).
///   2. Virtual network adapters — WireGuard, OpenVPN, TAP adapters, etc.
///
/// Fires <see cref="VpnEventOccurred"/> on connect / disconnect.
/// </summary>
public sealed class VpnMonitorService : IDisposable
{
    // ── VPN adapter name keywords for virtual-adapter detection ──────────────
    private static readonly string[] VirtualVpnKeywords =
        ["wireguard", "openvpn", "tap-", "tap0", "tun", "nordvpn", "expressvpn", "mullvad"];

    // ── RAS device types treated as VPN ──────────────────────────────────────
    private static readonly HashSet<string> RasVpnDeviceTypes =
        new(StringComparer.OrdinalIgnoreCase) { "VPN", "PPTP", "L2TP", "SSTP", "IKEv2" };

    // ── State ─────────────────────────────────────────────────────────────────
    private HashSet<string> _prevRas     = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _prevVirtual = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;

    private readonly System.Threading.Timer _timer;

    public event EventHandler<VpnEvent>? VpnEventOccurred;

    // ── Constructor ───────────────────────────────────────────────────────────
    public VpnMonitorService(int pollIntervalMs = 2000)
    {
        _timer = new System.Threading.Timer(
            callback: Poll,
            state:    null,
            dueTime:  TimeSpan.Zero,
            period:   TimeSpan.FromMilliseconds(pollIntervalMs));
    }

    // ── Poll ─────────────────────────────────────────────────────────────────
    private void Poll(object? _)
    {
        try
        {
            var ras     = GetRasVpnConnections();
            var virtual_ = GetVirtualVpnAdapters();

            if (!_initialized)
            {
                _prevRas     = ras;
                _prevVirtual = virtual_;
                _initialized = true;
                return;
            }

            DetectChanges(_prevRas,     ras,     "RAS");
            DetectChanges(_prevVirtual, virtual_, "Adapter");

            _prevRas     = ras;
            _prevVirtual = virtual_;
        }
        catch (Exception ex)
        {
            // TODO: plug in ILogger here
            System.Diagnostics.Debug.WriteLine($"[VpnMonitor] Poll error: {ex.Message}");
        }
    }

    private void DetectChanges(HashSet<string> previous, HashSet<string> current, string source)
    {
        foreach (var name in previous.Except(current, StringComparer.OrdinalIgnoreCase))
            Raise(VpnEventType.Disconnected, name, source);

        foreach (var name in current.Except(previous, StringComparer.OrdinalIgnoreCase))
            Raise(VpnEventType.Connected, name, source);
    }

    private void Raise(VpnEventType type, string name, string source) =>
        VpnEventOccurred?.Invoke(this, new VpnEvent
        {
            Type           = type,
            ConnectionName = name,
            Source         = source,
            Timestamp      = DateTime.Now
        });

    // ── RAS connections ───────────────────────────────────────────────────────
    /// <summary>
    /// Calls RasEnumConnections and returns names of VPN-type connections.
    /// </summary>
    public static HashSet<string> GetRasVpnConnections()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        int structSize = Marshal.SizeOf<RasInterop.RASCONN>();

        // Probe call: single-element array lets RAS tell us how many bytes it needs.
        var buffer = new RasInterop.RASCONN[1];
        buffer[0].dwSize = structSize;

        int cb    = structSize;
        int count = 0;

        uint ret = RasInterop.RasEnumConnections(buffer, ref cb, ref count);

        if (ret == RasInterop.ERROR_BUFFER_TOO_SMALL)
        {
            int needed = Math.Max(1, cb / structSize);
            buffer = new RasInterop.RASCONN[needed];
            for (int i = 0; i < needed; i++)
                buffer[i].dwSize = structSize;

            ret = RasInterop.RasEnumConnections(buffer, ref cb, ref count);
        }

        if (ret != RasInterop.ERROR_SUCCESS)
            return result;

        for (int i = 0; i < count; i++)
        {
            string devType = buffer[i].szDeviceType ?? string.Empty;
            if (RasVpnDeviceTypes.Contains(devType))
                result.Add(buffer[i].szEntryName ?? $"VPN-{i}");
        }

        return result;
    }

    // ── Virtual adapters (WireGuard, OpenVPN, TAP …) ─────────────────────────
    /// <summary>
    /// Returns names of Up network interfaces that look like VPN virtual adapters.
    /// </summary>
    public static HashSet<string> GetVirtualVpnAdapters()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up)
                continue;

            string name = nic.Name.ToLowerInvariant();
            string desc = nic.Description.ToLowerInvariant();

            if (VirtualVpnKeywords.Any(k => name.Contains(k) || desc.Contains(k)))
                result.Add(nic.Name);
        }

        return result;
    }

    /// <summary>Returns the combined set of all active VPN connections.</summary>
    public static HashSet<string> GetAllVpnConnections()
    {
        var all = GetRasVpnConnections();
        foreach (var n in GetVirtualVpnAdapters())
            all.Add(n);
        return all;
    }

    // ── Configuration ─────────────────────────────────────────────────────────
    public void UpdatePollInterval(int intervalMs) =>
        _timer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(intervalMs));

    // ── IDisposable ───────────────────────────────────────────────────────────
    public void Dispose() => _timer.Dispose();
}
