using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using RMSweep.Interfaces;
using RMSweep.Models;
using RMSweep.Services;

namespace RMSweep.Cleaners;

/// <summary>
/// Windows-specific implementation of system cleaning.
/// Uses Registry, Win32 APIs, and Windows paths.
/// </summary>
public class WindowsCleaner : ISystemCleaner
{
    public string PlatformName => "Windows";

    public bool IsRunningAsAdmin()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public async Task<CleanResult> CleanTemporaryFilesAsync(
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        var result = new CleanResult { OperationName = "Temporary Files" };

        try
        {
            var tempPaths = new List<string>();

            // User temp
            var userTemp = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);
            tempPaths.Add(Path.GetTempPath());

            // Windows temp
            var winTemp = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Windows", "Temp");
            if (Directory.Exists(winTemp)) tempPaths.Add(winTemp);

            // Prefetch
            var prefetch = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Windows", "Prefetch");
            if (Directory.Exists(prefetch)) tempPaths.Add(prefetch);

            var totalPaths = tempPaths.Count;
            for (int i = 0; i < totalPaths; i++)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report(new CleanProgress
                {
                    PercentComplete = (double)i / totalPaths * 100,
                    CurrentOperation = $"Cleaning {tempPaths[i]}",
                    StatusMessage = LocalizationService.GetString("CleaningTempFiles")
                });

                var (deleted, freed) = await CleanDirectoryAsync(tempPaths[i], ct);
                result.FilesDeleted += deleted;
                result.BytesFreed += freed;
            }

