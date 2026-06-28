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
                "/var/log",
                Path.Combine(home, ".Trash"),
                Path.GetTempPath(),

                // ~/Library/Caches (main cache location)
                Path.Combine(home, "Library", "Caches"),
                // Logs
                Path.Combine(home, "Library", "Logs"),
                // Saved application state
                Path.Combine(home, "Library", "Saved Application State"),
                // WebKit cache
                Path.Combine(home, "Library", "WebKit"),
                // HTTP storage
                Path.Combine(home, "Library", "HTTPStorages"),
                // Cookies
                Path.Combine(home, "Library", "Cookies"),
                // System caches
                "/Library/Caches",
                "/Library/Logs"
            };

            // Browser caches on Mac
            var browserPaths = new[]
            {
                Path.Combine(home, "Library", "Caches", "Google", "Chrome"),
                Path.Combine(home, "Library", "Caches", "com.google.Chrome"),
                Path.Combine(home, "Library", "Caches", "com.google.Chrome.Canary"),
                Path.Combine(home, "Library", "Caches", "com.microsoft.edgemac"),
                Path.Combine(home, "Library", "Caches", "Firefox"),
                Path.Combine(home, "Library", "Caches", "com.operasoftware.Opera"),
                Path.Combine(home, "Library", "Caches", "Safari"),
                Path.Combine(home, "Library", "Application Support", "Google", "Chrome", "Default", "Cache"),
                Path.Combine(home, "Library", "Application Support", "Microsoft", "Edge", "Default", "Cache"),
            };
            foreach (var bp in browserPaths)
                if (Directory.Exists(bp)) tempPaths.Add(bp);

            // Steam cache on Mac
            var steamPaths = new[]
            {
                Path.Combine(home, "Library", "Application Support", "Steam"),
                Path.Combine(home, "Library", "Caches", "Steam"),
            };
            foreach (var sp in steamPaths)
                if (Directory.Exists(sp)) tempPaths.Add(sp);

            // Discord on Mac
            var discordPaths = new[]
            {
                Path.Combine(home, "Library", "Caches", "com.discord.Discord"),
                Path.Combine(home, "Library", "Application Support", "Discord", "Cache"),
                Path.Combine(home, "Library", "Application Support", "Discord", "Code Cache"),
            };
            foreach (var dp in discordPaths)
                if (Directory.Exists(dp)) tempPaths.Add(dp);

            // Telegram on Mac
            var tgCache = Path.Combine(home, "Library", "Group Containers", "U6N74UHJXM.net.telegram.desktop", "Data", "Library", "Caches");
            if (Directory.Exists(tgCache)) tempPaths.Add(tgCache);

            // Adobe on Mac
            var adobePaths = new[]
            {
                Path.Combine(home, "Library", "Caches", "Adobe"),
                Path.Combine(home, "Library", "Application Support", "Adobe", "Common", "Media Cache"),
            };
            foreach (var ap in adobePaths)
                if (Directory.Exists(ap)) tempPaths.Add(ap);

            // Per-app Cache subdirs inside ~/Library/Caches
            var libCaches = Path.Combine(home, "Library", "Caches");
            if (Directory.Exists(libCaches))
            {
                try
                {
                    foreach (var dir in Directory.GetDirectories(libCaches))
                    {
                        ct.ThrowIfCancellationRequested();
                        var innerCache = Path.Combine(dir, "Cache");
                        if (Directory.Exists(innerCache))
                            tempPaths.Add(innerCache);
                    }
                }
                catch { }
            }

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

    public async Task<bool> UninstallAppAsync(InstalledApp app, CancellationToken ct = default)
    {
        // Step 1: Find all leftover files BEFORE uninstall
        var leftovers = FindMacLeftoverFiles(app, ct);

        bool uninstalled = false;

        // Step 2: Move app to Trash via osascript (proper macOS uninstall)
        if (!string.IsNullOrEmpty(app.InstallLocation) && Directory.Exists(app.InstallLocation))
        {
            try
            {
                var script = $"tell application \"Finder\" to delete POSIX file \"{app.InstallLocation}\"";
                await RunCommandAsync("osascript", $"-e '{script}'", ct);
                uninstalled = true;
            }
            catch
            {
                // Fallback: rm -rf
                try
                {
                    await RunCommandAsync("rm", $"-rf \"{app.InstallLocation}\"", ct);
                    uninstalled = true;
                }
                catch { }
            }
        }

        // Step 3: Wait for filesystem to settle
        await Task.Delay(1000, ct);

        // Step 4: Re-scan leftovers
        var postUninstallLeftovers = FindMacLeftoverFiles(app, ct);
        var allLeftovers = leftovers.Concat(postUninstallLeftovers)
            .GroupBy(l => l.Path)
            .Select(g => g.First())
            .ToList();

        // Step 5: Delete all leftover files and directories
        foreach (var leftover in allLeftovers)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (Directory.Exists(leftover.Path))
                    await RunCommandAsync("rm", $"-rf \"{leftover.Path}\"", ct);
                else if (File.Exists(leftover.Path))
                    File.Delete(leftover.Path);
            }
            catch { }
        }

        // Step 6: Clean plist files
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var prefsDir = Path.Combine(home, "Library", "Preferences");
        if (Directory.Exists(prefsDir))
        {
            foreach (var plist in Directory.GetFiles(prefsDir, "*.plist"))
            {
                ct.ThrowIfCancellationRequested();
                if (plist.Contains(app.Name, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(app.Publisher) && plist.Contains(app.Publisher, StringComparison.OrdinalIgnoreCase)))
                {
                    try { File.Delete(plist); } catch { }
                }
            }
        }

        return uninstalled || allLeftovers.Count > 0;
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

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
        _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
    };

    public Task<CleanResult> CleanDnsCacheAsync(
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        return Task.FromResult(new CleanResult
        {
            OperationName = "DNS Cache",
            Success = true,
            Message = "DNS cache flush: use 'sudo dscacheutil -flushcache' on macOS"
        });
    }

    public Task<CleanResult> CleanClipboardAsync(
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        return Task.FromResult(new CleanResult
        {
            OperationName = "Clipboard",
            Success = true,
            Message = "Clipboard clear: use 'pbcopy < /dev/null' on macOS"
        });
    }

    public async Task<CleanResult> CleanRecentDocumentsAsync(
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        var result = new CleanResult { OperationName = "Recent Documents" };

        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var recentPath = Path.Combine(home, "Library", "Recent");
            if (Directory.Exists(recentPath))
            {
                var (deleted, freed) = await CleanDirectoryAsync(recentPath, ct);
                result.FilesDeleted = deleted;
                result.BytesFreed = freed;
            }

            result.Success = true;
            result.Message = $"Recent documents cleared: {result.FilesDeleted} files";
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }

    public Task<CleanResult> CleanThumbnailCacheAsync(
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        return Task.FromResult(new CleanResult
        {
            OperationName = "Thumbnail Cache",
            Success = true,
            Message = "Thumbnail cache: macOS manages this automatically"
        });
    }

    public Task<CleanResult> CleanMemoryDumpsAsync(
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        return Task.FromResult(new CleanResult
        {
            OperationName = "Memory Dumps",
            Success = true,
            Message = "Memory dumps: macOS crash logs are in ~/Library/Logs/DiagnosticReports"
        });
    }

    public Task<CleanResult> CleanChkdskFragmentsAsync(
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        return Task.FromResult(new CleanResult
        {
            OperationName = "Chkdsk Fragments",
            Success = true,
            Message = "Chkdsk fragments: not applicable on macOS"
        });
    }

    public Task<CleanResult> CleanWindowsUpdateCacheAsync(
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        return Task.FromResult(new CleanResult
        {
            OperationName = "Windows Update Cache",
            Success = true,
            Message = "Windows Update cache: not applicable on macOS"
        });
    }

    public Task<CleanResult> CleanCustomFoldersAsync(
        List<string> folders, IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        return Task.FromResult(new CleanResult
        {
            OperationName = "Custom Folders",
            Success = true,
            Message = "Custom folders cleaning not yet implemented for macOS"
        });
    }

    public Task<CleanResult> WipeDriveFreeSpaceAsync(
        string driveLetter, DriveWipeMethod method,
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        return Task.FromResult(new CleanResult
        {
            OperationName = "Drive Wiper",
            Success = true,
            Message = "Drive wiper: use 'diskutil secureErase' on macOS"
        });
    }

    public Task<List<DuplicateGroup>> ScanForDuplicatesAsync(
        string directoryPath, IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        return Task.FromResult(new List<DuplicateGroup>());
    }

    public Task<List<DiskSpaceItem>> AnalyzeDiskSpaceAsync(
        string directoryPath, IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        return Task.FromResult(new List<DiskSpaceItem>());
    }

    public async Task<CleanResult> ShredItemAsync(string path, DriveWipeMethod method, IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        var result = new CleanResult { OperationName = "File Shredder" };
        try
        {
            if (File.Exists(path))
            {
                await ShredFileInternalAsync(path, method, progress, ct);
                result.FilesDeleted = 1;
            }
            else if (Directory.Exists(path))
            {
                var dir = new DirectoryInfo(path);
                var files = dir.GetFiles("*", SearchOption.AllDirectories);
                int total = files.Length;
                int current = 0;
                foreach (var file in files)
                {
                    current++;
                    progress?.Report(new CleanProgress
                    {
                        PercentComplete = (double)current / total * 100,
                        StatusMessage = $"Shredding {file.Name}"
                    });
                    await ShredFileInternalAsync(file.FullName, method, null, ct);
                    result.FilesDeleted++;
                }
                Directory.Delete(path, true);
            }
            else
            {
                throw new FileNotFoundException("Path not found");
            }

            result.Success = true;
            result.Message = $"Shredding complete: {result.FilesDeleted} files wiped";
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

    private async Task ShredFileInternalAsync(string filePath, DriveWipeMethod method, IProgress<CleanProgress>? progress, CancellationToken ct)
    {
        int passes = method switch
        {
            DriveWipeMethod.ZeroFill => 1,
            DriveWipeMethod.DoD522022M => 3,
            DriveWipeMethod.Gutmann => 35,
            _ => 1
        };

        var fileInfo = new FileInfo(filePath);
        long size = fileInfo.Length;
        int bufferSize = 1024 * 1024; // 1MB
        byte[] buffer = new byte[bufferSize];

        for (int pass = 0; pass < passes; pass++)
        {
            ct.ThrowIfCancellationRequested();
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous);
            long bytesWritten = 0;

            byte fillByte = (byte)(pass % 255); // simple pattern for DoD/Gutmann simulation
            if (method == DriveWipeMethod.ZeroFill) fillByte = 0;
            Array.Fill(buffer, fillByte);

            while (bytesWritten < size)
            {
                ct.ThrowIfCancellationRequested();
                int toWrite = (int)Math.Min(bufferSize, size - bytesWritten);
                await fs.WriteAsync(buffer, 0, toWrite, ct);
                bytesWritten += toWrite;
            }
            await fs.FlushAsync(ct);
        }

        // Rename file to obscure name before deletion
        var dir = Path.GetDirectoryName(filePath);
        var newPath = Path.Combine(dir ?? "", Guid.NewGuid().ToString() + ".tmp");
        File.Move(filePath, newPath);
        File.Delete(newPath);
    }
}
