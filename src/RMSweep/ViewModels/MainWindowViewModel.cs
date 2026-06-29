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

public partial class MainWindowViewModel : ViewModelBase
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

    // Tab selection
    [ObservableProperty] private int _selectedTabIndex;

    // Windows tab checkboxes
    [ObservableProperty] private bool _cleanTempFiles = true;
    [ObservableProperty] private bool _cleanBrowserCache = true;
    [ObservableProperty] private bool _cleanRecycleBin = false;
    [ObservableProperty] private bool _cleanSystemSettings = false;
    [ObservableProperty] private bool _cleanAutostart = false;
    [ObservableProperty] private bool _cleanSystemLogs = false;
    [ObservableProperty] private bool _cleanDnsCache = false;
    [ObservableProperty] private bool _cleanClipboard = false;
    [ObservableProperty] private bool _cleanRecentDocuments = false;
    [ObservableProperty] private bool _cleanThumbnailCache = false;
    [ObservableProperty] private bool _cleanMemoryDumps = false;
    [ObservableProperty] private bool _cleanChkdskFragments = false;
    [ObservableProperty] private bool _cleanWindowsUpdateCache = false;

    // Tools
    [ObservableProperty] private string _selectedDriveLetter = "C";
    [ObservableProperty] private int _selectedWipeMethod;
    [ObservableProperty] private string _scanDirectoryPath = string.Empty;
    [ObservableProperty] private string _customFolderPath = string.Empty;

    // Exclusion/Inclusion lists
    [ObservableProperty] private string _excludePathInput = string.Empty;
    [ObservableProperty] private string _includeFolderInput = string.Empty;

    // UI strings
    [ObservableProperty] private string _titleText = string.Empty;
    [ObservableProperty] private string _tempFilesText = string.Empty;
    [ObservableProperty] private string _browserCacheText = string.Empty;
    [ObservableProperty] private string _recycleBinText = string.Empty;
    [ObservableProperty] private string _systemSettingsText = string.Empty;
    [ObservableProperty] private string _autostartText = string.Empty;
    [ObservableProperty] private string _systemLogsText = string.Empty;
    [ObservableProperty] private string _dnsCacheText = string.Empty;
    [ObservableProperty] private string _clipboardText = string.Empty;
    [ObservableProperty] private string _recentDocumentsText = string.Empty;
    [ObservableProperty] private string _thumbnailCacheText = string.Empty;
    [ObservableProperty] private string _memoryDumpsText = string.Empty;
    [ObservableProperty] private string _chkdskFragmentsText = string.Empty;
    [ObservableProperty] private string _windowsUpdateCacheText = string.Empty;
    [ObservableProperty] private string _cleanButtonText = string.Empty;
    [ObservableProperty] private string _createRestorePointText = string.Empty;
    [ObservableProperty] private string _cancelButtonText = string.Empty;
    [ObservableProperty] private string _logHeaderText = string.Empty;
    [ObservableProperty] private string _languageLabelText = string.Empty;
    [ObservableProperty] private string _themeText = string.Empty;
    [ObservableProperty] private string _confirmTitle = string.Empty;
    [ObservableProperty] private string _confirmMessage = string.Empty;
    [ObservableProperty] private string _confirmButton = string.Empty;
    [ObservableProperty] private string _cancelConfirmButton = string.Empty;
    [ObservableProperty] private string _adminWarningText = string.Empty;
    [ObservableProperty] private string _noAdminWarning = string.Empty;

    [ObservableProperty] private bool _showConfirmation;
    [ObservableProperty] private bool _isNotAdmin;
    [ObservableProperty] private string _cleaningSummary = string.Empty;
    [ObservableProperty] private string _scanButtonText = string.Empty;
    [ObservableProperty] private string _installedAppsHeaderText = string.Empty;

    // Tools tab strings
    [ObservableProperty] private string _diskAnalyzerText = string.Empty;
    [ObservableProperty] private string _duplicateFinderText = string.Empty;
    [ObservableProperty] private string _driveWiperText = string.Empty;
    [ObservableProperty] private string _analyzeButton = string.Empty;
    [ObservableProperty] private string _scanDuplicatesButton = string.Empty;
    [ObservableProperty] private string _wipeButton = string.Empty;
    [ObservableProperty] private string _driveLabel = string.Empty;
    [ObservableProperty] private string _wipeMethodLabel = string.Empty;
    [ObservableProperty] private string _directoryLabel = string.Empty;
    [ObservableProperty] private string _fileShredderText = "File Shredder";
    [ObservableProperty] private string _shredItemPath = string.Empty;
    [ObservableProperty] private string _shredButtonText = "Shred File/Folder";

    // Settings tab strings
    [ObservableProperty] private string _excludePathsLabel = string.Empty;
    [ObservableProperty] private string _includeFoldersLabel = string.Empty;
    [ObservableProperty] private string _addExcludeButton = string.Empty;
    [ObservableProperty] private string _addIncludeButton = string.Empty;
    [ObservableProperty] private string _settingsHeaderText = string.Empty;

    public ObservableCollection<LogEntry> LogEntries { get; } = new();
    public ObservableCollection<string> AvailableLanguages { get; } = new();
    public ObservableCollection<InstalledApp> InstalledApps { get; } = new();
    public ObservableCollection<DiskInfo> Disks { get; } = new();
    public ObservableCollection<string> ExcludePaths { get; } = new();
    public ObservableCollection<string> IncludeFolders { get; } = new();
    public ObservableCollection<DuplicateGroup> DuplicateGroups { get; } = new();
    public ObservableCollection<DiskSpaceItem> DiskAnalysisItems { get; } = new();
    public ObservableCollection<string> AvailableDrives { get; } = new();
    public ObservableCollection<string> WipeMethods { get; } = new();

    public bool IsNotCleaning => !IsCleaning;
    public bool CanShowCleanButton => !IsCleaning && !ShowConfirmation;

    partial void OnIsCleaningChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotCleaning));
        OnPropertyChanged(nameof(CanShowCleanButton));
    }

    partial void OnShowConfirmationChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotConfirming));
        OnPropertyChanged(nameof(CanShowCleanButton));
    }

    public bool IsNotConfirming => !ShowConfirmation;

    public string PlatformInfo => _cleaner.PlatformName;

    public bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public bool IsMac => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public MainWindowViewModel()
    {
        _cleaner = CleanerFactory.Create();
        _logService = new LogService();
        _isNotAdmin = !_cleaner.IsRunningAsAdmin();

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
        _cleanDnsCache = settings.CleanDnsCache;
        _cleanClipboard = settings.CleanClipboard;
        _cleanRecentDocuments = settings.CleanRecentDocuments;
        _cleanThumbnailCache = settings.CleanThumbnailCache;
        _cleanMemoryDumps = settings.CleanMemoryDumps;
        _cleanChkdskFragments = settings.CleanChkdskFragments;
        _cleanWindowsUpdateCache = settings.CleanWindowsUpdateCache;
        _selectedLanguage = settings.Language;

        foreach (var path in settings.ExcludePaths)
            ExcludePaths.Add(path);
        foreach (var folder in settings.IncludeCustomFolders)
            IncludeFolders.Add(folder);

        LocalizationService.SetLanguage(_selectedLanguage);

        foreach (var lang in LocalizationService.GetAvailableLanguages())
            AvailableLanguages.Add(lang);

        WipeMethods.Add("Zero Fill");
        WipeMethods.Add("DoD 5220.22-M");
        WipeMethods.Add("Gutmann (35 passes)");

        LoadDiskInfo();
        UpdateUILanguage();
    }

    private void LoadDiskInfo()
    {
        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;
                Disks.Add(new DiskInfo
                {
                    Name = drive.Name,
                    Label = drive.VolumeLabel,
                    TotalBytes = drive.TotalSize,
                    FreeBytes = drive.AvailableFreeSpace
                });
                AvailableDrives.Add(drive.Name.TrimEnd('\\'));
            }
        }
        catch { }
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
        DnsCacheText = LocalizationService.GetString("CleanDnsCache");
        ClipboardText = LocalizationService.GetString("CleanClipboard");
        RecentDocumentsText = LocalizationService.GetString("CleanRecentDocuments");
        ThumbnailCacheText = LocalizationService.GetString("CleanThumbnailCache");
        MemoryDumpsText = LocalizationService.GetString("CleanMemoryDumps");
        ChkdskFragmentsText = LocalizationService.GetString("CleanChkdskFragments");
        WindowsUpdateCacheText = LocalizationService.GetString("CleanWindowsUpdateCache");
        CleanButtonText = LocalizationService.GetString("CleanButton");
        CreateRestorePointText = LocalizationService.GetString("CreateRestorePoint");
        CancelButtonText = LocalizationService.GetString("CancelButton");
        LogHeaderText = LocalizationService.GetString("LogHeader");
        LanguageLabelText = LocalizationService.GetString("LanguageLabel");
        ThemeText = LocalizationService.GetString("ThemeLabel");
        ConfirmTitle = LocalizationService.GetString("ConfirmTitle");
        ConfirmMessage = LocalizationService.GetString("ConfirmMessage");
        ConfirmButton = LocalizationService.GetString("ConfirmButton");
        CancelConfirmButton = LocalizationService.GetString("CancelConfirmButton");
        AdminWarningText = LocalizationService.GetString("AdminWarning");
        NoAdminWarning = LocalizationService.GetString("NoAdminWarning");
        ScanButtonText = LocalizationService.GetString("ScanButton");
        InstalledAppsHeaderText = LocalizationService.GetString("InstalledAppsHeader");
        DiskAnalyzerText = LocalizationService.GetString("DiskAnalyzer");
        DuplicateFinderText = LocalizationService.GetString("DuplicateFinder");
        DriveWiperText = LocalizationService.GetString("DriveWiper");
        AnalyzeButton = LocalizationService.GetString("AnalyzeButton");
        ScanDuplicatesButton = LocalizationService.GetString("ScanDuplicatesButton");
        WipeButton = LocalizationService.GetString("WipeButton");
        DriveLabel = LocalizationService.GetString("DriveLabel");
        WipeMethodLabel = LocalizationService.GetString("WipeMethodLabel");
        DirectoryLabel = LocalizationService.GetString("DirectoryLabel");
        FileShredderText = LocalizationService.GetString("FileShredder");
        ShredButtonText = LocalizationService.GetString("ShredButton");
        ExcludePathsLabel = LocalizationService.GetString("ExcludePathsLabel");
        IncludeFoldersLabel = LocalizationService.GetString("IncludeFoldersLabel");
        AddExcludeButton = LocalizationService.GetString("AddExcludeButton");
        AddIncludeButton = LocalizationService.GetString("AddIncludeButton");
        SettingsHeaderText = LocalizationService.GetString("SettingsHeader");
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
    partial void OnCleanDnsCacheChanged(bool value) => SaveSettings();
    partial void OnCleanClipboardChanged(bool value) => SaveSettings();
    partial void OnCleanRecentDocumentsChanged(bool value) => SaveSettings();
    partial void OnCleanThumbnailCacheChanged(bool value) => SaveSettings();
    partial void OnCleanMemoryDumpsChanged(bool value) => SaveSettings();
    partial void OnCleanChkdskFragmentsChanged(bool value) => SaveSettings();
    partial void OnCleanWindowsUpdateCacheChanged(bool value) => SaveSettings();

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
            CleanDnsCache = CleanDnsCache,
            CleanClipboard = CleanClipboard,
            CleanRecentDocuments = CleanRecentDocuments,
            CleanThumbnailCache = CleanThumbnailCache,
            CleanMemoryDumps = CleanMemoryDumps,
            CleanChkdskFragments = CleanChkdskFragments,
            CleanWindowsUpdateCache = CleanWindowsUpdateCache,
            Language = SelectedLanguage,
            ExcludePaths = ExcludePaths.ToList(),
            IncludeCustomFolders = IncludeFolders.ToList()
        });
    }

    private bool HasSelectedOperations =>
        CleanTempFiles || CleanBrowserCache || CleanRecycleBin ||
        CleanSystemSettings || CleanAutostart || CleanSystemLogs ||
        CleanDnsCache || CleanClipboard || CleanRecentDocuments ||
        CleanThumbnailCache || CleanMemoryDumps || CleanChkdskFragments ||
        CleanWindowsUpdateCache || IncludeFolders.Count > 0;

    [RelayCommand]
    private void ShowCleaningConfirmation()
    {
        if (IsCleaning || !HasSelectedOperations) return;
        ShowConfirmation = true;
    }

    [RelayCommand]
    private void CancelCleaningConfirmation()
    {
        ShowConfirmation = false;
    }

    [RelayCommand]
    private async Task StartCleaningAsync()
    {
        if (IsCleaning || !HasSelectedOperations) return;

        ShowConfirmation = false;
        CleaningSummary = string.Empty;

        if (!_cleaner.IsRunningAsAdmin())
        {
            _logService.AddWarning("System", NoAdminWarning);
        }

        IsCleaning = true;
        _cts = new CancellationTokenSource();
        LogEntries.Clear();
        ProgressValue = 0;

        var successCount = 0;
        var failCount = 0;

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
                if (result.Success) successCount++;
                else failCount++;
                completedOps++;
                ProgressValue = (double)completedOps / totalOps * 100;
            }

            var summary = failCount == 0
                ? $"Completed: {successCount} succeeded"
                : $"Completed: {successCount} succeeded, {failCount} failed";
            _logService.AddWarning("Summary", summary);
            CleaningSummary = summary;
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
        if (CleanDnsCache)
            ops[LocalizationService.GetString("CleanDnsCache")] = _cleaner.CleanDnsCacheAsync;
        if (CleanClipboard)
            ops[LocalizationService.GetString("CleanClipboard")] = _cleaner.CleanClipboardAsync;
        if (CleanRecentDocuments)
            ops[LocalizationService.GetString("CleanRecentDocuments")] = _cleaner.CleanRecentDocumentsAsync;
        if (CleanThumbnailCache)
            ops[LocalizationService.GetString("CleanThumbnailCache")] = _cleaner.CleanThumbnailCacheAsync;
        if (CleanMemoryDumps)
            ops[LocalizationService.GetString("CleanMemoryDumps")] = _cleaner.CleanMemoryDumpsAsync;
        if (CleanChkdskFragments)
            ops[LocalizationService.GetString("CleanChkdskFragments")] = _cleaner.CleanChkdskFragmentsAsync;
        if (CleanWindowsUpdateCache)
            ops[LocalizationService.GetString("CleanWindowsUpdateCache")] = _cleaner.CleanWindowsUpdateCacheAsync;

        if (IncludeFolders.Count > 0)
        {
            var folders = IncludeFolders.ToList();
            ops[LocalizationService.GetString("CustomFolders")] = (p, ct) =>
                _cleaner.CleanCustomFoldersAsync(folders, p, ct);
        }

        return ops;
    }

    [RelayCommand]
    private async Task ScanInstalledAppsAsync()
    {
        if (IsCleaning) return;

        IsCleaning = true;
        _cts = new CancellationTokenSource();
        LogEntries.Clear();
        InstalledApps.Clear();
        StatusText = LocalizationService.GetString("ScanningInstalledApps");

        try
        {
            var progress = new Progress<CleanProgress>(p =>
            {
                ProgressValue = p.PercentComplete;
                StatusText = p.StatusMessage;
            });

            var apps = await _cleaner.ScanInstalledAppsAsync(progress, _cts.Token);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                foreach (var app in apps)
                    InstalledApps.Add(app);
            });

            var totalLeftovers = apps.Sum(a => a.Leftovers.Count);
            var hiddenFiles = apps.Sum(a => a.Leftovers.Count(l => l.IsHidden));
            _logService.AddSuccess("Scan", $"Found {apps.Count} apps, {totalLeftovers} leftover items ({hiddenFiles} hidden)");
            StatusText = $"Found {apps.Count} apps, {totalLeftovers} leftovers";
        }
        catch (OperationCanceledException)
        {
            _logService.AddWarning("Scan", LocalizationService.GetString("OperationCancelled"));
        }
        catch (Exception ex)
        {
            _logService.AddError("Scan", ex.Message);
        }
        finally
        {
            IsCleaning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private async Task UninstallAppAsync(InstalledApp? app)
    {
        if (app == null || IsCleaning) return;

        IsCleaning = true;
        _cts = new CancellationTokenSource();
        StatusText = $"Uninstalling {app.Name}...";

        try
        {
            _logService.AddInfo(app.Name, "Starting uninstall + leftover cleanup...");

            var result = await _cleaner.UninstallAppAsync(app, _cts.Token);

            if (result)
            {
                _logService.AddSuccess(app.Name, "Uninstalled and cleaned up successfully");
                Avalonia.Threading.Dispatcher.UIThread.Post(() => InstalledApps.Remove(app));
            }
            else
            {
                _logService.AddWarning(app.Name, "Uninstall completed, some leftovers may remain");
            }

            StatusText = result ? $"Removed: {app.Name}" : $"Done: {app.Name}";
        }
        catch (OperationCanceledException)
        {
            _logService.AddWarning(app.Name, LocalizationService.GetString("OperationCancelled"));
        }
        catch (Exception ex)
        {
            _logService.AddError(app.Name, ex.Message);
        }
        finally
        {
            IsCleaning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private async Task AnalyzeDiskSpaceAsync()
    {
        if (IsCleaning) return;
        var dirPath = ScanDirectoryPath;
        if (string.IsNullOrWhiteSpace(dirPath) || !Directory.Exists(dirPath)) return;

        IsCleaning = true;
        _cts = new CancellationTokenSource();
        DiskAnalysisItems.Clear();
        StatusText = LocalizationService.GetString("AnalyzingDisk");

        try
        {
            var progress = new Progress<CleanProgress>(p =>
            {
                ProgressValue = p.PercentComplete;
                StatusText = p.StatusMessage;
            });

            var items = await _cleaner.AnalyzeDiskSpaceAsync(dirPath, progress, _cts.Token);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                foreach (var item in items)
                    DiskAnalysisItems.Add(item);
            });

            _logService.AddSuccess("Disk Analysis",
                $"Found {items.Count} file types, total: {FormatBytes(items.Sum(i => i.TotalSize))}");
            StatusText = $"Analysis complete: {items.Count} file types found";
        }
        catch (OperationCanceledException)
        {
            _logService.AddWarning("Disk Analysis", LocalizationService.GetString("OperationCancelled"));
        }
        catch (Exception ex)
        {
            _logService.AddError("Disk Analysis", ex.Message);
        }
        finally
        {
            IsCleaning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private async Task ScanDuplicatesAsync()
    {
        if (IsCleaning) return;
        var dirPath = ScanDirectoryPath;
        if (string.IsNullOrWhiteSpace(dirPath) || !Directory.Exists(dirPath)) return;

        IsCleaning = true;
        _cts = new CancellationTokenSource();
        DuplicateGroups.Clear();
        StatusText = LocalizationService.GetString("ScanningDuplicates");

        try
        {
            var progress = new Progress<CleanProgress>(p =>
            {
                ProgressValue = p.PercentComplete;
                StatusText = p.StatusMessage;
            });

            var groups = await _cleaner.ScanForDuplicatesAsync(dirPath, progress, _cts.Token);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                foreach (var group in groups)
                    DuplicateGroups.Add(group);
            });

            var totalDupes = groups.Sum(g => g.Count - 1);
            var wastedBytes = groups.Sum(g => g.FileSize * (g.Count - 1));
            _logService.AddSuccess("Duplicates",
                $"Found {groups.Count} duplicate groups, {totalDupes} extra files, {FormatBytes(wastedBytes)} wasted");
            StatusText = $"Found {groups.Count} duplicate groups";
        }
        catch (OperationCanceledException)
        {
            _logService.AddWarning("Duplicates", LocalizationService.GetString("OperationCancelled"));
        }
        catch (Exception ex)
        {
            _logService.AddError("Duplicates", ex.Message);
        }
        finally
        {
            IsCleaning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private async Task WipeDriveAsync()
    {
        if (IsCleaning) return;
        var drive = SelectedDriveLetter;
        if (string.IsNullOrWhiteSpace(drive)) return;

        IsCleaning = true;
        _cts = new CancellationTokenSource();
        StatusText = $"Wiping free space on {drive}...";

        try
        {
            var method = SelectedWipeMethod switch
            {
                1 => DriveWipeMethod.DoD522022M,
                2 => DriveWipeMethod.Gutmann,
                _ => DriveWipeMethod.ZeroFill
            };

            var progress = new Progress<CleanProgress>(p =>
            {
                ProgressValue = p.PercentComplete;
                StatusText = p.StatusMessage;
            });

            var result = await _cleaner.WipeDriveFreeSpaceAsync(drive, method, progress, _cts.Token);
            _logService.AddResult("Drive Wiper", result);
            StatusText = result.Success ? $"Drive {drive} wiped successfully" : $"Wipe failed: {result.Message}";
        }
        catch (OperationCanceledException)
        {
            _logService.AddWarning("Drive Wiper", LocalizationService.GetString("OperationCancelled"));
        }
        catch (Exception ex)
        {
            _logService.AddError("Drive Wiper", ex.Message);
        }
        finally
        {
            IsCleaning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private async Task ShredItemAsync()
    {
        if (IsCleaning) return;
        var path = ShredItemPath;
        if (string.IsNullOrWhiteSpace(path)) return;

        IsCleaning = true;
        _cts = new CancellationTokenSource();
        StatusText = $"Shredding {path}...";

        try
        {
            var method = SelectedWipeMethod switch
            {
                1 => DriveWipeMethod.DoD522022M,
                2 => DriveWipeMethod.Gutmann,
                _ => DriveWipeMethod.ZeroFill
            };

            var progress = new Progress<CleanProgress>(p =>
            {
                ProgressValue = p.PercentComplete;
                StatusText = p.StatusMessage;
            });

            var result = await _cleaner.ShredItemAsync(path, method, progress, _cts.Token);
            _logService.AddResult("File Shredder", result);
            StatusText = result.Success ? "Shredding completed successfully" : $"Shredding failed: {result.Message}";
        }
        catch (OperationCanceledException)
        {
            _logService.AddWarning("File Shredder", LocalizationService.GetString("OperationCancelled"));
        }
        catch (Exception ex)
        {
            _logService.AddError("File Shredder", ex.Message);
        }
        finally
        {
            IsCleaning = false;
            _cts?.Dispose();
            _cts = null;
            ShredItemPath = string.Empty;
        }
    }

    [RelayCommand]
    private void AddExcludePath()
    {
        if (!string.IsNullOrWhiteSpace(ExcludePathInput) && !ExcludePaths.Contains(ExcludePathInput))
        {
            ExcludePaths.Add(ExcludePathInput);
            ExcludePathInput = string.Empty;
            SaveSettings();
        }
    }

    [RelayCommand]
    private void RemoveExcludePath(string? path)
    {
        if (path != null && ExcludePaths.Contains(path))
        {
            ExcludePaths.Remove(path);
            SaveSettings();
        }
    }

    [RelayCommand]
    private void AddIncludeFolder()
    {
        if (!string.IsNullOrWhiteSpace(IncludeFolderInput) && !IncludeFolders.Contains(IncludeFolderInput))
        {
            IncludeFolders.Add(IncludeFolderInput);
            IncludeFolderInput = string.Empty;
            SaveSettings();
        }
    }

    [RelayCommand]
    private void RemoveIncludeFolder(string? folder)
    {
        if (folder != null && IncludeFolders.Contains(folder))
        {
            IncludeFolders.Remove(folder);
            SaveSettings();
        }
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
        _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
    };
}
