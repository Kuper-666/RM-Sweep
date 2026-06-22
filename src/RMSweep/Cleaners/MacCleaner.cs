using System.Diagnostics;
using System.Runtime.InteropServices;
using RMSweep.Interfaces;
using RMSweep.Models;
using RMSweep.Services;

namespace RMSweep.Cleaners;

/// <summary>
/// macOS-specific implementation of system cleaning.
/// Cleans caches, preferences, application support, and trash.
/// </summary>
public class MacCleaner : ISystemCleaner
{
    public string PlatformName => "macOS";

    public bool IsRunningAsAdmin()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return false;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-c \"id -u\"",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using var process = Process.Start(psi);
            var output = process?.StandardOutput.ReadToEnd().Trim();
            process?.WaitForExit();
            return output == "0"; // root UID
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
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var tempPaths = new List<string>
            {
                "/tmp",
                "/var/tmp",
                Path.Combine(home, ".Trash"),
                Path.GetTempPath()
            };

            var total = tempPaths.Count;
            for (int i = 0; i < total; i++)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report(new CleanProgress
                {
                    PercentComplete = (double)i / total * 100,
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
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var libraryCaches = Path.Combine(home, "Library", "Caches");

            var browserPaths = new Dictionary<string, string>
            {
                ["Chrome"] = Path.Combine(libraryCaches, "com.google.Chrome"),
                ["Chrome Canary"] = Path.Combine(libraryCaches, "com.google.Chrome.Canary"),
                ["Edge"] = Path.Combine(libraryCaches, "com.microsoft.edgemac"),
                ["Firefox"] = Path.Combine(libraryCaches, "Firefox"),
                ["Safari"] = Path.Combine(home, "Library", "Safari"),
                ["Opera"] = Path.Combine(libraryCaches, "com.operasoftware.Opera"),
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
        var result = new CleanResult { OperationName = "Preferences & Plist" };

        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var prefsPath = Path.Combine(home, "Library", "Preferences");
            var appSupport = Path.Combine(home, "Library", "Application Support");

            progress?.Report(new CleanProgress
            {
                PercentComplete = 10,
                StatusMessage = LocalizationService.GetString("CleaningPreferences")
            });

            // Remove invalid/corrupt plist files
            if (Directory.Exists(prefsPath))
            {
                foreach (var plist in Directory.GetFiles(prefsPath, "*.plist"))
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        // Validate plist with plutil
                        var valid = await ValidatePlistAsync(plist, ct);
                        if (!valid)
                        {
                            var info = new FileInfo(plist);
                            result.BytesFreed += info.Length;
                            File.Delete(plist);
                            result.FilesDeleted++;
                        }
                    }
                    catch
                    {
                        // Skip protected files
                    }
                }
            }

            // Clean old Application Support caches
            if (Directory.Exists(appSupport))
            {
                var cacheDirs = Directory.GetDirectories(appSupport, "Caches", SearchOption.AllDirectories);
                foreach (var cacheDir in cacheDirs)
                {
                    ct.ThrowIfCancellationRequested();
                    var (deleted, freed) = await CleanDirectoryAsync(cacheDir, ct);
                    result.FilesDeleted += deleted;
                    result.BytesFreed += freed;
                }
            }

            // Clean system caches
            var systemCachePaths = new[]
            {
                "/Library/Caches",
                "/System/Library/Caches"
            };

            foreach (var sysCache in systemCachePaths)
            {
                ct.ThrowIfCancellationRequested();
                if (Directory.Exists(sysCache))
                {
                    var (deleted, freed) = await CleanDirectoryAsync(sysCache, ct);
                    result.FilesDeleted += deleted;
                    result.BytesFreed += freed;
                }
            }

            result.Success = true;
            result.Message = $"Cleaned preferences and caches: {result.FilesDeleted} files, freed {result.BytesFreed} bytes";
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
        var result = new CleanResult { OperationName = "Login Items" };

        try
        {
            progress?.Report(new CleanProgress
            {
                PercentComplete = 30,
                StatusMessage = LocalizationService.GetString("CleaningLoginItems")
            });

            // List and report login items (non-destructive by default)
            var items = await RunCommandAsync("osascript", "-e 'tell application \"System Events\" to get the name of every login item'", ct);

            result.Success = true;
            result.Message = $"Found login items: {items}";
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
        var result = new CleanResult { OperationName = "Trash" };

        try
        {
            progress?.Report(new CleanProgress
            {
                PercentComplete = 50,
                StatusMessage = LocalizationService.GetString("EmptyingTrash")
            });

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var trashPath = Path.Combine(home, ".Trash");

            if (Directory.Exists(trashPath))
            {
                var (deleted, freed) = await CleanDirectoryAsync(trashPath, ct);
                result.FilesDeleted = deleted;
                result.BytesFreed = freed;
            }

            result.Success = true;
            result.Message = $"Trash emptied: {result.FilesDeleted} files, freed {result.BytesFreed} bytes";
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
            var logPaths = new[]
            {
                "/var/log",
                "/Library/Logs",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs")
            };

            var total = logPaths.Length;
            for (int i = 0; i < total; i++)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report(new CleanProgress
                {
                    PercentComplete = (double)i / total * 100,
                    CurrentOperation = $"Cleaning {logPaths[i]}",
                    StatusMessage = LocalizationService.GetString("CleaningSystemLogs")
                });

                if (Directory.Exists(logPaths[i]))
                {
                    var (deleted, freed) = await CleanDirectoryAsync(logPaths[i], ct);
                    result.FilesDeleted += deleted;
                    result.BytesFreed += freed;
                }
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
        var result = new CleanResult { OperationName = "Time Machine Snapshot" };

        try
        {
            progress?.Report(new CleanProgress
            {
                PercentComplete = 30,
                StatusMessage = LocalizationService.GetString("CreatingSnapshot")
            });

            // Attempt to create a local Time Machine snapshot
            var output = await RunCommandAsync("tmutil", "snapshot", ct);

            result.Success = true;
            result.Message = $"Snapshot created: {output}";
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

    public async Task<List<InstalledApp>> ScanInstalledAppsAsync(
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        var apps = new List<InstalledApp>();

        await Task.Run(async () =>
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appDirs = new List<string>();

            // /Applications
            if (Directory.Exists("/Applications"))
                appDirs.AddRange(Directory.GetDirectories("/Applications"));

            // ~/Applications
            var userApps = Path.Combine(home, "Applications");
            if (Directory.Exists(userApps))
                appDirs.AddRange(Directory.GetDirectories(userApps));

            var total = appDirs.Count;
            for (int i = 0; i < total; i++)
            {
                ct.ThrowIfCancellationRequested();
                var appDir = appDirs[i];
                var appName = Path.GetFileNameWithoutExtension(appDir);

                // Skip system apps
                if (IsMacSystemApp(appDir, appName)) continue;

                progress?.Report(new CleanProgress
                {
                    PercentComplete = (double)i / total * 100,
                    CurrentOperation = $"Scanning: {appName}",
                    StatusMessage = LocalizationService.GetString("ScanningInstalledApps")
                });

                var app = new InstalledApp
                {
                    Name = appName,
                    InstallLocation = appDir,
                    Publisher = ""
                };

                // Read Info.plist for bundle ID
                var plistPath = Path.Combine(appDir, "Contents", "Info.plist");
                if (File.Exists(plistPath))
                {
                    try
                    {
                        var plistContent = await File.ReadAllTextAsync(plistPath, ct);
                        if (plistContent.Contains("CFBundleIdentifier"))
                        {
                            var start = plistContent.IndexOf("<string>", plistContent.IndexOf("CFBundleIdentifier"));
                            if (start > 0)
                            {
                                start += 8;
                                var end = plistContent.IndexOf("</string>", start);
                                if (end > start)
                                    app.Publisher = plistContent.Substring(start, end - start);
                            }
                        }
                    }
                    catch { }
                }

                // Scan for leftover files
                app.Leftovers = FindMacLeftoverFiles(app, ct);
                apps.Add(app);
            }
        }, ct);

        return apps;
    }

    private static List<LeftoverFile> FindMacLeftoverFiles(InstalledApp app, CancellationToken ct)
    {
        var leftovers = new List<LeftoverFile>();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var searchPaths = new[]
        {
            Path.Combine(home, "Library", "Caches"),
            Path.Combine(home, "Library", "Preferences"),
            Path.Combine(home, "Library", "Application Support"),
            Path.Combine(home, "Library", "Logs"),
            Path.Combine(home, "Library", "Saved Application State"),
            Path.Combine(home, "Library", "WebKit"),
            "/Library/Caches",
            "/Library/Logs",
            "/Library/Application Support"
        };

        var searchTerms = new[] { app.Name, app.Publisher }
            .Where(t => !string.IsNullOrEmpty(t)).ToArray();

        foreach (var searchPath in searchPaths)
        {
            ct.ThrowIfCancellationRequested();
            if (!Directory.Exists(searchPath)) continue;

            foreach (var term in searchTerms)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    foreach (var dir in Directory.GetDirectories(searchPath, $"*{term}*", SearchOption.TopDirectoryOnly))
                    {
                        ct.ThrowIfCancellationRequested();
                        // Skip if app is still installed
                        if (app.InstallLocation != null && Directory.Exists(app.InstallLocation))
                            continue;

                        var info = new DirectoryInfo(dir);
                        var isHidden = (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                        long size = 0;
                        try { size = info.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length); }
                        catch { }

                        leftovers.Add(new LeftoverFile
                        {
                            Path = dir,
                            IsHidden = isHidden,
                            IsSystem = false,
                            Size = size,
                            Type = "Directory"
                        });
                    }
                }
                catch { }
            }
        }

        return leftovers;
    }

    // --- Private helpers ---

    private static bool IsMacSystemApp(string appDir, string appName)
    {
        var systemApps = new[]
        {
            "Safari", "Mail", "Messages", "FaceTime", "Maps", "Photos",
            "Calendar", "Contacts", "Notes", "Reminders", "Music",
            "Podcasts", "TV", "News", "Stocks", "Books", "Home",
            "Voice Memos", "Preview", "TextEdit", "QuickTime Player",
            "Chess", "Stickies", "Digital Color Meter", "Grapher",
            "Activity Monitor", "Console", "Disk Utility", "Keychain Access",
            "Migration Assistant", "Boot Camp Assistant", "System Information",
            "Font Book", "Image Capture", "Script Editor", "Automator",
            "Xcode", "Instruments", "Accessibility Inspector",
            "System Preferences", "System Settings"
        };

        foreach (var sysApp in systemApps)
        {
            if (appName.Equals(sysApp, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Skip Apple system directories
        if (appDir.StartsWith("/System/Applications", StringComparison.OrdinalIgnoreCase) ||
            appDir.StartsWith("/System/Library", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static async Task<bool> ValidatePlistAsync(string plistPath, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "plutil",
                Arguments = $"-lint \"{plistPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi)!;
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            return output.Contains("OK") || !output.Contains("error");
        }
        catch
        {
            return true; // Assume valid if we can't check
        }
    }

    private static async Task<string> RunCommandAsync(string command, string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return output.Trim();
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
                if (!Directory.Exists(directoryPath)) return;

                var dir = new DirectoryInfo(directoryPath);

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
                        // Skip locked files
                    }
                }

                foreach (var subDir in dir.EnumerateDirectories("*", SearchOption.AllDirectories)
                             .OrderByDescending(d => d.FullName.Length))
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        if (!subDir.EnumerateFiles().Any())
                            subDir.Delete(false);
                    }
                    catch
                    {
                        // Skip
                    }
                }
            }
            catch
            {
                // Skip inaccessible dirs
            }
        }, ct);

        return (filesDeleted, bytesFreed);
    }
}
