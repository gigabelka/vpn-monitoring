using System.Windows;
using System.Windows.Forms;
using VpnMonitor.Core;
using VpnMonitor.Models;
using VpnMonitor.Notifications;

namespace VpnMonitor;

public partial class App : System.Windows.Application
{
    private NotifyIcon?            _trayIcon;
    private VpnMonitorService?     _monitor;
    private NotificationManager?   _notificationManager;

    // ─────────────────────────────────────────────────────────────────────────
    // Startup
    // ─────────────────────────────────────────────────────────────────────────
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _notificationManager = new NotificationManager();

        _monitor = new VpnMonitorService();
        _monitor.VpnEventOccurred += OnVpnEvent;

        SetupTrayIcon();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // VPN event → dispatch to UI thread → show notification
    // ─────────────────────────────────────────────────────────────────────────
    private void OnVpnEvent(object? sender, VpnEvent evt)
    {
        Dispatcher.Invoke(() =>
        {
            _notificationManager?.Show(evt);
            UpdateTrayIconState(evt.Type == VpnEventType.Connected);
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tray icon
    // ─────────────────────────────────────────────────────────────────────────
    private void SetupTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon    = TrayIconFactory.Create(connected: false),
            Visible = true,
            Text    = "VPN Monitor — нет подключений"
        };

        // Context menu
        var menu = new ContextMenuStrip();

        var statusItem = new ToolStripMenuItem("Статус VPN") { Enabled = false };
        menu.Items.Add(statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Проверить сейчас", null, (_, _) => CheckNow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => ExitApp());

        _trayIcon.ContextMenuStrip = menu;

        // Double-click → balloon with current status
        _trayIcon.DoubleClick += (_, _) => CheckNow();
    }

    private void UpdateTrayIconState(bool connected)
    {
        if (_trayIcon is null) return;
        _trayIcon.Icon = TrayIconFactory.Create(connected);
        _trayIcon.Text = connected
            ? "VPN Monitor — подключён ✔"
            : "VPN Monitor — нет подключений";
    }

    private void CheckNow()
    {
        var connections = VpnMonitorService.GetAllVpnConnections();
        var msg = connections.Count == 0
            ? "Активных VPN-соединений нет"
            : $"Активно: {string.Join(", ", connections)}";

        _trayIcon?.ShowBalloonTip(4000, "VPN Monitor", msg,
            connections.Count > 0 ? ToolTipIcon.Info : ToolTipIcon.Warning);
    }

    private void ExitApp()
    {
        _trayIcon!.Visible = false;
        _monitor?.Dispose();
        _trayIcon?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _monitor?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
