using RMSweep.Models;
using RMSweep.Services;

namespace RMSweep.Tests.Services;

public class LogServiceTests
{
    private readonly LogService _logService = new();

    [Fact]
    public void Entries_IsInitiallyEmpty()
    {
        Assert.Empty(_logService.Entries);
    }

    [Fact]
    public void AddInfo_AddsEntryWithCorrectLevel()
    {
        _logService.AddInfo("TestOp", "Test message");

        var entries = _logService.Entries;
        Assert.Single(entries);
        Assert.Equal(LogLevel.Info, entries[0].Level);
        Assert.Equal("TestOp", entries[0].Operation);
        Assert.Equal("Test message", entries[0].Details);
    }

    [Fact]
    public void AddSuccess_AddsEntryWithCorrectLevel()
    {
        _logService.AddSuccess("Op", "Success msg");

        Assert.Single(_logService.Entries);
        Assert.Equal(LogLevel.Success, _logService.Entries[0].Level);
    }

    [Fact]
    public void AddWarning_AddsEntryWithCorrectLevel()
    {
        _logService.AddWarning("Op", "Warning msg");

        Assert.Single(_logService.Entries);
        Assert.Equal(LogLevel.Warning, _logService.Entries[0].Level);
    }

    [Fact]
    public void AddError_AddsEntryWithCorrectLevel()
    {
        _logService.AddError("Op", "Error msg");

        Assert.Single(_logService.Entries);
        Assert.Equal(LogLevel.Error, _logService.Entries[0].Level);
    }

    [Fact]
    public void AddResult_Success_CallsAddSuccess()
    {
        var result = new CleanResult
        {
            Success = true,
            Message = "All good",
            FilesDeleted = 10,
            BytesFreed = 1024
        };

        _logService.AddResult("Op", result);

        Assert.Single(_logService.Entries);
        Assert.Equal(LogLevel.Success, _logService.Entries[0].Level);
        Assert.Contains("All good", _logService.Entries[0].Details);
        Assert.Contains("10 files", _logService.Entries[0].Details);
    }

    [Fact]
    public void AddResult_Failure_CallsAddError()
    {
        var result = new CleanResult
        {
            Success = false,
            Message = "Failed",
            Errors = { "err1", "err2" }
        };

        _logService.AddResult("Op", result);

        Assert.Equal(3, _logService.Entries.Count); // 1 error + 2 error details
        Assert.All(_logService.Entries, e => Assert.Equal(LogLevel.Error, e.Level));
    }

    [Fact]
    public void AddResult_FailureNoErrors_CallsAddErrorOnce()
    {
        var result = new CleanResult
        {
            Success = false,
            Message = "Failed"
        };

        _logService.AddResult("Op", result);

        Assert.Single(_logService.Entries);
        Assert.Equal(LogLevel.Error, _logService.Entries[0].Level);
    }

    [Fact]
    public void EntryAdded_Event_FiresOnAdd()
    {
        LogEntry? received = null;
        _logService.EntryAdded += e => received = e;

        _logService.AddInfo("Op", "msg");

        Assert.NotNull(received);
        Assert.Equal("Op", received!.Operation);
    }

    [Fact]
    public void EntryAdded_Event_FiresForEveryAdd()
    {
        var count = 0;
        _logService.EntryAdded += _ => count++;

        _logService.AddInfo("Op", "1");
        _logService.AddSuccess("Op", "2");
        _logService.AddWarning("Op", "3");
        _logService.AddError("Op", "4");

        Assert.Equal(4, count);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        _logService.AddInfo("Op", "1");
        _logService.AddInfo("Op", "2");
        _logService.AddInfo("Op", "3");

        Assert.Equal(3, _logService.Entries.Count);

        _logService.Clear();

        Assert.Empty(_logService.Entries);
    }

    [Fact]
    public void Clear_IsThreadSafe()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => _logService.AddInfo("Op", "msg")));
        }
        Task.WaitAll(tasks.ToArray());

        _logService.Clear();
        Assert.Empty(_logService.Entries);
    }

    [Fact]
    public void Entries_ReturnsSnapshot()
    {
        _logService.AddInfo("Op", "1");
        var snapshot = _logService.Entries;
        _logService.AddInfo("Op", "2");

        Assert.Single(snapshot);
        Assert.Equal(2, _logService.Entries.Count);
    }

    [Fact]
    public void Timestamp_IsSetOnEntry()
    {
        var before = DateTime.Now;
        _logService.AddInfo("Op", "msg");
        var after = DateTime.Now;

        Assert.InRange(_logService.Entries[0].Timestamp, before, after);
    }

    [Fact]
    public void MultipleOperations_TrackedCorrectly()
    {
        _logService.AddInfo("Op1", "msg1");
        _logService.AddSuccess("Op2", "msg2");
        _logService.AddError("Op3", "msg3");

        var entries = _logService.Entries;
        Assert.Equal(3, entries.Count);
        Assert.Equal("Op1", entries[0].Operation);
        Assert.Equal("Op2", entries[1].Operation);
        Assert.Equal("Op3", entries[2].Operation);
    }
}

public class SettingsServiceTests : IDisposable
{
    private readonly string _originalSettingsPath;

    public SettingsServiceTests()
    {
        _originalSettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RMSweep", "appsettings.json");

        if (File.Exists(_originalSettingsPath))
            File.Copy(_originalSettingsPath, _originalSettingsPath + ".bak", true);
    }

