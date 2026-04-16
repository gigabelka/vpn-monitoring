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

WPF system-tray application (.NET 8, Windows x64) that monitors AmneziaWG tunnel connections and shows desktop notifications. No third-party NuGet packages — only .NET framework libraries. UI text is in Russian.

### Core flow

`App.xaml.cs` is the entry point — creates and wires all components, owns the system tray `NotifyIcon`, and subscribes to `VpnMonitorService.VpnEventOccurred` events. Events are dispatched to the UI thread via `Dispatcher.Invoke()` and forwarded to `NotificationManager.Show()`.

### AmneziaWG detection (Core/)

`VpnMonitorService` polls every 2 seconds using a hybrid detection approach:

1. **Process check** — verifies `amneziawg.exe` is running via `Process.GetProcessesByName()`. If not running, no tunnels are reported.
2. **Network adapters** — `NetworkInterface.GetAllNetworkInterfaces()` filtered by `Description` containing "WireGuard Tunnel" (AmneziaWG creates adapters with this description).

State is tracked via `_prevTunnels` HashSet; diffs fire `VpnEvent` records.

### Notifications (Notifications/)

`NotificationManager` maintains a bottom-right stack of `NotificationWindow` instances. Slot 0 is nearest the taskbar; older notifications stack upward. Windows are frameless, always-on-top, with slide-in/out animations via WPF Storyboards.

`NotificationWindow` uses code-behind as its own DataContext — bound properties: `IconText`, `Title`, `Message`, `TimestampText`, `AccentBrush`.

### Tray icon (Core/TrayIconFactory.cs)

Dynamically renders a 32×32 shield icon via GDI+ at runtime (green checkmark = connected, grey X = disconnected). No embedded .ico files.

### Settings (Settings/, Core/SettingsService.cs)

`SettingsService` persists `AppSettings` to `%LOCALAPPDATA%\VpnMonitor\settings.json` using atomic writes (write to `.tmp`, then `File.Move`). It also manages the Windows autostart registry key (`HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`).

`SettingsWindow` is a frameless dark-themed WPF window implementing `INotifyPropertyChanged` directly (no MVVM framework). Save flow: `SettingsWindow.SettingsSaved` → `App.OnSettingsSaved()` → `SettingsService.Save()`, which also live-updates poll interval and notification duration on the running services.

### Implementation notes

- WinForms `NotifyIcon` is used because WPF has no native tray icon support — the csproj enables both `UseWPF` and `UseWindowsForms`.
- 300ms `DispatcherTimer` debounce on tray click distinguishes single-click (open settings) from double-click (check status now).
- First poll is silent (`_initialized` flag in `VpnMonitorService`) to avoid spurious events on app startup.
- `VpnMonitorService.GetAllVpnConnections()` is a static method for on-demand status checks, used by the "Проверить сейчас" context menu item.

## Conventions

- Namespace: `VpnMonitor` root with `Core`, `Models`, `Notifications`, `Settings` sub-namespaces
- Classes are `sealed` where possible; `VpnEvent` is a sealed record with init-only properties
- String comparisons use `StringComparer.OrdinalIgnoreCase` for network/registry names
- `IDisposable` pattern for Timer and graphics resources
- Nullable reference types enabled, implicit usings enabled
