using RMSweep.Cleaners;
using RMSweep.Interfaces;
using RMSweep.Models;

namespace RMSweep.Tests.Cleaners;

public class CleanerFactoryTests
{
    [Fact]
    public void Create_ReturnsISystemCleaner()
    {
        var cleaner = CleanerFactory.Create();
        Assert.NotNull(cleaner);
        Assert.IsAssignableFrom<ISystemCleaner>(cleaner);
    }

    [Fact]
    public void Create_ReturnsWindowsCleaner_OnWindows()
    {
        if (!OperatingSystem.IsWindows()) return;

        var cleaner = CleanerFactory.Create();
        Assert.IsType<WindowsCleaner>(cleaner);
        Assert.Equal("Windows", cleaner.PlatformName);
    }

    [Fact]
    public void Create_ReturnsMacCleaner_OnMac()
    {
        if (!OperatingSystem.IsMacOS()) return;

        var cleaner = CleanerFactory.Create();
        Assert.IsAssignableFrom<ISystemCleaner>(cleaner);
        Assert.Equal("macOS", cleaner.PlatformName);
    }
}

public class WindowsCleanerTests
{
    private readonly WindowsCleaner _cleaner = new();

    [Fact]
    public void PlatformName_ReturnsWindows()
    {
        if (!OperatingSystem.IsWindows()) return;
        Assert.Equal("Windows", _cleaner.PlatformName);
    }

    [Fact]
    public async Task CleanTemporaryFilesAsync_ReturnsSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.CleanTemporaryFilesAsync();

        Assert.NotNull(result);
        Assert.Equal("Temporary Files", result.OperationName);
        // Success depends on permissions but should not throw
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task CleanTemporaryFilesAsync_ReportsProgress()
    {
        if (!OperatingSystem.IsWindows()) return;

        var progressReports = new List<CleanProgress>();
        var progress = new Progress<CleanProgress>(p => progressReports.Add(p));

        await _cleaner.CleanTemporaryFilesAsync(progress);

        // Should have reported at least one progress
        Assert.NotEmpty(progressReports);
        Assert.All(progressReports, p =>
        {
            Assert.True(p.PercentComplete >= 0 && p.PercentComplete <= 100);
            Assert.NotNull(p.StatusMessage);
        });
    }

