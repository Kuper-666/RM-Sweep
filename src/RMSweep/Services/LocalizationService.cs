using System.Globalization;
using System.Resources;

namespace RMSweep.Services;

/// <summary>
/// Manages localization at runtime using embedded .resx resource files.
/// </summary>
public static class LocalizationService
{
    private static ResourceManager? _resourceManager;
    private static volatile CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

    public static event Action? LanguageChanged;

    public static void Initialize()
    {
        _resourceManager = new ResourceManager(
            "RMSweep.Resources.Localization.Strings",
            typeof(LocalizationService).Assembly);
    }

    public static string GetString(string key)
    {
        _resourceManager ??= new ResourceManager(
            "RMSweep.Resources.Localization.Strings",
            typeof(LocalizationService).Assembly);

        return _resourceManager.GetString(key, _currentCulture) ?? $"[{key}]";
    }

    public static void SetLanguage(string cultureCode)
    {
        var culture = new CultureInfo(cultureCode);
        _currentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        LanguageChanged?.Invoke();
    }

    public static string GetCurrentLanguage() => _currentCulture.Name;

    public static IReadOnlyList<string> GetAvailableLanguages() =>
        new[] { "en-US", "ru-RU", "de-DE" };

    public static string GetLanguageDisplayName(string code) => code switch
    {
        "en-US" => "English",
        "ru-RU" => "Русский",
        "de-DE" => "Deutsch",
        _ => code
    };
}
