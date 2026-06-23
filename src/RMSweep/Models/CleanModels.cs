using System.Globalization;

namespace RMSweep.Models;

public enum CleanOperation
{
    TemporaryFiles,
    BrowserCache,
    SystemSettings,
    Autostart,
    RecycleBin,
    SystemLogs,
    DnsCache,
    Clipboard,
    RecentDocuments,
    ThumbnailCache,
    MemoryDumps,
    ChkdskFragments,
    WindowsUpdateCache
}

public class CleanResult
{
    public bool Success { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public long BytesFreed { get; set; }
    public int FilesDeleted { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class CleanProgress
{
    public double PercentComplete { get; set; }
    public string CurrentOperation { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public LogLevel Level { get; set; } = LogLevel.Info;
}

public enum LogLevel
{
    Info,
    Warning,
    Error,
    Success
}

public class AppSettings
{
    public bool CleanTemporaryFiles { get; set; } = true;
    public bool CleanBrowserCache { get; set; } = true;
    public bool CleanRecycleBin { get; set; } = false;
    public bool CleanSystemSettings { get; set; } = false;
    public bool CleanAutostart { get; set; } = false;
    public bool CleanSystemLogs { get; set; } = false;
    public bool CleanDnsCache { get; set; } = false;
    public bool CleanClipboard { get; set; } = false;
    public bool CleanRecentDocuments { get; set; } = false;
    public bool CleanThumbnailCache { get; set; } = false;
    public bool CleanMemoryDumps { get; set; } = false;
    public bool CleanChkdskFragments { get; set; } = false;
    public bool CleanWindowsUpdateCache { get; set; } = false;
    public bool ScanInstalledApps { get; set; } = true;
    public string Language { get; set; } = "en-US";
    public List<string> ExcludePaths { get; set; } = new();
    public List<string> ExcludeRegistryKeys { get; set; } = new();
    public List<string> IncludeCustomFolders { get; set; } = new();
    public bool SecureDelete { get; set; } = false;
    public int SecureDeletePasses { get; set; } = 3;
}

public class InstalledApp
{
    public string Name { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string InstallLocation { get; set; } = string.Empty;
    public string UninstallString { get; set; } = string.Empty;
    public long EstimatedSize { get; set; }
    public List<LeftoverFile> Leftovers { get; set; } = new();
}

public class DiskInfo
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public long FreeBytes { get; set; }
    public long UsedBytes => TotalBytes - FreeBytes;
    public double UsedPercent => TotalBytes > 0 ? (double)UsedBytes / TotalBytes * 100 : 0;
    public string TotalFormatted => FormatBytes(TotalBytes);
    public string FreeFormatted => FormatBytes(FreeBytes);
    public string UsedFormatted => FormatBytes(UsedBytes);
    public string UsageFormatted => $"{FormatBytes(UsedBytes)} / {FormatBytes(TotalBytes)}";

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => string.Format(CultureInfo.InvariantCulture, "{0:F1} KB", bytes / 1024.0),
        < 1024 * 1024 * 1024 => string.Format(CultureInfo.InvariantCulture, "{0:F1} MB", bytes / (1024.0 * 1024.0)),
        _ => string.Format(CultureInfo.InvariantCulture, "{0:F2} GB", bytes / (1024.0 * 1024.0 * 1024.0))
    };
}

public class LeftoverFile
{
    public string Path { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    public bool IsSystem { get; set; }
    public long Size { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class DuplicateItem
{
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
}

public class DuplicateGroup
{
    public string Hash { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int Count { get; set; }
    public List<DuplicateItem> Items { get; set; } = new();
}

public class DiskSpaceItem
{
    public string Extension { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public int FileCount { get; set; }
    public string SizeFormatted => FormatBytes(TotalSize);

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => string.Format(CultureInfo.InvariantCulture, "{0:F1} KB", bytes / 1024.0),
        < 1024 * 1024 * 1024 => string.Format(CultureInfo.InvariantCulture, "{0:F1} MB", bytes / (1024.0 * 1024.0)),
        _ => string.Format(CultureInfo.InvariantCulture, "{0:F2} GB", bytes / (1024.0 * 1024.0 * 1024.0))
    };
}

public enum DriveWipeMethod
{
    ZeroFill,
    DoD522022M,
    Gutmann
}
