using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using VpnMonitoring.Models;

namespace VpnMonitoring.Core;

public sealed class SettingsService
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VpnMonitoring");

    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppSettings Current { get; private set; } = new();

    public event Action? SettingsChanged;

    public void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch
        {
            Current = new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Dir);

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var tmp  = FilePath + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, FilePath, overwrite: true);

            Current = settings;
            ApplyStartWithWindows(settings.StartWithWindows);
            SettingsChanged?.Invoke();
        }
        catch
        {
            // Best-effort save — don't crash the app if the file system fails.
        }
    }

    private static void ApplyStartWithWindows(bool enabled)
    {
        const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        const string valueName = "VpnMonitoring";

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
            if (key is null) return;

            if (enabled)
            {
                var exePath = Environment.ProcessPath;
                if (exePath is not null)
                    key.SetValue(valueName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(valueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Registry access may be restricted — ignore silently.
        }
    }
}