    public void Dispose()
    {
        if (File.Exists(_originalSettingsPath + ".bak"))
        {
            File.Copy(_originalSettingsPath + ".bak", _originalSettingsPath, true);
            File.Delete(_originalSettingsPath + ".bak");
        }
    }

    [Fact]
    public void Save_CreatesFile()
    {
        var settings = new AppSettings { Language = "ru-RU" };

        SettingsService.Save(settings);

        Assert.True(File.Exists(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RMSweep", "appsettings.json")));
    }

    [Fact]
    public void SaveAndLoad_PreservesSettings()
    {
        var settings = new AppSettings
        {
            Language = "de-DE",
            CleanRecycleBin = true,
            CleanDnsCache = true,
            ExcludePaths = new List<string> { @"C:\Test" },
            IncludeCustomFolders = new List<string> { @"D:\Custom" }
        };

        SettingsService.Save(settings);
        var loaded = SettingsService.Load();

        Assert.Equal("de-DE", loaded.Language);
        Assert.True(loaded.CleanRecycleBin);
        Assert.True(loaded.CleanDnsCache);
        Assert.Single(loaded.ExcludePaths);
        Assert.Contains(@"C:\Test", loaded.ExcludePaths);
        Assert.Single(loaded.IncludeCustomFolders);
    }

    [Fact]
    public void Current_ReturnsLoadedSettings()
    {
        var current = SettingsService.Current;
        Assert.NotNull(current);
    }

    [Fact]
    public void LoadHandles_CorruptedJson()
    {
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RMSweep", "appsettings.json");

        File.WriteAllText(settingsPath, "{ invalid json !!!");

        var settings = SettingsService.Load();

        Assert.NotNull(settings);
    }
}

public class LocalizationServiceTests
{
    [Fact]
    public void GetAvailableLanguages_ReturnsThreeLanguages()
    {
        var langs = LocalizationService.GetAvailableLanguages();

        Assert.Equal(3, langs.Count);
        Assert.Contains("en-US", langs);
        Assert.Contains("ru-RU", langs);
        Assert.Contains("de-DE", langs);
    }

    [Fact]
    public void GetLanguageDisplayName_ReturnsCorrectNames()
    {
        Assert.Equal("English", LocalizationService.GetLanguageDisplayName("en-US"));
        Assert.Equal("Русский", LocalizationService.GetLanguageDisplayName("ru-RU"));
        Assert.Equal("Deutsch", LocalizationService.GetLanguageDisplayName("de-DE"));
    }

    [Fact]
    public void GetLanguageDisplayName_ReturnsCodeForUnknown()
    {
        Assert.Equal("fr-FR", LocalizationService.GetLanguageDisplayName("fr-FR"));
        Assert.Equal("xx", LocalizationService.GetLanguageDisplayName("xx"));
    }

    [Fact]
    public void GetString_ReturnsFallbackForMissingKey()
    {
        var result = LocalizationService.GetString("NonExistentKey_12345");
        Assert.Equal("[NonExistentKey_12345]", result);
    }

    [Fact]
    public void GetString_ReturnsEnglishForDefaultKey()
    {
        LocalizationService.SetLanguage("en-US");
        var result = LocalizationService.GetString("AppTitle");
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.DoesNotContain("[", result);
    }

    [Fact]
    public void SetLanguage_ChangesCurrentLanguage()
    {
        LocalizationService.SetLanguage("ru-RU");
        Assert.Equal("ru-RU", LocalizationService.GetCurrentLanguage());

        LocalizationService.SetLanguage("en-US");
        Assert.Equal("en-US", LocalizationService.GetCurrentLanguage());
    }

    [Fact]
    public void LanguageChanged_Event_Fires()
    {
        var fired = false;
        LocalizationService.LanguageChanged += () => fired = true;

        LocalizationService.SetLanguage("de-DE");

        Assert.True(fired);
    }

    [Fact]
    public void SetLanguage_SetsCultureCorrectly()
    {
        LocalizationService.SetLanguage("ru-RU");

        Assert.Equal("ru-RU", System.Globalization.CultureInfo.CurrentUICulture.Name);
        Assert.Equal("ru-RU", System.Globalization.CultureInfo.CurrentCulture.Name);
    }

    [Fact]
    public void AllLanguageKeys_ReturnNonNull()
    {
        var keys = new[]
        {
            "AppTitle", "CleanTempFiles", "CleanBrowserCache",
            "CleanRecycleBin", "CleanSystemSettings", "CleanAutostart",
            "CleanSystemLogs", "CleanButton", "CancelButton",
            "LogHeader", "LanguageLabel", "ThemeLabel",
            "ConfirmTitle", "ConfirmMessage", "ConfirmButton",
            "AdminWarning", "NoAdminWarning", "LogInfo",
            "LogSuccess", "LogWarning", "LogError",
            "StartingOperation", "OperationCancelled", "CleaningComplete",
            "CleaningTempFiles", "CleaningBrowserCache", "CleaningRegistry",
            "CleaningAutostart", "CleaningSystemLogs",
            "EmptyingRecycleBin", "CreatingRestorePoint",
            "RestorePoint", "RestorePointComplete",
            "ScanInstalledApps", "InstalledAppsHeader",
            "LeftoverFiles", "HiddenFiles", "ScanButton",
            "AppPublisher", "AppSize"
        };

        LocalizationService.SetLanguage("en-US");
        foreach (var key in keys)
        {
            var value = LocalizationService.GetString(key);
            Assert.False(string.IsNullOrEmpty(value), $"Key '{key}' returned null/empty");
            Assert.DoesNotContain($"[{key}]", value);
        }
    }
}
