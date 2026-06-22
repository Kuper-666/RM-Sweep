using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RMSweep.Cleaners;
using RMSweep.Interfaces;
using RMSweep.Models;
using RMSweep.Services;

namespace RMSweep.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
{
    private readonly ISystemCleaner _cleaner;
    private readonly LogService _logService;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private bool _isCleaning;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _selectedLanguage = "en-US";

    [ObservableProperty]
    private bool _isDarkTheme = true;

    // Checkbox states
    [ObservableProperty] private bool _cleanTempFiles = true;
    [ObservableProperty] private bool _cleanBrowserCache = true;
    [ObservableProperty] private bool _cleanRecycleBin = false;
    [ObservableProperty] private bool _cleanSystemSettings = false;
    [ObservableProperty] private bool _cleanAutostart = false;
    [ObservableProperty] private bool _cleanSystemLogs = false;

    // UI strings (bound to UI, updated on language change)
    [ObservableProperty] private string _titleText = string.Empty;
    [ObservableProperty] private string _tempFilesText = string.Empty;
    [ObservableProperty] private string _browserCacheText = string.Empty;
    [ObservableProperty] private string _recycleBinText = string.Empty;
    [ObservableProperty] private string _systemSettingsText = string.Empty;
    [ObservableProperty] private string _autostartText = string.Empty;
    [ObservableProperty] private string _systemLogsText = string.Empty;
    [ObservableProperty] private string _cleanButtonText = string.Empty;
    [ObservableProperty] private string _createRestorePointText = string.Empty;
    [ObservableProperty] private string _cancelButtonText = string.Empty;
    [ObservableProperty] private string _logHeaderText = string.Empty;
    [ObservableProperty] private string _languageLabelText = string.Empty;
    [ObservableProperty] private string _themeText = string.Empty;
    [ObservableProperty] private string _confirmTitle = string.Empty;
    [ObservableProperty] private string _confirmMessage = string.Empty;
    [ObservableProperty] private string _confirmButton = string.Empty;
    [ObservableProperty] private string _adminWarningText = string.Empty;
    [ObservableProperty] private string _noAdminWarning = string.Empty;

    public ObservableCollection<LogEntry> LogEntries { get; } = new();
    public ObservableCollection<string> AvailableLanguages { get; } = new();

    public bool IsNotCleaning => !IsCleaning;