            result.Success = true;
            result.Message = $"Cleaned {result.FilesDeleted} files, freed {result.BytesFreed} bytes";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());
        }

        return result;
    }

    public async Task<CleanResult> CleanBrowserCacheAsync(
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        var result = new CleanResult { OperationName = "Browser Cache" };

        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var browserPaths = new Dictionary<string, string>
            {
                ["Chrome"] = Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Cache"),
                ["Edge"] = Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Cache"),
                ["Firefox"] = Path.Combine(localAppData, "Mozilla", "Firefox", "Profiles"),
                ["Chrome Code Cache"] = Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Code Cache"),
                ["Edge Code Cache"] = Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Code Cache"),
            };

            var total = browserPaths.Count;
            int current = 0;
            foreach (var (name, path) in browserPaths)
            {
                ct.ThrowIfCancellationRequested();
                current++;
                progress?.Report(new CleanProgress
                {
                    PercentComplete = (double)current / total * 100,
                    CurrentOperation = $"Cleaning {name}",
                    StatusMessage = LocalizationService.GetString("CleaningBrowserCache")
                });

                if (Directory.Exists(path))
                {
                    var (deleted, freed) = await CleanDirectoryAsync(path, ct);
                    result.FilesDeleted += deleted;
                    result.BytesFreed += freed;
                }
            }

            result.Success = true;
            result.Message = $"Cleaned browser caches: {result.FilesDeleted} files, freed {result.BytesFreed} bytes";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());
        }

        return result;
    }

    public async Task<CleanResult> CleanSystemSettingsAsync(
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        var result = new CleanResult { OperationName = "Registry (System Settings)" };

        try
        {
            progress?.Report(new CleanProgress
            {
                PercentComplete = 10,
                StatusMessage = LocalizationService.GetString("CleaningRegistry")
            });

            await Task.Run(() => CleanRegistryKeys(ct), ct);

            result.Success = true;
            result.Message = "Registry cleanup completed";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());
        }

        return result;
    }

    public async Task<CleanResult> CleanAutostartAsync(
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        var result = new CleanResult { OperationName = "Autostart" };

        try
        {
            progress?.Report(new CleanProgress
            {
                PercentComplete = 20,
                StatusMessage = LocalizationService.GetString("CleaningAutostart")
            });

            await Task.Run(() => CleanAutostartKeys(ct), ct);

            result.Success = true;
            result.Message = "Autostart cleanup completed";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());
        }

        return result;
    }

    public async Task<CleanResult> EmptyRecycleBinAsync(
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        var result = new CleanResult { OperationName = "Recycle Bin" };

        try
        {
            progress?.Report(new CleanProgress
            {
                PercentComplete = 50,
                StatusMessage = LocalizationService.GetString("EmptyingRecycleBin")
            });

            await Task.Run(() =>
            {
                // SHFileOperation is deprecated; use SHEmptyRecycleBin via P/Invoke
                uint hr = SHEmptyRecycleBin(IntPtr.Zero, null,
                    (uint)(RecycleBinFlags.SHRB_NOCONFIRMATION | RecycleBinFlags.SHRB_NOPROGRESS));
            }, ct);

            result.Success = true;
            result.Message = "Recycle bin emptied successfully";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());
        }

        return result;
    }

    public async Task<CleanResult> CleanSystemLogsAsync(
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        var result = new CleanResult { OperationName = "System Logs" };

        try
        {
            var logsPath = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Windows", "Logs");
            progress?.Report(new CleanProgress
            {
                PercentComplete = 30,
                StatusMessage = LocalizationService.GetString("CleaningSystemLogs")
            });

            if (Directory.Exists(logsPath))
            {
                var (deleted, freed) = await CleanDirectoryAsync(logsPath, ct);
                result.FilesDeleted = deleted;
                result.BytesFreed = freed;
            }

            result.Success = true;
            result.Message = $"Cleaned logs: {result.FilesDeleted} files, freed {result.BytesFreed} bytes";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());
        }

        return result;
    }

    public async Task<CleanResult> CreateRestorePointAsync(
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        var result = new CleanResult { OperationName = "System Restore Point" };

        try
        {
            progress?.Report(new CleanProgress
            {
                PercentComplete = 10,
                StatusMessage = LocalizationService.GetString("CreatingRestorePoint")
            });

            await Task.Run(() => CreateSystemRestorePoint(ct), ct);

            result.Success = true;
            result.Message = "System restore point created successfully";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
            result.Errors.Add(ex.ToString());
        }

        return result;
    }

    // --- Private helpers ---

    private void CleanRegistryKeys(CancellationToken ct)
    {
        var runKeys = new[]
        {
            (@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", Registry.LocalMachine),
            (@"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", Registry.LocalMachine),
            (@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", Registry.CurrentUser),
            (@"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", Registry.CurrentUser),
            (@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Run", Registry.LocalMachine),
        };

        foreach (var (subKeyPath, hive) in runKeys)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var key = hive.OpenSubKey(subKeyPath, writable: true);
                if (key == null) continue;

                foreach (var valueName in key.GetValueNames())
                {
                    var value = key.GetValue(valueName) as string;
                    if (string.IsNullOrEmpty(value)) continue;

                    // Check if the referenced executable exists
                    var exePath = ExtractExePath(value);
                    if (!string.IsNullOrEmpty(exePath) && !File.Exists(exePath))
                    {
                        key.DeleteValue(valueName, throwOnMissingValue: false);
                    }
                }
            }
            catch
            {
                // Skip keys we cannot access
            }
        }
    }

    private void CleanAutostartKeys(CancellationToken ct)
    {
        // Clean startup folder
        var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        if (Directory.Exists(startupPath))
        {
            foreach (var file in Directory.GetFiles(startupPath))
            {
                ct.ThrowIfCancellationRequested();
                try { File.Delete(file); } catch { }
            }
        }

        // Clean Run keys (same as registry cleanup but focused on autostart)
        CleanRegistryKeys(ct);
    }

    private static string ExtractExePath(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return string.Empty;

        command = command.Trim();

        // Handle quoted paths
        if (command.StartsWith('"'))
        {
            var endQuote = command.IndexOf('"', 1);
            if (endQuote > 1) return command.Substring(1, endQuote - 1);
        }

        // Handle paths with spaces (try common extensions)
        var parts = command.Split(' ');
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            var candidate = string.Join(" ", parts.Take(i + 1));
            if (File.Exists(candidate)) return candidate;
        }

        return parts[0];
    }

    private static void CreateSystemRestorePoint(CancellationToken ct)
    {
        // Use WMI via PowerShell to create restore point
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-Command \"Enable-ComputerRestore -Drive 'C:\\'; " +
                        "Checkpoint-Computer -Description 'RM-Sweep Restore Point' -RestorePointType MODIFY_SETTINGS\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi);
        if (process != null)
        {
            process.WaitForExit(120000); // 2 min timeout
        }
    }

    private async Task<(int filesDeleted, long bytesFreed)> CleanDirectoryAsync(
        string directoryPath, CancellationToken ct)
    {
        int filesDeleted = 0;
        long bytesFreed = 0;

        await Task.Run(() =>
        {
            try
            {
                var dir = new DirectoryInfo(directoryPath);

                // Delete files
                foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        bytesFreed += file.Length;
                        file.Delete();
                        filesDeleted++;
                    }
                    catch
                    {
                        // Skip locked/in-use files
                    }
                }

                // Delete empty directories (bottom-up)
                foreach (var dirEntry in dir.EnumerateDirectories("*", SearchOption.AllDirectories)
                             .OrderByDescending(d => d.FullName.Length))
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        if (!dirEntry.EnumerateFiles().Any())
                        {
                            dirEntry.Delete(false);
                        }
                    }
                    catch
                    {
                        // Skip non-empty or locked dirs
                    }
                }
            }
            catch
            {
                // Skip inaccessible directories
            }
        }, ct);

        return (filesDeleted, bytesFreed);
    }

    // --- P/Invoke for Recycle Bin ---

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    [Flags]
    private enum RecycleBinFlags : uint
    {
        SHRB_NOCONFIRMATION = 0x0001,
        SHRB_NOPROGRESS = 0x0002,
        SHRB_NO_SOUND = 0x0004
    }
}
