using RMSweep.Models;

namespace RMSweep.Services;

/// <summary>
/// Thread-safe log collector for cleaning operations.
/// </summary>
public class LogService
{
    private readonly List<LogEntry> _entries = new();
    private readonly object _lock = new();

    public event Action<LogEntry>? EntryAdded;

    public IReadOnlyList<LogEntry> Entries
    {
        get { lock (_lock) return _entries.ToList(); }
    }

    public void AddInfo(string operation, string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Operation = operation,
            Status = LocalizationService.GetString("LogInfo"),
            Details = message,
            Level = LogLevel.Info
        };
        Add(entry);
    }

    public void AddSuccess(string operation, string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Operation = operation,
            Status = LocalizationService.GetString("LogSuccess"),
            Details = message,
            Level = LogLevel.Success
        };
        Add(entry);
    }

    public void AddWarning(string operation, string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Operation = operation,
            Status = LocalizationService.GetString("LogWarning"),
            Details = message,
            Level = LogLevel.Warning
        };
        Add(entry);
    }

    public void AddError(string operation, string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Operation = operation,
            Status = LocalizationService.GetString("LogError"),
            Details = message,
            Level = LogLevel.Error
        };
        Add(entry);
    }

    public void AddResult(string operation, CleanResult result)
    {
        if (result.Success)
        {
            var details = $"{result.Message} ({result.FilesDeleted} files, {FormatBytes(result.BytesFreed)})";
            AddSuccess(operation, details);
        }
        else
        {
            AddError(operation, result.Message);
            foreach (var error in result.Errors)
                AddError(operation, error);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
    }

    private void Add(LogEntry entry)
    {
        lock (_lock)
        {
            _entries.Add(entry);
        }
        EntryAdded?.Invoke(entry);
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
        _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
    };
}
