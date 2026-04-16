using System.Windows;
using System.Windows.Threading;
using VpnMonitor.Models;

namespace VpnMonitor.Notifications;

/// <summary>
/// Manages a vertical stack of <see cref="NotificationWindow"/> instances
/// pinned to the bottom-right corner of the primary work area.
///
/// Layout (bottom → top):
///   slot 0 → newest notification  (closest to taskbar)
///   slot 1 → previous
///   slot n → oldest
///
/// When a window is closed the remaining windows animate downward to fill the gap.
/// </summary>
public sealed class NotificationManager
{
    // ── Configuration ─────────────────────────────────────────────────────────
    private const double WindowHeight  = 88;   // matches NotificationWindow.Height
    private const double WindowSpacing = 8;    // gap between windows
    private const double ScreenMargin  = 14;   // distance from screen edge / taskbar

    // ── State ─────────────────────────────────────────────────────────────────
    private readonly List<NotificationWindow> _stack = [];
    private readonly Dictionary<NotificationWindow, DispatcherTimer> _autoCloseTimers = [];

    /// <summary>Auto-close delay in seconds. 0 = manual close only.</summary>
    public int AutoCloseDurationSeconds { get; set; } = 5;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Create and display a new notification for <paramref name="evt"/>.</summary>
    public void Show(VpnEvent evt)
    {
        var win = new NotificationWindow(evt);
        win.Closed += OnWindowClosed;

        _stack.Insert(0, win); // newest at index 0 (bottom of screen)

        // Shift all existing windows up to make room for the new one.
        RepositionAll(animateExisting: true, excludeIndex: 0);

        // Place the new window at slot 0 (still off-screen to the right) then animate in.
        ApplyPosition(win, 0);
        win.Show();
        win.PlaySlideIn();

        if (AutoCloseDurationSeconds > 0)
            StartAutoClose(win);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void StartAutoClose(NotificationWindow win)
    {
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(AutoCloseDurationSeconds)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (win.IsLoaded)
                win.AnimateClose();
        };
        _autoCloseTimers[win] = timer;
        timer.Start();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is NotificationWindow win)
        {
            if (_autoCloseTimers.Remove(win, out var timer))
                timer.Stop();

            _stack.Remove(win);
            RepositionAll(animateExisting: true);
        }
    }

    /// <summary>Recalculate and (optionally) animate every window in the stack.</summary>
    private void RepositionAll(bool animateExisting, int excludeIndex = -1)
    {
        for (int i = 0; i < _stack.Count; i++)
        {
            if (i == excludeIndex) continue;

            double targetTop = ComputeTop(i);

            if (animateExisting)
                _stack[i].AnimateToTop(targetTop);
            else
                _stack[i].Top = targetTop;
        }
    }

    /// <summary>Immediately set position of a window at <paramref name="slot"/> index.</summary>
    private static void ApplyPosition(NotificationWindow win, int slot)
    {
        var area = SystemParameters.WorkArea;
        win.Left = area.Right  - win.Width - ScreenMargin;
        win.Top  = ComputeTop(slot);
    }

    /// <summary>
    /// Returns the Top coordinate for a window at <paramref name="slot"/>.
    /// Slot 0 is nearest the taskbar, higher slots stack upward.
    /// </summary>
    private static double ComputeTop(int slot)
    {
        var area = SystemParameters.WorkArea;
        return area.Bottom - ScreenMargin - (slot + 1) * (WindowHeight + WindowSpacing);
    }
}
