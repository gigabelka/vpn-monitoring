# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet run                    # Run in Debug mode
dotnet build                  # Build (Debug)
dotnet build -c Release       # Build (Release)

# Publish as single self-contained exe
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

No test project exists yet. No linter or formatter configured.

## Architecture

WPF system-tray application (.NET 8, Windows x64) that monitors VPN connections and shows desktop notifications. No third-party NuGet packages — only .NET framework libraries. UI text is in Russian.

### Core flow

`App.xaml.cs` is the entry point — creates and wires all components, owns the system tray `NotifyIcon`, and subscribes to `VpnMonitorService.VpnChanged` events. Events are dispatched to the UI thread via `Dispatcher.Invoke()` and forwarded to `NotificationManager.Show()`.

### VPN detection (Core/)

`VpnMonitorService` polls every 2 seconds using two detection sources:

1. **RAS API** — P/Invoke to `rasapi32.dll` (`RasInterop.cs`) detects built-in Windows VPN types (PPTP, L2TP, SSTP, IKEv2)
2. **Network adapters** — `NetworkInterface.GetAllNetworkInterfaces()` with keyword matching for third-party VPNs (WireGuard, OpenVPN, TAP, NordVPN, ExpressVPN, Mullvad)

State is tracked via `_prevRas` / `_prevVirtual` HashSets; diffs fire `VpnEvent` records.

### Notifications (Notifications/)

`NotificationManager` maintains a bottom-right stack of `NotificationWindow` instances. Slot 0 is nearest the taskbar; older notifications stack upward. Windows are frameless, always-on-top, with slide-in/out animations via WPF Storyboards.

`NotificationWindow` uses code-behind as its own DataContext — bound properties: `IconText`, `Title`, `Message`, `TimestampText`, `AccentBrush`.

### Tray icon (Core/TrayIconFactory.cs)

Dynamically renders a 32×32 shield icon via GDI+ at runtime (green checkmark = connected, grey X = disconnected). No embedded .ico files.

## Conventions

- Namespace: `VpnMonitor` root with `Core`, `Models`, `Notifications` sub-namespaces
- Classes are `sealed` where possible; `VpnEvent` is a sealed record with init-only properties
- String comparisons use `StringComparer.OrdinalIgnoreCase` for network/registry names
- `IDisposable` pattern for Timer and graphics resources
- Nullable reference types enabled, implicit usings enabled, unsafe blocks allowed
