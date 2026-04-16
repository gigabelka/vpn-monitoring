using System.Runtime.InteropServices;

namespace VpnMonitor.Core;

/// <summary>
/// P/Invoke declarations for the Windows Remote Access Service (RAS) API.
/// Docs: https://learn.microsoft.com/en-us/windows/win32/api/ras/
/// </summary>
internal static class RasInterop
{
    // ── Constants ────────────────────────────────────────────────────────────
    public const uint ERROR_SUCCESS          = 0;
    public const uint ERROR_BUFFER_TOO_SMALL = 603;

    private const int RAS_MaxEntryName  = 256;
    private const int RAS_MaxDeviceType = 16;
    private const int RAS_MaxDeviceName = 128;
    private const int RAS_MaxPhoneNumber = 128;

    // ── Structs ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Describes a RAS connection.
    /// dwSize MUST be set before the first call.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct RASCONN
    {
        public int dwSize;
        public IntPtr hrasconn;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RAS_MaxEntryName + 1)]
        public string szEntryName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RAS_MaxDeviceType + 1)]
        public string szDeviceType;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RAS_MaxDeviceName + 1)]
        public string szDeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RAS_MaxPhoneNumber + 1)]
        public string szPhoneNumber;
    }

    // ── Imports ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all active RAS connections on this machine.
    /// lprasconn : pre-allocated RASCONN[] with dwSize initialised; may be null on first probe call.
    /// lpcb      : in/out buffer size in bytes.
    /// lpcConnections : out number of returned entries.
    /// </summary>
    [DllImport("rasapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern uint RasEnumConnections(
        [In, Out] RASCONN[]? lprasconn,
        ref int              lpcb,
        ref int              lpcConnections);
}
