using System.Text.Json;
using RMSweep.Models;

namespace RMSweep.Services;

/// <summary>
/// Persists user preferences across sessions.
/// </summary>
public static class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RMSweep",
        "appsettings.json");

    private static AppSettings? _settings;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                return _settings;
            }
        }
        catch
        {
            // Fallback to defaults on any error
        }

        _settings = new AppSettings();
        return _settings;
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            _settings = settings;
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently ignore save errors
        }
    }

    public static AppSettings Current => _settings ?? Load();
}
