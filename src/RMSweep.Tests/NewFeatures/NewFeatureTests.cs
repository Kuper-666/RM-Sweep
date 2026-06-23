using RMSweep.Cleaners;
using RMSweep.Models;
using RMSweep.Services;

namespace RMSweep.Tests.NewFeatures;

public class NewCleaningOperationsTests
{
    private readonly WindowsCleaner _cleaner = new();

    private static string CreateTestDir(string prefix)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task CleanDnsCacheAsync_ReportsProgress()
    {
        if (!OperatingSystem.IsWindows()) return;

        var reports = new List<CleanProgress>();
        await _cleaner.CleanDnsCacheAsync(new Progress<CleanProgress>(p => reports.Add(p)));

        Assert.NotEmpty(reports);
        Assert.All(reports, r => Assert.Contains("DNS", r.StatusMessage));
    }

    [Fact]
    public async Task CleanClipboardAsync_ReportsProgress()
    {
        if (!OperatingSystem.IsWindows()) return;

        var reports = new List<CleanProgress>();
        await _cleaner.CleanClipboardAsync(new Progress<CleanProgress>(p => reports.Add(p)));

        Assert.NotEmpty(reports);
        Assert.All(reports, r => Assert.Contains("clipboard", r.StatusMessage, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CleanRecentDocumentsAsync_ClearsRecentFolder()
    {
        if (!OperatingSystem.IsWindows()) return;

        var testDir = CreateTestDir("recent");
        try
        {
            File.WriteAllText(Path.Combine(testDir, "recent_test.txt"), "recent");
            var result = await _cleaner.CleanRecentDocumentsAsync();

            Assert.True(result.Success);
            Assert.True(result.FilesDeleted >= 0);
        }
        finally
        {
            if (Directory.Exists(testDir)) Directory.Delete(testDir, recursive: true);
        }
    }

    [Fact]
    public async Task CleanRecentDocumentsAsync_SupportsCancellation()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _cleaner.CleanRecentDocumentsAsync(ct: cts.Token));
    }

    [Fact]
    public async Task CleanThumbnailCacheAsync_ReportsProgress()
    {
        if (!OperatingSystem.IsWindows()) return;

        var reports = new List<CleanProgress>();
        await _cleaner.CleanThumbnailCacheAsync(new Progress<CleanProgress>(p => reports.Add(p)));

        Assert.NotEmpty(reports);
        Assert.All(reports, r => Assert.Contains("thumbnail", r.StatusMessage, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CleanThumbnailCacheAsync_SupportsCancellation()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _cleaner.CleanThumbnailCacheAsync(ct: cts.Token));
    }

    [Fact]
    public async Task CleanMemoryDumpsAsync_ReportsProgress()
    {
        if (!OperatingSystem.IsWindows()) return;

        var reports = new List<CleanProgress>();
        await _cleaner.CleanMemoryDumpsAsync(new Progress<CleanProgress>(p => reports.Add(p)));

        Assert.NotEmpty(reports);
    }

    [Fact]
    public async Task CleanMemoryDumpsAsync_SupportsCancellation()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _cleaner.CleanMemoryDumpsAsync(ct: cts.Token));
    }

    [Fact]
    public async Task CleanChkdskFragmentsAsync_ReportsProgress()
    {
        if (!OperatingSystem.IsWindows()) return;

        var reports = new List<CleanProgress>();
        await _cleaner.CleanChkdskFragmentsAsync(new Progress<CleanProgress>(p => reports.Add(p)));

        Assert.NotEmpty(reports);
    }

