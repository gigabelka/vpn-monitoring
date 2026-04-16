using System.Media;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using VpnMonitor.Core;
using VpnMonitor.Models;
using VpnMonitor.Notifications;
using VpnMonitor.Settings;

namespace VpnMonitor;

public partial class App : System.Windows.Application
{
    private NotifyIcon?            _trayIcon;
    private VpnMonitorService?     _monitor;
    private NotificationManager?   _notificationManager;
    private SettingsService?       _settings;
    private SettingsWindow?        _settingsWindow;
    private DispatcherTimer?       _clickDebounce;

    // ─────────────────────────────────────────────────────────────────────────
    // Startup
    // ─────────────────────────────────────────────────────────────────────────
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settings = new SettingsService();
        _settings.Load();

        _notificationManager = new NotificationManager
        {
            AutoCloseDurationSeconds = _settings.Current.NotificationDurationSeconds
        };

        _monitor = new VpnMonitorService(_settings.Current.PollIntervalSeconds * 1000);
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

            if (_settings?.Current.PlaySound == true)
                SystemSounds.Asterisk.Play();
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
            Text    = "AmneziaWG Monitor — нет подключений"
        };

        // Context menu
        var menu = new ContextMenuStrip();

        var statusItem = new ToolStripMenuItem("Статус AmneziaWG") { Enabled = false };
        menu.Items.Add(statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Проверить сейчас", null, (_, _) => CheckNow());
        menu.Items.Add("Настройки",        null, (_, _) => OpenSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => ExitApp());

        _trayIcon.ContextMenuStrip = menu;

        // Click / double-click debounce:
        // Single click → open settings (after 300 ms debounce)
        // Double click → cancel debounce, show status balloon
        _clickDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _clickDebounce.Tick += (_, _) =>
        {
            _clickDebounce.Stop();
            OpenSettings();
        };

        _trayIcon.Click += (_, e) =>
        {
            if (e is System.Windows.Forms.MouseEventArgs me && me.Button != MouseButtons.Left)
                return;
            _clickDebounce.Start();
        };

        _trayIcon.DoubleClick += (_, _) =>
        {
            _clickDebounce.Stop();
            CheckNow();
        };
    }

    private void OpenSettings()
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings!.Current.Clone());
        _settingsWindow.SettingsSaved += OnSettingsSaved;
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private void OnSettingsSaved(AppSettings newSettings)
    {
        _settings!.Save(newSettings);

        _monitor?.UpdatePollInterval(newSettings.PollIntervalSeconds * 1000);

        if (_notificationManager is not null)
            _notificationManager.AutoCloseDurationSeconds = newSettings.NotificationDurationSeconds;
    }

    private void UpdateTrayIconState(bool connected)
    {
        if (_trayIcon is null) return;
        _trayIcon.Icon = TrayIconFactory.Create(connected);
        _trayIcon.Text = connected
            ? "AmneziaWG Monitor — подключён ✔"
            : "AmneziaWG Monitor — нет подключений";
    }

    private void CheckNow()
    {
        var connections = VpnMonitorService.GetAllVpnConnections();
        var msg = connections.Count == 0
            ? "Активных AmneziaWG-туннелей нет"
            : $"AmneziaWG: {string.Join(", ", connections)}";

        _trayIcon?.ShowBalloonTip(4000, "AmneziaWG Monitor", msg,
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
