using RMSweep.Models;

namespace RMSweep.Interfaces;

/// <summary>
/// Strategy interface for platform-specific system cleaning operations.
/// </summary>
public interface ISystemCleaner
{
    /// <summary>Platform display name.</summary>
    string PlatformName { get; }

    /// <summary>Clean temporary files (system and user).</summary>
    Task<CleanResult> CleanTemporaryFilesAsync(IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Clean browser caches (Chrome, Edge, Firefox).</summary>
    Task<CleanResult> CleanBrowserCacheAsync(IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Clean system registry (Windows) or preferences/plist (Mac).</summary>
    Task<CleanResult> CleanSystemSettingsAsync(IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Clean autostart entries.</summary>
    Task<CleanResult> CleanAutostartAsync(IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Empty recycle bin / trash.</summary>
    Task<CleanResult> EmptyRecycleBinAsync(IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Clean system logs.</summary>
    Task<CleanResult> CleanSystemLogsAsync(IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Create a restore point or backup.</summary>
    Task<CleanResult> CreateRestorePointAsync(IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Scan installed applications and find leftover/hidden files.</summary>
    Task<List<InstalledApp>> ScanInstalledAppsAsync(IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Uninstall an application.</summary>
    Task<bool> UninstallAppAsync(InstalledApp app, CancellationToken ct = default);

    /// <summary>Check if running with admin privileges.</summary>
    bool IsRunningAsAdmin();

    /// <summary>Flush DNS cache.</summary>
    Task<CleanResult> CleanDnsCacheAsync(IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Clear clipboard contents.</summary>
    Task<CleanResult> CleanClipboardAsync(IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Clear recent documents history.</summary>
    Task<CleanResult> CleanRecentDocumentsAsync(IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Clear thumbnail/icon cache.</summary>
    Task<CleanResult> CleanThumbnailCacheAsync(IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Delete memory dumps and crash reports.</summary>
    Task<CleanResult> CleanMemoryDumpsAsync(IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Delete Chkdsk file fragments.</summary>
    Task<CleanResult> CleanChkdskFragmentsAsync(IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Clean Windows Update download cache.</summary>
    Task<CleanResult> CleanWindowsUpdateCacheAsync(IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Securely wipe free space on a drive.</summary>
    Task<CleanResult> WipeDriveFreeSpaceAsync(string driveLetter, DriveWipeMethod method, IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Scan a directory for duplicate files.</summary>
    Task<List<DuplicateGroup>> ScanForDuplicatesAsync(string directoryPath, IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Analyze disk space usage by file extension.</summary>
    Task<List<DiskSpaceItem>> AnalyzeDiskSpaceAsync(string directoryPath, IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Clean with custom paths (included folders from settings).</summary>
    Task<CleanResult> CleanCustomFoldersAsync(List<string> folders, IProgress<CleanProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Securely shred a file or directory.</summary>
    Task<CleanResult> ShredItemAsync(string path, DriveWipeMethod method, IProgress<CleanProgress>? progress = null, CancellationToken ct = default);
}