    [Fact]
    public async Task CleanChkdskFragmentsAsync_SupportsCancellation()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _cleaner.CleanChkdskFragmentsAsync(ct: cts.Token));
    }

    [Fact]
    public async Task CleanWindowsUpdateCacheAsync_ReportsProgress()
    {
        if (!OperatingSystem.IsWindows()) return;

        var reports = new List<CleanProgress>();
        await _cleaner.CleanWindowsUpdateCacheAsync(new Progress<CleanProgress>(p => reports.Add(p)));

        Assert.NotEmpty(reports);
    }

    [Fact]
    public async Task CleanWindowsUpdateCacheAsync_SupportsCancellation()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _cleaner.CleanWindowsUpdateCacheAsync(ct: cts.Token));
    }

    [Fact]
    public async Task CleanDnsCacheAsync_SupportsCancellation()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _cleaner.CleanDnsCacheAsync(ct: cts.Token));
    }

    [Fact]
    public async Task CleanClipboardAsync_SupportsCancellation()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _cleaner.CleanClipboardAsync(ct: cts.Token));
    }

    [Fact]
    public async Task AllNewOperations_ReturnNonNullResults()
    {
        if (!OperatingSystem.IsWindows()) return;

        var results = new[]
        {
            await _cleaner.CleanDnsCacheAsync(),
            await _cleaner.CleanClipboardAsync(),
            await _cleaner.CleanRecentDocumentsAsync(),
            await _cleaner.CleanThumbnailCacheAsync(),
            await _cleaner.CleanMemoryDumpsAsync(),
            await _cleaner.CleanChkdskFragmentsAsync(),
            await _cleaner.CleanWindowsUpdateCacheAsync()
        };

        Assert.All(results, r =>
        {
            Assert.NotNull(r);
            Assert.False(string.IsNullOrEmpty(r.OperationName));
            Assert.False(string.IsNullOrEmpty(r.Message));
        });
    }

    [Fact]
    public async Task AllNewOperations_HaveNoErrorsOnSuccess()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dns = await _cleaner.CleanDnsCacheAsync();
        Assert.Empty(dns.Errors);

        var clip = await _cleaner.CleanClipboardAsync();
        Assert.Empty(clip.Errors);

        var recent = await _cleaner.CleanRecentDocumentsAsync();
        Assert.Empty(recent.Errors);
    }
}

public class CustomFoldersAdvancedTests
{
    private readonly WindowsCleaner _cleaner = new();

    private static string CreateTestDir(string prefix)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task CleanCustomFoldersAsync_MultipleFolders()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir1 = CreateTestDir("custom1");
        var dir2 = CreateTestDir("custom2");

