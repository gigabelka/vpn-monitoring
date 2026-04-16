using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using VpnMonitor.Models;

namespace VpnMonitor.Notifications;

public partial class NotificationWindow : Window
{
    // ── Bound properties (DataContext = this) ─────────────────────────────────
    public string           IconText      { get; }
    public new string       Title         { get; }
    public string           Message       { get; }
    public string           TimestampText { get; }
    public SolidColorBrush  AccentBrush   { get; }

    private static readonly System.Windows.Media.Color ColorConnected    = System.Windows.Media.Color.FromRgb(0x22, 0xC5, 0x5E); // emerald
    private static readonly System.Windows.Media.Color ColorDisconnected = System.Windows.Media.Color.FromRgb(0xEF, 0x44, 0x44); // red

    // ─────────────────────────────────────────────────────────────────────────
    public NotificationWindow(VpnEvent evt)
    {
        bool connected = evt.Type == VpnEventType.Connected;

        IconText      = connected ? "🟢" : "🔴";
        Title         = connected ? "VPN подключён" : "VPN отключён";
        Message       = evt.ConnectionName.Length > 0 ? evt.ConnectionName : "Неизвестное соединение";
        TimestampText = evt.Timestamp.ToString("HH:mm:ss");
        AccentBrush   = new SolidColorBrush(connected ? ColorConnected : ColorDisconnected);

        DataContext = this;
        InitializeComponent();
    }

    // ── Animations ────────────────────────────────────────────────────────────

    /// <summary>Call after Show() to play the slide-in animation.</summary>
    public void PlaySlideIn()
    {
        var sb = (Storyboard)FindResource("SlideIn");
        sb.Begin(this);
    }

    private void PlaySlideOut(Action onCompleted)
    {
        var sb = (Storyboard)FindResource("SlideOut");
        sb.Completed += (_, _) => onCompleted();
        sb.Begin(this);
    }

    // ── Reposition with animation (called by NotificationManager) ────────────

    /// <summary>Smoothly move the window to a new vertical position.</summary>
    public void AnimateToTop(double newTop)
    {
        var anim = new DoubleAnimation(newTop, new Duration(TimeSpan.FromMilliseconds(260)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        // Stop any running Top animation before starting a new one.
        BeginAnimation(TopProperty, null);
        BeginAnimation(TopProperty, anim);
    }

    // ── Close button ─────────────────────────────────────────────────────────

    private bool _closing;

    /// <summary>Trigger the slide-out animation followed by Close (used by auto-close timer).</summary>
    public void AnimateClose()
    {
        if (_closing) return;
        _closing = true;
        PlaySlideOut(Close);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => AnimateClose();
}
