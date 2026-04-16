using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using VpnMonitor.Models;

namespace VpnMonitor.Settings;

public partial class SettingsWindow : Window, INotifyPropertyChanged
{
    private int  _pollIntervalSeconds;
    private int  _notificationDurationSeconds;
    private bool _startWithWindows;
    private bool _playSound;

    public event Action<AppSettings>?       SettingsSaved;
    public event PropertyChangedEventHandler? PropertyChanged;

    public int PollIntervalSeconds
    {
        get => _pollIntervalSeconds;
        set { _pollIntervalSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(PollIntervalText)); }
    }

    public int NotificationDurationSeconds
    {
        get => _notificationDurationSeconds;
        set { _notificationDurationSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(NotificationDurationText)); }
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set { _startWithWindows = value; OnPropertyChanged(); }
    }

    public bool PlaySound
    {
        get => _playSound;
        set { _playSound = value; OnPropertyChanged(); }
    }

    public string PollIntervalText => $"{_pollIntervalSeconds} сек";

    public string NotificationDurationText =>
        _notificationDurationSeconds == 0 ? "вручную" : $"{_notificationDurationSeconds} сек";

    public SettingsWindow(AppSettings settings)
    {
        _pollIntervalSeconds         = settings.PollIntervalSeconds;
        _notificationDurationSeconds = settings.NotificationDurationSeconds;
        _startWithWindows            = settings.StartWithWindows;
        _playSound                   = settings.PlaySound;

        DataContext = this;
        InitializeComponent();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var result = new AppSettings
        {
            PollIntervalSeconds         = PollIntervalSeconds,
            NotificationDurationSeconds = NotificationDurationSeconds,
            StartWithWindows            = StartWithWindows,
            PlaySound                   = PlaySound
        };

        SettingsSaved?.Invoke(result);
        Close();
    }
}