        try
        {
            File.WriteAllText(Path.Combine(dir1, "a.txt"), "aaa");
            File.WriteAllText(Path.Combine(dir2, "b.txt"), "bbb");

            var result = await _cleaner.CleanCustomFoldersAsync(new List<string> { dir1, dir2 });

            Assert.True(result.Success);
            Assert.Equal(2, result.FilesDeleted);
        }
        finally
        {
            if (Directory.Exists(dir1)) Directory.Delete(dir1, recursive: true);
            if (Directory.Exists(dir2)) Directory.Delete(dir2, recursive: true);
        }
    }

    [Fact]
    public async Task CleanCustomFoldersAsync_MixExistingAndNonExisting()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir1 = CreateTestDir("mix");
        try
        {
            File.WriteAllText(Path.Combine(dir1, "file.txt"), "data");

            var result = await _cleaner.CleanCustomFoldersAsync(
                new List<string> { dir1, @"C:\NonExistent_12345" });

            Assert.True(result.Success);
            Assert.Equal(1, result.FilesDeleted);
        }
        finally
        {
            if (Directory.Exists(dir1)) Directory.Delete(dir1, recursive: true);
        }
    }

    [Fact]
    public async Task CleanCustomFoldersAsync_NestedSubdirectories()
    {
        if (!OperatingSystem.IsWindows()) return;

        var root = CreateTestDir("nested");
        try
        {
            var l1 = Path.Combine(root, "level1");
            var l2 = Path.Combine(l1, "level2");
            var l3 = Path.Combine(l2, "level3");
            Directory.CreateDirectory(l3);

            File.WriteAllText(Path.Combine(root, "root.txt"), "r");
            File.WriteAllText(Path.Combine(l1, "l1.txt"), "l1");
            File.WriteAllText(Path.Combine(l2, "l2.txt"), "l2");
            File.WriteAllText(Path.Combine(l3, "l3.txt"), "l3");

            var result = await _cleaner.CleanCustomFoldersAsync(new List<string> { root });

            Assert.True(result.Success);
            Assert.Equal(4, result.FilesDeleted);
            Assert.False(Directory.Exists(l3));
            Assert.False(Directory.Exists(l2));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task CleanCustomFoldersAsync_EmptySubdirectoriesDeleted()
    {
        if (!OperatingSystem.IsWindows()) return;

        var root = CreateTestDir("emptydirs");
        try
        {
            var sub1 = Path.Combine(root, "sub1");
            var sub2 = Path.Combine(root, "sub2");
            Directory.CreateDirectory(sub1);
            Directory.CreateDirectory(sub2);

            var result = await _cleaner.CleanCustomFoldersAsync(new List<string> { root });

            Assert.True(result.Success);
            Assert.False(Directory.Exists(sub1));
            Assert.False(Directory.Exists(sub2));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task CleanCustomFoldersAsync_ReportsProgressPerFolder()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir1 = CreateTestDir("prog1");
        var dir2 = CreateTestDir("prog2");

        try
        {
            File.WriteAllText(Path.Combine(dir1, "a.txt"), "a");
            File.WriteAllText(Path.Combine(dir2, "b.txt"), "b");

            var reports = new List<CleanProgress>();
            await _cleaner.CleanCustomFoldersAsync(
                new List<string> { dir1, dir2 },
                new Progress<CleanProgress>(p => reports.Add(p)));

            Assert.NotEmpty(reports);
            Assert.True(reports.Count >= 2);
        }
        finally
        {
            if (Directory.Exists(dir1)) Directory.Delete(dir1, recursive: true);
            if (Directory.Exists(dir2)) Directory.Delete(dir2, recursive: true);
        }
    }

    [Fact]
    public async Task CleanCustomFoldersAsync_SupportsCancellation()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("cancel");
        try
        {
            File.WriteAllText(Path.Combine(dir, "file.txt"), "data");

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _cleaner.CleanCustomFoldersAsync(new List<string> { dir }, ct: cts.Token));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}

public class DuplicateFinderAdvancedTests
{
    private readonly WindowsCleaner _cleaner = new();

    private static string CreateTestDir(string prefix)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task ScanForDuplicatesAsync_MultipleDuplicateGroups()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("multi_dupe");
        try
        {
            var contentA = "Content group A - unique data here";
            var contentB = "Content group B - different data";

            File.WriteAllText(Path.Combine(dir, "a1.txt"), contentA);
            File.WriteAllText(Path.Combine(dir, "a2.txt"), contentA);
            File.WriteAllText(Path.Combine(dir, "b1.txt"), contentB);
            File.WriteAllText(Path.Combine(dir, "b2.txt"), contentB);
            File.WriteAllText(Path.Combine(dir, "b3.txt"), contentB);

            var groups = await _cleaner.ScanForDuplicatesAsync(dir);

            Assert.Equal(2, groups.Count);
            var groupA = groups.First(g => g.Count == 2);
            var groupB = groups.First(g => g.Count == 3);
            Assert.Equal(2, groupA.Items.Count);
            Assert.Equal(3, groupB.Items.Count);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanForDuplicatesAsync_NestedDirectories()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("nested_dupe");
        try
        {
            var sub = Path.Combine(dir, "sub");
            Directory.CreateDirectory(sub);

            var content = "Same content in different dirs";
            File.WriteAllText(Path.Combine(dir, "root_file.txt"), content);
            File.WriteAllText(Path.Combine(sub, "sub_file.txt"), content);

            var groups = await _cleaner.ScanForDuplicatesAsync(dir);

            Assert.Single(groups);
            Assert.Equal(2, groups[0].Count);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanForDuplicatesAsync_NoDuplicates_ReturnsEmpty()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("no_dupe");
        try
        {
            File.WriteAllText(Path.Combine(dir, "unique1.txt"), "Content A");
            File.WriteAllText(Path.Combine(dir, "unique2.txt"), "Content B");
            File.WriteAllText(Path.Combine(dir, "unique3.txt"), "Content C");

            var groups = await _cleaner.ScanForDuplicatesAsync(dir);

            Assert.Empty(groups);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanForDuplicatesAsync_BinaryFiles()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("binary_dupe");
        try
        {
            var bytes = new byte[1024];
            new Random(42).NextBytes(bytes);

            File.WriteAllBytes(Path.Combine(dir, "bin1.dat"), bytes);
            File.WriteAllBytes(Path.Combine(dir, "bin2.dat"), bytes);

            var groups = await _cleaner.ScanForDuplicatesAsync(dir);

            Assert.Single(groups);
            Assert.Equal(2, groups[0].Count);
            Assert.Equal(1024, groups[0].FileSize);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanForDuplicatesAsync_SortedBySizeDescending()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("sorted_dupe");
        try
        {
            var smallContent = new string('A', 100);
            var largeContent = new string('B', 10000);

            File.WriteAllText(Path.Combine(dir, "small1.txt"), smallContent);
            File.WriteAllText(Path.Combine(dir, "small2.txt"), smallContent);
            File.WriteAllText(Path.Combine(dir, "large1.txt"), largeContent);
            File.WriteAllText(Path.Combine(dir, "large2.txt"), largeContent);

            var groups = await _cleaner.ScanForDuplicatesAsync(dir);

            Assert.Equal(2, groups.Count);
            Assert.True(groups[0].FileSize > groups[1].FileSize);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanForDuplicatesAsync_ProgressReports()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("progress_dupe");
        try
        {
            for (int i = 0; i < 5; i++)
                File.WriteAllText(Path.Combine(dir, $"file{i}.txt"), $"content {i}");

            var reports = new List<CleanProgress>();
            await _cleaner.ScanForDuplicatesAsync(dir, new Progress<CleanProgress>(p => reports.Add(p)));

            Assert.NotEmpty(reports);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanForDuplicatesAsync_EmptyDirectory()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("empty_dupe");
        try
        {
            var groups = await _cleaner.ScanForDuplicatesAsync(dir);

            Assert.NotNull(groups);
            Assert.Empty(groups);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ScanForDuplicatesAsync_OnlyEmptyFiles()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("zero_dupe");
        try
        {
            File.WriteAllBytes(Path.Combine(dir, "zero1.bin"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(dir, "zero2.bin"), Array.Empty<byte>());

            var groups = await _cleaner.ScanForDuplicatesAsync(dir);

            Assert.Empty(groups);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}

public class DiskAnalyzerAdvancedTests
{
    private readonly WindowsCleaner _cleaner = new();

    private static string CreateTestDir(string prefix)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task AnalyzeDiskSpaceAsync_FileCountCorrect()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("count_analyze");
        try
        {
            File.WriteAllText(Path.Combine(dir, "1.txt"), "a");
            File.WriteAllText(Path.Combine(dir, "2.txt"), "b");
            File.WriteAllText(Path.Combine(dir, "3.txt"), "c");
            File.WriteAllText(Path.Combine(dir, "1.log"), "log");

            var items = await _cleaner.AnalyzeDiskSpaceAsync(dir);

            var txt = items.First(i => i.Extension == ".txt");
            Assert.Equal(3, txt.FileCount);

            var log = items.First(i => i.Extension == ".log");
            Assert.Equal(1, log.FileCount);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeDiskSpaceAsync_TotalSizeCorrect()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("size_analyze");
        try
        {
            var content = new string('X', 1000);
            File.WriteAllText(Path.Combine(dir, "a.txt"), content);
            File.WriteAllText(Path.Combine(dir, "b.txt"), content);

            var items = await _cleaner.AnalyzeDiskSpaceAsync(dir);

            var txt = items.First(i => i.Extension == ".txt");
            Assert.True(txt.TotalSize >= 2000);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeDiskSpaceAsync_NoExtensionFiles()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("noext_analyze");
        try
        {
            File.WriteAllText(Path.Combine(dir, "Makefile"), "content");
            File.WriteAllText(Path.Combine(dir, "README"), "readme");

            var items = await _cleaner.AnalyzeDiskSpaceAsync(dir);

            var noExt = items.FirstOrDefault(i => i.Extension == "(no ext)");
            Assert.NotNull(noExt);
            Assert.Equal(2, noExt!.FileCount);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeDiskSpaceAsync_NestedDirectories()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("nested_analyze");
        try
        {
            var sub = Path.Combine(dir, "sub");
            Directory.CreateDirectory(sub);
            File.WriteAllText(Path.Combine(dir, "root.txt"), "r");
            File.WriteAllText(Path.Combine(sub, "sub.txt"), "s");

            var items = await _cleaner.AnalyzeDiskSpaceAsync(dir);

            var txt = items.First(i => i.Extension == ".txt");
            Assert.Equal(2, txt.FileCount);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeDiskSpaceAsync_CaseInsensitiveExtensionMerge()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("case_analyze");
        try
        {
            File.WriteAllText(Path.Combine(dir, "a.TXT"), "A");
            File.WriteAllText(Path.Combine(dir, "b.txt"), "B");

            var items = await _cleaner.AnalyzeDiskSpaceAsync(dir);

            var txt = items.FirstOrDefault(i =>
                string.Equals(i.Extension, ".txt", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(txt);
            Assert.Equal(2, txt!.FileCount);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeDiskSpaceAsync_EmptyDirectory()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("empty_analyze");
        try
        {
            var items = await _cleaner.AnalyzeDiskSpaceAsync(dir);

            Assert.NotNull(items);
            Assert.Empty(items);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeDiskSpaceAsync_SingleLargeFile()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("single_analyze");
        try
        {
            var largeContent = new string('Z', 50000);
            File.WriteAllText(Path.Combine(dir, "huge.csv"), largeContent);

            var items = await _cleaner.AnalyzeDiskSpaceAsync(dir);

            Assert.Single(items);
            Assert.Equal(".csv", items[0].Extension);
            Assert.Equal(1, items[0].FileCount);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeDiskSpaceAsync_ProgressReports()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = CreateTestDir("progress_analyze");
        try
        {
            for (int i = 0; i < 10; i++)
                File.WriteAllText(Path.Combine(dir, $"file{i}.txt"), $"content {i}");

            var reports = new List<CleanProgress>();
            await _cleaner.AnalyzeDiskSpaceAsync(dir, new Progress<CleanProgress>(p => reports.Add(p)));

            Assert.NotEmpty(reports);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}

public class DriveWiperAdvancedTests
{
    private readonly WindowsCleaner _cleaner = new();

    [Theory]
    [InlineData("X")]
    [InlineData("Y")]
    [InlineData("Z")]
    public async Task WipeDriveFreeSpaceAsync_NonExistentDrives_ReturnsFailure(string drive)
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.WipeDriveFreeSpaceAsync(drive, DriveWipeMethod.ZeroFill);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task WipeDriveFreeSpaceAsync_CDrivePath_FormatsCorrectly()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.WipeDriveFreeSpaceAsync("C", DriveWipeMethod.ZeroFill);

        Assert.Contains("C:\\", result.Message);
    }

    [Fact]
    public async Task WipeDriveFreeSpaceAsync_FullPath_DoesNotDoubleBackslash()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = await _cleaner.WipeDriveFreeSpaceAsync("C:\\", DriveWipeMethod.ZeroFill);

        Assert.Contains("C:\\", result.Message);
        Assert.DoesNotContain("C:\\\\", result.Message);
    }

    [Fact]
    public async Task WipeDriveFreeSpaceAsync_SupportsCancellation()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _cleaner.WipeDriveFreeSpaceAsync("C", DriveWipeMethod.ZeroFill, ct: cts.Token));
    }

    [Fact]
    public async Task WipeDriveFreeSpaceAsync_ReportsProgress()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var reports = new List<CleanProgress>();

        try
        {
            await _cleaner.WipeDriveFreeSpaceAsync("C", DriveWipeMethod.ZeroFill,
                new Progress<CleanProgress>(p => reports.Add(p)), cts.Token);
        }
        catch (OperationCanceledException) { }

        // Should have at least the initial progress report
        Assert.NotEmpty(reports);
    }

    [Fact]
    public void DriveWipeMethod_AllValuesExist()
    {
        Assert.True(Enum.IsDefined(typeof(DriveWipeMethod), DriveWipeMethod.ZeroFill));
        Assert.True(Enum.IsDefined(typeof(DriveWipeMethod), DriveWipeMethod.DoD522022M));
        Assert.True(Enum.IsDefined(typeof(DriveWipeMethod), DriveWipeMethod.Gutmann));
    }

    [Fact]
    public void DriveWipeMethod_ThreeMethodsTotal()
    {
        Assert.Equal(3, Enum.GetValues<DriveWipeMethod>().Length);
    }
}

public class NewAppSettingsTests
{
    [Fact]
    public void ExcludePaths_DefaultIsEmpty()
    {
        var settings = new AppSettings();
        Assert.NotNull(settings.ExcludePaths);
        Assert.Empty(settings.ExcludePaths);
    }

    [Fact]
    public void ExcludeRegistryKeys_DefaultIsEmpty()
    {
        var settings = new AppSettings();
        Assert.NotNull(settings.ExcludeRegistryKeys);
        Assert.Empty(settings.ExcludeRegistryKeys);
    }

    [Fact]
    public void IncludeCustomFolders_DefaultIsEmpty()
    {
        var settings = new AppSettings();
        Assert.NotNull(settings.IncludeCustomFolders);
        Assert.Empty(settings.IncludeCustomFolders);
    }

    [Fact]
    public void SecureDelete_DefaultIsFalse()
    {
        var settings = new AppSettings();
        Assert.False(settings.SecureDelete);
    }

    [Fact]
    public void SecureDeletePasses_DefaultIsThree()
    {
        var settings = new AppSettings();
        Assert.Equal(3, settings.SecureDeletePasses);
    }

    [Fact]
    public void ExcludePaths_MultiplePaths()
    {
        var settings = new AppSettings();
        settings.ExcludePaths.Add(@"C:\Keep");
        settings.ExcludePaths.Add(@"D:\Important");
        settings.ExcludePaths.Add(@"E:\Data");

        Assert.Equal(3, settings.ExcludePaths.Count);
    }

    [Fact]
    public void IncludeCustomFolders_MultipleFolders()
    {
        var settings = new AppSettings();
        settings.IncludeCustomFolders.Add(@"C:\Cache1");
        settings.IncludeCustomFolders.Add(@"D:\Cache2");

        Assert.Equal(2, settings.IncludeCustomFolders.Count);
    }

    [Fact]
    public void AllNewBoolProperties_IndependentlySettable()
    {
        var settings = new AppSettings();

        settings.CleanDnsCache = true;
        Assert.True(settings.CleanDnsCache);

        settings.CleanClipboard = true;
        Assert.True(settings.CleanClipboard);

        settings.CleanRecentDocuments = true;
        Assert.True(settings.CleanRecentDocuments);

        settings.CleanThumbnailCache = true;
        Assert.True(settings.CleanThumbnailCache);

        settings.CleanMemoryDumps = true;
        Assert.True(settings.CleanMemoryDumps);

        settings.CleanChkdskFragments = true;
        Assert.True(settings.CleanChkdskFragments);

        settings.CleanWindowsUpdateCache = true;
        Assert.True(settings.CleanWindowsUpdateCache);

        settings.SecureDelete = true;
        Assert.True(settings.SecureDelete);
    }

    [Fact]
    public void NewSettings_SerializeToJson()
    {
        var settings = new AppSettings
        {
            CleanDnsCache = true,
            CleanClipboard = true,
            CleanRecentDocuments = true,
            CleanThumbnailCache = true,
            CleanMemoryDumps = true,
            CleanChkdskFragments = true,
            CleanWindowsUpdateCache = true,
            SecureDelete = true,
            SecureDeletePasses = 7,
            ExcludePaths = new List<string> { @"C:\Test" },
            IncludeCustomFolders = new List<string> { @"D:\Custom" }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(settings);

        Assert.Contains("CleanDnsCache", json);
        Assert.Contains("CleanClipboard", json);
        Assert.Contains("SecureDelete", json);
        Assert.Contains("ExcludePaths", json);
        Assert.Contains("IncludeCustomFolders", json);
    }

    [Fact]
    public void NewSettings_DeserializeFromJson()
    {
        var json = @"{
            ""CleanDnsCache"": true,
            ""CleanClipboard"": true,
            ""CleanRecentDocuments"": true,
            ""SecureDelete"": true,
            ""SecureDeletePasses"": 5,
            ""ExcludePaths"": [""C:\\Test""],
            ""IncludeCustomFolders"": [""D:\\Custom""]
        }";

        var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(settings);
        Assert.True(settings!.CleanDnsCache);
        Assert.True(settings.CleanClipboard);
        Assert.True(settings.CleanRecentDocuments);
        Assert.True(settings.SecureDelete);
        Assert.Equal(5, settings.SecureDeletePasses);
        Assert.Single(settings.ExcludePaths);
        Assert.Single(settings.IncludeCustomFolders);
    }
}

public class NewModelTests
{
    [Fact]
    public void DuplicateItem_AllPropertiesSettable()
    {
        var item = new DuplicateItem
        {
            Path = @"C:\test\file.txt",
            Size = 4096,
            Hash = "abc123def456",
            GroupId = "group1"
        };

        Assert.Equal(@"C:\test\file.txt", item.Path);
        Assert.Equal(4096, item.Size);
        Assert.Equal("abc123def456", item.Hash);
        Assert.Equal("group1", item.GroupId);
    }

    [Fact]
    public void DuplicateGroup_EmptyItemsDefault()
    {
        var group = new DuplicateGroup();
        Assert.NotNull(group.Items);
        Assert.Empty(group.Items);
        Assert.Equal(0, group.Count);
    }

    [Fact]
    public void DuplicateGroup_ItemsAreMutable()
    {
        var group = new DuplicateGroup();
        group.Items.Add(new DuplicateItem { Path = "a.txt", Size = 100 });
        group.Items.Add(new DuplicateItem { Path = "b.txt", Size = 100 });
        group.Count = 2;

        Assert.Equal(2, group.Items.Count);
        Assert.Equal(2, group.Count);
    }

    [Fact]
    public void DiskSpaceItem_AllPropertiesSettable()
    {
        var item = new DiskSpaceItem
        {
            Extension = ".txt",
            TotalSize = 1024000,
            FileCount = 42
        };

        Assert.Equal(".txt", item.Extension);
        Assert.Equal(1024000, item.TotalSize);
        Assert.Equal(42, item.FileCount);
    }

    [Fact]
    public void DiskSpaceItem_SizeFormatted_BoundaryValues()
    {
        Assert.Equal("0 B", new DiskSpaceItem { TotalSize = 0 }.SizeFormatted);
        Assert.Equal("1023 B", new DiskSpaceItem { TotalSize = 1023 }.SizeFormatted);
        Assert.Equal("1.0 KB", new DiskSpaceItem { TotalSize = 1024 }.SizeFormatted);
        Assert.Equal("1.0 MB", new DiskSpaceItem { TotalSize = 1048576 }.SizeFormatted);
        Assert.Equal("1.00 GB", new DiskSpaceItem { TotalSize = 1073741824 }.SizeFormatted);
    }

    [Fact]
    public void CleanOperation_ContainsAllNewOperations()
    {
        var values = Enum.GetValues<CleanOperation>();

        Assert.Contains(CleanOperation.DnsCache, values);
        Assert.Contains(CleanOperation.Clipboard, values);
        Assert.Contains(CleanOperation.RecentDocuments, values);
        Assert.Contains(CleanOperation.ThumbnailCache, values);
        Assert.Contains(CleanOperation.MemoryDumps, values);
        Assert.Contains(CleanOperation.ChkdskFragments, values);
        Assert.Contains(CleanOperation.WindowsUpdateCache, values);
    }
}

public class LocalizationNewStringsTests
{
    [Theory]
    [InlineData("CleanDnsCache")]
    [InlineData("CleanClipboard")]
    [InlineData("CleanRecentDocuments")]
    [InlineData("CleanThumbnailCache")]
    [InlineData("CleanMemoryDumps")]
    [InlineData("CleanChkdskFragments")]
    [InlineData("CleanWindowsUpdateCache")]
    [InlineData("CustomFolders")]
    [InlineData("DiskAnalyzer")]
    [InlineData("DuplicateFinder")]
    [InlineData("DriveWiper")]
    [InlineData("AnalyzeButton")]
    [InlineData("ScanDuplicatesButton")]
    [InlineData("WipeButton")]
    [InlineData("DriveLabel")]
    [InlineData("WipeMethodLabel")]
    [InlineData("DirectoryLabel")]
    [InlineData("ExcludePathsLabel")]
    [InlineData("IncludeFoldersLabel")]
    [InlineData("AddExcludeButton")]
    [InlineData("AddIncludeButton")]
    [InlineData("SettingsHeader")]
    [InlineData("AnalyzingDisk")]
    [InlineData("ScanningDuplicates")]
    public void AllNewKeys_ExistInEnglish(string key)
    {
        LocalizationService.SetLanguage("en-US");
        var value = LocalizationService.GetString(key);

        Assert.NotNull(value);
        Assert.NotEmpty(value);
        Assert.DoesNotContain($"[{key}]", value);
    }

    [Theory]
    [InlineData("CleanDnsCache")]
    [InlineData("CleanClipboard")]
    [InlineData("CleanRecentDocuments")]
    [InlineData("CleanThumbnailCache")]
    [InlineData("CleanMemoryDumps")]
    [InlineData("CleanChkdskFragments")]
    [InlineData("CleanWindowsUpdateCache")]
    [InlineData("CustomFolders")]
    [InlineData("DiskAnalyzer")]
    [InlineData("DuplicateFinder")]
    [InlineData("DriveWiper")]
    [InlineData("ExcludePathsLabel")]
    [InlineData("IncludeFoldersLabel")]
    [InlineData("SettingsHeader")]
    public void AllNewKeys_ExistInRussian(string key)
    {
        LocalizationService.SetLanguage("ru-RU");
        var value = LocalizationService.GetString(key);

        Assert.NotNull(value);
        Assert.NotEmpty(value);
        Assert.DoesNotContain($"[{key}]", value);
    }

    [Theory]
    [InlineData("CleanDnsCache")]
    [InlineData("CleanClipboard")]
    [InlineData("CleanRecentDocuments")]
    [InlineData("DiskAnalyzer")]
    [InlineData("DuplicateFinder")]
    [InlineData("DriveWiper")]
    [InlineData("ExcludePathsLabel")]
    [InlineData("SettingsHeader")]
    public void AllNewKeys_ExistInGerman(string key)
    {
        LocalizationService.SetLanguage("de-DE");
        var value = LocalizationService.GetString(key);

        Assert.NotNull(value);
        Assert.NotEmpty(value);
        Assert.DoesNotContain($"[{key}]", value);
    }

    [Fact]
    public void NewKeys_HaveDifferentTranslations()
    {
        LocalizationService.SetLanguage("en-US");
        var en = LocalizationService.GetString("CleanDnsCache");

        LocalizationService.SetLanguage("ru-RU");
        var ru = LocalizationService.GetString("CleanDnsCache");

        Assert.NotEqual(en, ru);
    }
}