    partial void OnIsCleaningChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotCleaning));
    }

    public string PlatformInfo => _cleaner.PlatformName;

    public bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public bool IsMac => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public MainWindowViewModel()
    {
        _cleaner = CleanerFactory.Create();
        _logService = new LogService();

        _logService.EntryAdded += entry =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                LogEntries.Add(entry);
            });
        };

        LocalizationService.LanguageChanged += UpdateUILanguage;

        // Load saved settings
        var settings = SettingsService.Load();
        _cleanTempFiles = settings.CleanTemporaryFiles;
        _cleanBrowserCache = settings.CleanBrowserCache;
        _cleanRecycleBin = settings.CleanRecycleBin;
        _cleanSystemSettings = settings.CleanSystemSettings;
        _cleanAutostart = settings.CleanAutostart;
        _cleanSystemLogs = settings.CleanSystemLogs;
        _selectedLanguage = settings.Language;

        // Set initial language
        LocalizationService.SetLanguage(_selectedLanguage);

        // Populate language list
        foreach (var lang in LocalizationService.GetAvailableLanguages())
            AvailableLanguages.Add(lang);

        UpdateUILanguage();
    }

    private void UpdateUILanguage()
    {
        TitleText = LocalizationService.GetString("AppTitle");
        TempFilesText = LocalizationService.GetString("CleanTempFiles");
        BrowserCacheText = LocalizationService.GetString("CleanBrowserCache");
        RecycleBinText = LocalizationService.GetString("CleanRecycleBin");
        SystemSettingsText = LocalizationService.GetString("CleanSystemSettings");
        AutostartText = LocalizationService.GetString("CleanAutostart");
        SystemLogsText = LocalizationService.GetString("CleanSystemLogs");
        CleanButtonText = LocalizationService.GetString("CleanButton");
        CreateRestorePointText = LocalizationService.GetString("CreateRestorePoint");
        CancelButtonText = LocalizationService.GetString("CancelButton");
        LogHeaderText = LocalizationService.GetString("LogHeader");
        LanguageLabelText = LocalizationService.GetString("LanguageLabel");
        ThemeText = LocalizationService.GetString("ThemeLabel");
        ConfirmTitle = LocalizationService.GetString("ConfirmTitle");
        ConfirmMessage = LocalizationService.GetString("ConfirmMessage");
        ConfirmButton = LocalizationService.GetString("ConfirmButton");
        AdminWarningText = LocalizationService.GetString("AdminWarning");
        NoAdminWarning = LocalizationService.GetString("NoAdminWarning");
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        LocalizationService.SetLanguage(value);
        SaveSettings();
    }

    partial void OnCleanTempFilesChanged(bool value) => SaveSettings();
    partial void OnCleanBrowserCacheChanged(bool value) => SaveSettings();
    partial void OnCleanRecycleBinChanged(bool value) => SaveSettings();
    partial void OnCleanSystemSettingsChanged(bool value) => SaveSettings();
    partial void OnCleanAutostartChanged(bool value) => SaveSettings();
    partial void OnCleanSystemLogsChanged(bool value) => SaveSettings();

    private void SaveSettings()
    {
        SettingsService.Save(new AppSettings
        {
            CleanTemporaryFiles = CleanTempFiles,
            CleanBrowserCache = CleanBrowserCache,
            CleanRecycleBin = CleanRecycleBin,
            CleanSystemSettings = CleanSystemSettings,
            CleanAutostart = CleanAutostart,
            CleanSystemLogs = CleanSystemLogs,
            Language = SelectedLanguage
        });
    }

    private bool HasSelectedOperations =>
        CleanTempFiles || CleanBrowserCache || CleanRecycleBin ||
        CleanSystemSettings || CleanAutostart || CleanSystemLogs;

    [RelayCommand]
    private async Task StartCleaningAsync()
    {
        if (IsCleaning || !HasSelectedOperations) return;

        // Check admin rights
        if (!_cleaner.IsRunningAsAdmin())
        {
            _logService.AddWarning("System", NoAdminWarning);
        }

        IsCleaning = true;
        _cts = new CancellationTokenSource();
        LogEntries.Clear();
        ProgressValue = 0;

        try
        {
            var operations = GetSelectedOperations();
            var totalOps = operations.Count;
            var completedOps = 0;

            foreach (var op in operations)
            {
                if (_cts.Token.IsCancellationRequested) break;

                _logService.AddInfo(op.Key, LocalizationService.GetString("StartingOperation"));
                StatusText = op.Key;

                CleanResult result;
                try
                {
                    var progress = new Progress<CleanProgress>(p =>
                    {
                        var overallPercent = ((double)completedOps / totalOps * 100) +
                                             (p.PercentComplete / totalOps);
                        ProgressValue = Math.Min(overallPercent, 100);
                        StatusText = $"{op.Key}: {p.StatusMessage}";
                    });

                    result = await op.Value(progress, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logService.AddWarning(op.Key, LocalizationService.GetString("OperationCancelled"));
                    break;
                }
                catch (Exception ex)
                {
                    result = new CleanResult
                    {
                        OperationName = op.Key,
                        Success = false,
                        Message = ex.Message
                    };
                }

                _logService.AddResult(op.Key, result);
                completedOps++;
                ProgressValue = (double)completedOps / totalOps * 100;
            }

            StatusText = LocalizationService.GetString("CleaningComplete");
        }
        finally
        {
            IsCleaning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private async Task CreateRestorePointAsync()
    {
        if (IsCleaning) return;

        IsCleaning = true;
        _cts = new CancellationTokenSource();
        StatusText = LocalizationService.GetString("CreatingRestorePoint");

        try
        {
            var progress = new Progress<CleanProgress>(p =>
            {
                ProgressValue = p.PercentComplete;
                StatusText = p.StatusMessage;
            });

            var result = await _cleaner.CreateRestorePointAsync(progress, _cts.Token);
            _logService.AddResult(LocalizationService.GetString("RestorePoint"), result);
            StatusText = LocalizationService.GetString("RestorePointComplete");
        }
        catch (OperationCanceledException)
        {
            _logService.AddWarning("Restore Point", LocalizationService.GetString("OperationCancelled"));
        }
        catch (Exception ex)
        {
            _logService.AddError("Restore Point", ex.Message);
        }
        finally
        {
            IsCleaning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void CancelCleaning()
    {
        _cts?.Cancel();
        StatusText = LocalizationService.GetString("Cancelling");
    }

    private Dictionary<string, Func<IProgress<CleanProgress>?, CancellationToken, Task<CleanResult>>>
        GetSelectedOperations()
    {
        var ops = new Dictionary<string, Func<IProgress<CleanProgress>?, CancellationToken, Task<CleanResult>>>();

        if (CleanTempFiles)
            ops[LocalizationService.GetString("CleanTempFiles")] = _cleaner.CleanTemporaryFilesAsync;
        if (CleanBrowserCache)
            ops[LocalizationService.GetString("CleanBrowserCache")] = _cleaner.CleanBrowserCacheAsync;
        if (CleanRecycleBin)
            ops[LocalizationService.GetString("CleanRecycleBin")] = _cleaner.EmptyRecycleBinAsync;
        if (CleanSystemSettings)
            ops[LocalizationService.GetString("CleanSystemSettings")] = _cleaner.CleanSystemSettingsAsync;
        if (CleanAutostart)
            ops[LocalizationService.GetString("CleanAutostart")] = _cleaner.CleanAutostartAsync;
        if (CleanSystemLogs)
            ops[LocalizationService.GetString("CleanSystemLogs")] = _cleaner.CleanSystemLogsAsync;

        return ops;
    }
}
