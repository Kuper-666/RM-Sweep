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

            // %TEMP% и %WINDIR%\Temp
            tempPaths.Add(Path.GetTempPath());
            var winTemp = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Windows", "Temp");
            if (Directory.Exists(winTemp)) tempPaths.Add(winTemp);

            // Prefetch
            var prefetch = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Windows", "Prefetch");
            if (Directory.Exists(prefetch)) tempPaths.Add(prefetch);

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            // WebCache (Explorer icons/web views)
            var webCache = Path.Combine(localAppData, "Microsoft", "Windows", "WebCache");
            if (Directory.Exists(webCache)) tempPaths.Add(webCache);

            // WER ReportQueue (crash reports - gigabytes)
            var werPath = Path.Combine(programData, "Microsoft", "Windows", "WER", "ReportQueue");
            if (Directory.Exists(werPath)) tempPaths.Add(werPath);
            var werPathUser = Path.Combine(localAppData, "Microsoft", "Windows", "WER", "ReportQueue");
            if (Directory.Exists(werPathUser)) tempPaths.Add(werPathUser);

            // Windows logs
            var winLogs = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Windows", "Logs");
            if (Directory.Exists(winLogs)) tempPaths.Add(winLogs);

            // --- Game launchers ---
            // Steam cache
            var steamCache = Path.Combine(localAppData, "Steam", "cache");
            if (Directory.Exists(steamCache)) tempPaths.Add(steamCache);
            // Battle.net cache
            var bnetCache = Path.Combine(localAppData, "Battle.net", "Cache");
            if (Directory.Exists(bnetCache)) tempPaths.Add(bnetCache);
            // Epic Games logs
            var epicLogs = Path.Combine(localAppData, "EpicGamesLauncher", "Saved", "Logs");
            if (Directory.Exists(epicLogs)) tempPaths.Add(epicLogs);
            // Discord cache
            var discordCache = Path.Combine(localAppData, "Discord", "Cache");
            if (Directory.Exists(discordCache)) tempPaths.Add(discordCache);
            var discordCodeCache = Path.Combine(localAppData, "Discord", "Code Cache");
            if (Directory.Exists(discordCodeCache)) tempPaths.Add(discordCodeCache);
            // Telegram cache
            var telegramCache = Path.Combine(appData, "TelegramDesktop", "Cache");
            if (Directory.Exists(telegramCache)) tempPaths.Add(telegramCache);

            // --- Adobe cache ---
            var adobeCache = Path.Combine(appData, "Adobe", "Common", "Media Cache");
            if (Directory.Exists(adobeCache)) tempPaths.Add(adobeCache);
            var adobeCacheFiles = Path.Combine(appData, "Adobe", "Common", "Media Cache Files");
            if (Directory.Exists(adobeCacheFiles)) tempPaths.Add(adobeCacheFiles);

            // --- MS Office ---
            var officeCache = Path.Combine(localAppData, "Microsoft", "Office", "16.0", "WebCache");
            if (Directory.Exists(officeCache)) tempPaths.Add(officeCache);
            var officeCacheOld = Path.Combine(localAppData, "Microsoft", "Office", "15.0", "WebCache");
            if (Directory.Exists(officeCacheOld)) tempPaths.Add(officeCacheOld);

            // --- Browser caches ---
            // Chrome
            var chromeCache = Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Cache");
            if (Directory.Exists(chromeCache)) tempPaths.Add(chromeCache);
            var chromeCodeCache = Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Code Cache");
            if (Directory.Exists(chromeCodeCache)) tempPaths.Add(chromeCodeCache);
            // Edge
            var edgeCache = Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Cache");
            if (Directory.Exists(edgeCache)) tempPaths.Add(edgeCache);
            var edgeCodeCache = Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Code Cache");
            if (Directory.Exists(edgeCodeCache)) tempPaths.Add(edgeCodeCache);
            // Firefox (find profile wildcard)
            var firefoxProfiles = Path.Combine(appData, "Mozilla", "Firefox", "Profiles");
            if (Directory.Exists(firefoxProfiles))
            {
                foreach (var profile in Directory.GetDirectories(firefoxProfiles))
                {
                    var ffCache = Path.Combine(profile, "cache2");
                    if (Directory.Exists(ffCache)) tempPaths.Add(ffCache);
                }
            }
            // Opera
            var operaCache = Path.Combine(appData, "Opera Software", "Opera Stable", "Cache");
            if (Directory.Exists(operaCache)) tempPaths.Add(operaCache);

            // --- AppData generic cache/temp/crash ---
            var cacheNames = new[] { "Cache", "cache", "CrashDumps", "D3DSCache",
                "IconCache", "MediaCache", "Code Cache", "GPUCache",
                "Service Worker", "Temp", "tmp",
                "Downloaded Installations", "DownloadedPrograms" };

            if (Directory.Exists(localAppData))
            {
                foreach (var cacheName in cacheNames)
                {
                    var cachePath = Path.Combine(localAppData, cacheName);
                    if (Directory.Exists(cachePath)) tempPaths.Add(cachePath);
                }

                foreach (var dir in Directory.GetDirectories(localAppData))
                {
                    var dirName = Path.GetFileName(dir);
                    if (dirName.StartsWith(".")) continue;

                    var subCacheNames = new[] { "Cache", "cache", "CrashDumps", "Code Cache",
                        "GPUCache", "Service Worker", "Temp", "tmp", "logs", "Logs" };

                    foreach (var sub in subCacheNames)
                    {
                        var subPath = Path.Combine(dir, sub);
                        if (Directory.Exists(subPath)) tempPaths.Add(subPath);
                    }
                }
            }

            if (Directory.Exists(appData))
            {
                foreach (var dir in Directory.GetDirectories(appData))
                {
                    var subCacheNames = new[] { "Cache", "cache", "CrashDumps", "Code Cache",
                        "GPUCache", "Temp", "tmp", "logs", "Logs" };

                    foreach (var sub in subCacheNames)
                    {
                        var subPath = Path.Combine(dir, sub);
                        if (Directory.Exists(subPath)) tempPaths.Add(subPath);
                    }
                }
            }

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

    public async Task<bool> UninstallAppAsync(InstalledApp app, CancellationToken ct = default)
    {
        // Step 1: Find all leftover files BEFORE uninstall
        var leftovers = FindLeftoverFiles(app, ct);

        // Step 2: Run uninstaller
        bool uninstalled = false;
        if (!string.IsNullOrEmpty(app.UninstallString))
        {
            uninstalled = await Task.Run(() =>
            {
                try
                {
                    var uninstallCmd = app.UninstallString;

                    if (uninstallCmd.StartsWith("MsiExec.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        var msiArgs = uninstallCmd.Replace("MsiExec.exe", "").Trim();
                        if (!msiArgs.Contains("/qn"))
                            msiArgs += " /qn";
                        var psi = new ProcessStartInfo { FileName = "msiexec.exe", Arguments = msiArgs, UseShellExecute = true, Verb = "runas" };
                        var process = Process.Start(psi);
                        process?.WaitForExit(120000);
                        return true;
                    }

                    if (uninstallCmd.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                        uninstallCmd.Contains("uninstall", StringComparison.OrdinalIgnoreCase))
                    {
                        var exePath = ExtractExePath(uninstallCmd);
                        var args = uninstallCmd.Contains(" ")
                            ? uninstallCmd.Substring(uninstallCmd.IndexOf(exePath) + exePath.Length).Trim()
                            : "/S /silent /quiet";
                        if (string.IsNullOrEmpty(args)) args = "/S /silent /quiet";
                        var psi = new ProcessStartInfo { FileName = exePath, Arguments = args, UseShellExecute = true, Verb = "runas" };
                        var process = Process.Start(psi);
                        process?.WaitForExit(120000);
                        return true;
                    }

                    var fallbackPsi = new ProcessStartInfo { FileName = "cmd.exe", Arguments = $"/c \"{uninstallCmd}\"", UseShellExecute = true, Verb = "runas" };
                    var fallbackProcess = Process.Start(fallbackPsi);
                    fallbackProcess?.WaitForExit(120000);
                    return true;
                }
                catch { return false; }
            }, ct);
        }

        // Step 3: Wait a moment for filesystem to settle
        await Task.Delay(1000, ct);

        // Step 4: Re-scan leftovers (some may appear after uninstall)
        var postUninstallLeftovers = FindLeftoverFiles(app, ct);
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
                var attrs = File.GetAttributes(leftover.Path);
                if (attrs.HasFlag(FileAttributes.Directory))
                {
                    Directory.Delete(leftover.Path, recursive: true);
                }
                else
                {
                    File.Delete(leftover.Path);
                }
            }
            catch { }
        }

        // Step 6: Clean registry entries for this app
        CleanRegistryEntriesForApp(app.Name, ct);

        return uninstalled || allLeftovers.Count > 0;
    }

    private static void CleanRegistryEntriesForApp(string appName, CancellationToken ct)
    {
        var uninstallKeys = new[]
        {
            (@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", Registry.LocalMachine),
            (@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", Registry.LocalMachine),
            (@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", Registry.CurrentUser)
        };

        foreach (var (keyPath, hive) in uninstallKeys)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var key = hive.OpenSubKey(keyPath, writable: true);
                if (key == null) continue;

                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        using var subKey = key.OpenSubKey(subKeyName);
                        var name = subKey?.GetValue("DisplayName") as string;
                        if (name != null && name.Equals(appName, StringComparison.OrdinalIgnoreCase))
                        {
                            key.DeleteSubKeyTree(subKeyName, throwOnMissingSubKey: false);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        // Also clean Run/RunOnce keys
        var runKeys = new[]
        {
            (@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", Registry.CurrentUser),
            (@"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", Registry.CurrentUser)
        };

        foreach (var (keyPath, hive) in runKeys)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var key = hive.OpenSubKey(keyPath, writable: true);
                if (key == null) continue;

                foreach (var valueName in key.GetValueNames())
                {
                    var value = key.GetValue(valueName) as string ?? "";
                    if (value.Contains(appName, StringComparison.OrdinalIgnoreCase))
                    {
                        key.DeleteValue(valueName, throwOnMissingValue: false);
                    }
                }
            }
            catch { }
        }
    }

    public async Task<List<InstalledApp>> ScanInstalledAppsAsync(
        IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        var apps = new List<InstalledApp>();

        await Task.Run(() =>
        {
            var uninstallKeys = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var keyPath in uninstallKeys)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                    if (key == null) continue;

                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            using var subKey = key.OpenSubKey(subKeyName);
                            if (subKey == null) continue;

                            var name = subKey.GetValue("DisplayName") as string;
                            if (string.IsNullOrEmpty(name)) continue;

                            // Skip system components
                            var systemComponent = subKey.GetValue("SystemComponent");
                            if (systemComponent is int sc && sc == 1) continue;

                            var parentName = subKey.GetValue("ParentDisplayName") as string;
                            if (!string.IsNullOrEmpty(parentName)) continue;

                            var releaseType = subKey.GetValue("ReleaseType") as string ?? "";
                            if (releaseType.Equals("Update", StringComparison.OrdinalIgnoreCase) ||
                                releaseType.Equals("Security Update", StringComparison.OrdinalIgnoreCase) ||
                                releaseType.Equals("Hotfix", StringComparison.OrdinalIgnoreCase)) continue;

                            var installLocation = subKey.GetValue("InstallLocation") as string ?? "";
                            var uninstallString = subKey.GetValue("UninstallString") as string ?? "";
                            var publisher = subKey.GetValue("Publisher") as string ?? "";

                            // Skip known system publishers
                            if (IsSystemPublisher(publisher)) continue;
                            if (IsSystemApp(name, installLocation)) continue;

                            long estimatedSize = 0;
                            var sizeVal = subKey.GetValue("EstimatedSize");
                            if (sizeVal is int intSize) estimatedSize = intSize * 1024;

                            apps.Add(new InstalledApp
                            {
                                Name = name,
                                Publisher = publisher,
                                InstallLocation = installLocation,
                                UninstallString = uninstallString,
                                EstimatedSize = estimatedSize
                            });
                        }
                        catch { }
                    }
                }
                catch { }
            }

            // Also scan user-uninstall keys
            try
            {
                using var userKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (userKey != null)
                {
                    foreach (var subKeyName in userKey.GetSubKeyNames())
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            using var subKey = userKey.OpenSubKey(subKeyName);
                            if (subKey == null) continue;

                            var name = subKey.GetValue("DisplayName") as string;
                            if (string.IsNullOrEmpty(name)) continue;
                            if (apps.Any(a => a.Name == name)) continue;

                            var systemComponent = subKey.GetValue("SystemComponent");
                            if (systemComponent is int sc2 && sc2 == 1) continue;

                            var publisher = subKey.GetValue("Publisher") as string ?? "";
                            if (IsSystemPublisher(publisher)) continue;

                            apps.Add(new InstalledApp
                            {
                                Name = name,
                                Publisher = subKey.GetValue("Publisher") as string ?? "",
                                InstallLocation = subKey.GetValue("InstallLocation") as string ?? "",
                                UninstallString = subKey.GetValue("UninstallString") as string ?? "",
                                EstimatedSize = 0
                            });
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // Scan for leftover files for each app
            var totalApps = apps.Count;
            for (int i = 0; i < totalApps; i++)
            {
                ct.ThrowIfCancellationRequested();
                var app = apps[i];

                progress?.Report(new CleanProgress
                {
                    PercentComplete = (double)i / totalApps * 100,
                    CurrentOperation = $"Scanning: {app.Name}",
                    StatusMessage = LocalizationService.GetString("ScanningInstalledApps")
                });

                app.Leftovers = FindLeftoverFiles(app, ct);
            }
        }, ct);

        return apps;
    }

    private static List<LeftoverFile> FindLeftoverFiles(InstalledApp app, CancellationToken ct)
    {
        var leftovers = new List<LeftoverFile>();
        var searchLocations = new List<string>();

        // AppData folders
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        searchLocations.Add(appData);
        searchLocations.Add(localAppData);
        searchLocations.Add(programData);

        // Add install location parent
        if (!string.IsNullOrEmpty(app.InstallLocation) && Directory.Exists(app.InstallLocation))
        {
            var parent = Path.GetDirectoryName(app.InstallLocation);
            if (parent != null) searchLocations.Add(parent);
        }

        var searchTerms = new List<string> { app.Name };
        // Also search by publisher
        if (!string.IsNullOrEmpty(app.Publisher))
            searchTerms.Add(app.Publisher);

        foreach (var location in searchLocations)
        {
            ct.ThrowIfCancellationRequested();
            if (!Directory.Exists(location)) continue;

            foreach (var term in searchTerms)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var dirs = Directory.GetDirectories(location, $"*{term}*", SearchOption.TopDirectoryOnly);
                    foreach (var dir in dirs)
                    {
                        ct.ThrowIfCancellationRequested();
                        var info = new DirectoryInfo(dir);
                        var isHidden = (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                        var isSystem = (info.Attributes & FileAttributes.System) == FileAttributes.System;

                        // Check if install location still exists
                        if (!string.IsNullOrEmpty(app.InstallLocation) && Directory.Exists(app.InstallLocation))
                            continue; // App is still installed, skip

                        long size = 0;
                        try { size = new DirectoryInfo(dir).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length); }
                        catch { }

                        leftovers.Add(new LeftoverFile
                        {
                            Path = dir,
                            IsHidden = isHidden,
                            IsSystem = isSystem,
                            Size = size,
                            Type = "Directory"
                        });
                    }

                    // Search for files
                    var files = Directory.GetFiles(location, $"*{term}*", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        ct.ThrowIfCancellationRequested();
                        var info = new FileInfo(file);
                        var isHidden = (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                        var isSystem = (info.Attributes & FileAttributes.System) == FileAttributes.System;

                        leftovers.Add(new LeftoverFile
                        {
                            Path = file,
                            IsHidden = isHidden,
                            IsSystem = isSystem,
                            Size = info.Length,
                            Type = "File"
                        });
                    }
                }
                catch { }
            }
        }

        return leftovers;
    }

    // --- Private helpers ---

    private static bool IsSystemPublisher(string publisher)
    {
        var sysPublishers = new[]
        {
            "Microsoft Corporation",
            "Microsoft Windows",
            "Microsoft",
            "Intel",
            "Intel(R) Software and Firmware Products",
            "NVIDIA",
            "Realtek",
            "Qualcomm",
            "Synaptics",
            "AMD",
            "Broadcom",
            "Dell Inc.",
            "HP Inc.",
            "Lenovo",
            "Samsung",
            "Apple Inc.",
            "Google LLC"
        };

        return sysPublishers.Any(sp =>
            publisher.Contains(sp, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSystemApp(string name, string location)
    {
        var systemNames = new[]
        {
            "Microsoft Visual C++",
            "Microsoft .NET",
            "Microsoft Windows",
            "Windows SDK",
            "Windows Driver",
            "Windows Update",
            "DirectX",
            "VC Redist",
            "vcredist",
            "Redistributable",
            "Runtime",
            "Update for",
            "Security Update",
            "Hotfix",
            "Cumulative Update"
        };

        foreach (var sys in systemNames)
        {
            if (name.Contains(sys, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Skip Windows system directories
        if (!string.IsNullOrEmpty(location))
        {
            var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            if (location.StartsWith(winDir, StringComparison.OrdinalIgnoreCase))
                return true;
            if (location.Equals(progFiles, StringComparison.OrdinalIgnoreCase) ||
                location.Equals(progFilesX86, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

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
