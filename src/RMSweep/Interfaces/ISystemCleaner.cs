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
}