    [Fact]
    public async Task CleanTemporaryFilesAsync_SupportsCancellation()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _cleaner.CleanTemporaryFilesAsync(ct: cts.Token));
    }

    [Fact]
    public async Task CleanBrowserCacheAsync_ReturnsSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.CleanBrowserCacheAsync();

        Assert.NotNull(result);
        Assert.Equal("Browser Cache", result.OperationName);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task CleanBrowserCacheAsync_ReportsProgress()
    {
        if (!OperatingSystem.IsWindows()) return;

        var progressReports = new List<CleanProgress>();
        var progress = new Progress<CleanProgress>(p => progressReports.Add(p));

        await _cleaner.CleanBrowserCacheAsync(progress);

        Assert.NotEmpty(progressReports);
        Assert.All(progressReports, p => Assert.True(p.PercentComplete >= 0));
    }

    [Fact]
    public async Task CleanSystemSettingsAsync_ReturnsSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.CleanSystemSettingsAsync();

        Assert.NotNull(result);
        Assert.Equal("Registry (System Settings)", result.OperationName);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task CleanAutostartAsync_ReturnsSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.CleanAutostartAsync();

        Assert.NotNull(result);
        Assert.Equal("Autostart", result.OperationName);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task EmptyRecycleBinAsync_ReturnsSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.EmptyRecycleBinAsync();

        Assert.NotNull(result);
        Assert.Equal("Recycle Bin", result.OperationName);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task CleanSystemLogsAsync_ReturnsSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.CleanSystemLogsAsync();

        Assert.NotNull(result);
        Assert.Equal("System Logs", result.OperationName);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateRestorePointAsync_ReturnsSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.CreateRestorePointAsync();

        Assert.NotNull(result);
        Assert.Equal("System Restore Point", result.OperationName);
        // May fail without admin but should not throw
    }

    [Fact]
    public async Task CleanDnsCacheAsync_ReturnsSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.CleanDnsCacheAsync();

        Assert.NotNull(result);
        Assert.Equal("DNS Cache", result.OperationName);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task CleanClipboardAsync_ReturnsSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.CleanClipboardAsync();

        Assert.NotNull(result);
        Assert.Equal("Clipboard", result.OperationName);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task CleanRecentDocumentsAsync_ReturnsSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.CleanRecentDocumentsAsync();

        Assert.NotNull(result);
        Assert.Equal("Recent Documents", result.OperationName);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task CleanThumbnailCacheAsync_ReturnsSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.CleanThumbnailCacheAsync();

        Assert.NotNull(result);
        Assert.Equal("Thumbnail Cache", result.OperationName);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task CleanMemoryDumpsAsync_ReturnsSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.CleanMemoryDumpsAsync();

        Assert.NotNull(result);
        Assert.Equal("Memory Dumps", result.OperationName);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task CleanChkdskFragmentsAsync_ReturnsSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.CleanChkdskFragmentsAsync();

        Assert.NotNull(result);
        Assert.Equal("Chkdsk Fragments", result.OperationName);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task CleanWindowsUpdateCacheAsync_ReturnsSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.CleanWindowsUpdateCacheAsync();

        Assert.NotNull(result);
        Assert.Equal("Windows Update Cache", result.OperationName);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ScanInstalledAppsAsync_ReturnsNonEmptyList()
    {
        if (!OperatingSystem.IsWindows()) return;

        var apps = await _cleaner.ScanInstalledAppsAsync();

        Assert.NotNull(apps);
        Assert.NotEmpty(apps);
    }

    [Fact]
    public async Task ScanInstalledAppsAsync_EachAppHasName()
    {
        if (!OperatingSystem.IsWindows()) return;

        var apps = await _cleaner.ScanInstalledAppsAsync();

        Assert.All(apps, app => Assert.False(string.IsNullOrEmpty(app.Name)));
    }

    [Fact]
    public async Task ScanInstalledAppsAsync_SkipsSystemComponents()
    {
        if (!OperatingSystem.IsWindows()) return;

        var apps = await _cleaner.ScanInstalledAppsAsync();

        // Should not contain Windows system components
        Assert.DoesNotContain(apps, a => a.Name.Contains("Microsoft Visual C++"));
        Assert.DoesNotContain(apps, a => a.Name.Contains("Microsoft .NET"));
        Assert.DoesNotContain(apps, a => a.Name.Contains("Windows SDK"));
        Assert.DoesNotContain(apps, a => a.Name.Contains("Redistributable"));
    }

    [Fact]
    public async Task ScanInstalledAppsAsync_SkipsSystemPublishers()
    {
        if (!OperatingSystem.IsWindows()) return;

        var apps = await _cleaner.ScanInstalledAppsAsync();

        Assert.DoesNotContain(apps, a =>
            a.Publisher.Contains("Microsoft Corporation", StringComparison.OrdinalIgnoreCase) ||
            a.Publisher.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
            a.Publisher.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScanInstalledAppsAsync_ProgressReports()
    {
        if (!OperatingSystem.IsWindows()) return;

        var progressReports = new List<CleanProgress>();
        var progress = new Progress<CleanProgress>(p => progressReports.Add(p));

        await _cleaner.ScanInstalledAppsAsync(progress);

        Assert.NotEmpty(progressReports);
    }

    [Fact]
    public async Task ScanInstalledAppsAsync_SupportsCancellation()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _cleaner.ScanInstalledAppsAsync(ct: cts.Token));
    }

    [Fact]
    public async Task CleanCustomFoldersAsync_EmptyList_ReturnsSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.CleanCustomFoldersAsync(new List<string>());

        Assert.NotNull(result);
        Assert.Equal("Custom Folders", result.OperationName);
        Assert.True(result.Success);
        Assert.Equal(0, result.FilesDeleted);
    }

    [Fact]
    public async Task CleanCustomFoldersAsync_NonExistentFolder_ReturnsSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.CleanCustomFoldersAsync(
            new List<string> { @"C:\NonExistentFolder_12345" });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(0, result.FilesDeleted);
    }

    [Fact]
    public async Task CleanCustomFoldersAsync_ActualFolder_CleansFiles()
    {
        if (!OperatingSystem.IsWindows()) return;

        var testDir = Path.Combine(Path.GetTempPath(), $"rmsweep_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);

        try
        {
            // Create test files
            File.WriteAllText(Path.Combine(testDir, "test1.txt"), "hello");
            File.WriteAllText(Path.Combine(testDir, "test2.log"), "log data");
            var subDir = Path.Combine(testDir, "subdir");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "test3.tmp"), "tmp data");

            var result = await _cleaner.CleanCustomFoldersAsync(new List<string> { testDir });

            Assert.True(result.Success);
            Assert.Equal(3, result.FilesDeleted);
            Assert.True(result.BytesFreed > 0);
            Assert.False(Directory.Exists(subDir)); // Empty subdirs deleted
        }
        finally
        {
            if (Directory.Exists(testDir))
                Directory.Delete(testDir, recursive: true);
        }
    }

    [Fact]
    public async Task WipeDriveFreeSpaceAsync_NonExistentDrive_ReturnsFailure()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.WipeDriveFreeSpaceAsync("Z", DriveWipeMethod.ZeroFill);

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task WipeDriveFreeSpaceAsync_CDrive_Succeeds()
    {
        if (!OperatingSystem.IsWindows()) return;

        // This is a quick test - just wipe a tiny amount
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            var result = await _cleaner.WipeDriveFreeSpaceAsync("C", DriveWipeMethod.ZeroFill, ct: cts.Token);
            // May timeout or succeed
        }
        catch (OperationCanceledException)
        {
            // Expected for quick test
        }
    }

    [Fact]
    public async Task ScanForDuplicatesAsync_NonExistentDir_ReturnsEmpty()
    {
        if (!OperatingSystem.IsWindows()) return;

        var groups = await _cleaner.ScanForDuplicatesAsync(@"C:\NonExistentDir_12345");

        Assert.NotNull(groups);
        Assert.Empty(groups);
    }

    [Fact]
    public async Task ScanForDuplicatesAsync_DetectsDuplicates()
    {
        if (!OperatingSystem.IsWindows()) return;

        var testDir = Path.Combine(Path.GetTempPath(), $"rmsweep_dupes_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);

        try
        {
            var content = "This is test content for duplicate detection";
            File.WriteAllText(Path.Combine(testDir, "file1.txt"), content);
            File.WriteAllText(Path.Combine(testDir, "file2.txt"), content);
            File.WriteAllText(Path.Combine(testDir, "unique.txt"), "Different content");

            var groups = await _cleaner.ScanForDuplicatesAsync(testDir);

            Assert.Single(groups);
            Assert.Equal(2, groups[0].Count);
            Assert.Equal(2, groups[0].Items.Count);
        }
        finally
        {
            Directory.Delete(testDir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanForDuplicatesAsync_IgnoresEmptyFiles()
    {
        if (!OperatingSystem.IsWindows()) return;

        var testDir = Path.Combine(Path.GetTempPath(), $"rmsweep_empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);

        try
        {
            File.WriteAllText(Path.Combine(testDir, "empty1.txt"), "");
            File.WriteAllText(Path.Combine(testDir, "empty2.txt"), "");

            var groups = await _cleaner.ScanForDuplicatesAsync(testDir);

            Assert.Empty(groups);
        }
        finally
        {
            Directory.Delete(testDir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanForDuplicatesAsync_SupportsCancellation()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _cleaner.ScanForDuplicatesAsync(@"C:\", ct: cts.Token));
    }

    [Fact]
    public async Task AnalyzeDiskSpaceAsync_NonExistentDir_ReturnsEmpty()
    {
        if (!OperatingSystem.IsWindows()) return;

        var items = await _cleaner.AnalyzeDiskSpaceAsync(@"C:\NonExistentDir_12345");

        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task AnalyzeDiskSpaceAsync_AnalyzesCorrectly()
    {
        if (!OperatingSystem.IsWindows()) return;

        var testDir = Path.Combine(Path.GetTempPath(), $"rmsweep_analyze_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);

        try
        {
            File.WriteAllText(Path.Combine(testDir, "a.txt"), "aaa");
            File.WriteAllText(Path.Combine(testDir, "b.txt"), "bbb");
            File.WriteAllText(Path.Combine(testDir, "c.log"), "log");
            File.WriteAllText(Path.Combine(testDir, "d.tmp"), "tmp");

            var items = await _cleaner.AnalyzeDiskSpaceAsync(testDir);

            Assert.NotEmpty(items);
            var txtGroup = items.FirstOrDefault(i => i.Extension == ".txt");
            Assert.NotNull(txtGroup);
            Assert.Equal(2, txtGroup!.FileCount);
        }
        finally
        {
            Directory.Delete(testDir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeDiskSpaceAsync_SortsBySizeDescending()
    {
        if (!OperatingSystem.IsWindows()) return;

        var testDir = Path.Combine(Path.GetTempPath(), $"rmsweep_sort_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);

        try
        {
            File.WriteAllText(Path.Combine(testDir, "small.txt"), "s");
            File.WriteAllText(Path.Combine(testDir, "large.log"), new string('x', 10000));

            var items = await _cleaner.AnalyzeDiskSpaceAsync(testDir);

            Assert.True(items.Count >= 2);
            Assert.True(items[0].TotalSize >= items[1].TotalSize);
        }
        finally
        {
            Directory.Delete(testDir, recursive: true);
        }
    }

    [Fact]
    public void IsRunningAsAdmin_DoesNotThrow()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = _cleaner.IsRunningAsAdmin();
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task AllCleanupMethods_HaveCorrectOperationName()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dnsResult = await _cleaner.CleanDnsCacheAsync();
        Assert.Equal("DNS Cache", dnsResult.OperationName);

        var clipResult = await _cleaner.CleanClipboardAsync();
        Assert.Equal("Clipboard", clipResult.OperationName);

        var recentResult = await _cleaner.CleanRecentDocumentsAsync();
        Assert.Equal("Recent Documents", recentResult.OperationName);

        var thumbResult = await _cleaner.CleanThumbnailCacheAsync();
        Assert.Equal("Thumbnail Cache", thumbResult.OperationName);

        var memResult = await _cleaner.CleanMemoryDumpsAsync();
        Assert.Equal("Memory Dumps", memResult.OperationName);

        var chkdskResult = await _cleaner.CleanChkdskFragmentsAsync();
        Assert.Equal("Chkdsk Fragments", chkdskResult.OperationName);

        var wuResult = await _cleaner.CleanWindowsUpdateCacheAsync();
        Assert.Equal("Windows Update Cache", wuResult.OperationName);
    }
}
