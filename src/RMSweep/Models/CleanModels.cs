namespace RMSweep.Models;

public enum CleanOperation
{
    TemporaryFiles,
    BrowserCache,
    SystemSettings,
    Autostart,
    RecycleBin,
    SystemLogs
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
    public bool ScanInstalledApps { get; set; } = true;
    public string Language { get; set; } = "en-US";
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
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
        _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
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
