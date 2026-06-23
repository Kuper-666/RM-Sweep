using RMSweep.Models;

namespace RMSweep.Tests.Models;

public class CleanResultTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var result = new CleanResult();

        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.OperationName);
        Assert.Equal(string.Empty, result.Message);
        Assert.Equal(0L, result.BytesFreed);
        Assert.Equal(0, result.FilesDeleted);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void BytesFreed_CanBeSetToLargeValues()
    {
        var result = new CleanResult { BytesFreed = long.MaxValue };
        Assert.Equal(long.MaxValue, result.BytesFreed);
    }

    [Fact]
    public void FilesDeleted_CanBeSetToNegative()
    {
        var result = new CleanResult { FilesDeleted = -5 };
        Assert.Equal(-5, result.FilesDeleted);
    }

    [Fact]
    public void Errors_ListIsMutable()
    {
        var result = new CleanResult();
        result.Errors.Add("test error");
        result.Errors.Add("another error");

        Assert.Equal(2, result.Errors.Count);
        Assert.Equal("test error", result.Errors[0]);
    }
}

public class CleanProgressTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var progress = new CleanProgress();

        Assert.Equal(0.0, progress.PercentComplete);
        Assert.Equal(string.Empty, progress.CurrentOperation);
        Assert.Equal(string.Empty, progress.StatusMessage);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(50.0)]
    [InlineData(100.0)]
    [InlineData(-1.0)]
    public void PercentComplete_AcceptsAnyDouble(double value)
    {
        var progress = new CleanProgress { PercentComplete = value };
        Assert.Equal(value, progress.PercentComplete);
    }
}

public class LogEntryTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var entry = new LogEntry();

        Assert.Equal(default(DateTime), entry.Timestamp);
        Assert.Equal(string.Empty, entry.Operation);
        Assert.Equal(string.Empty, entry.Status);
        Assert.Equal(string.Empty, entry.Details);
        Assert.Equal(LogLevel.Info, entry.Level);
    }

    [Theory]
    [InlineData(LogLevel.Info)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Success)]
    public void Level_AcceptsAllEnumValues(LogLevel level)
    {
        var entry = new LogEntry { Level = level };
        Assert.Equal(level, entry.Level);
    }
}

public class AppSettingsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var settings = new AppSettings();

        Assert.True(settings.CleanTemporaryFiles);
        Assert.True(settings.CleanBrowserCache);
        Assert.False(settings.CleanRecycleBin);
        Assert.False(settings.CleanSystemSettings);
        Assert.False(settings.CleanAutostart);
        Assert.False(settings.CleanSystemLogs);
        Assert.False(settings.CleanDnsCache);
        Assert.False(settings.CleanClipboard);
        Assert.False(settings.CleanRecentDocuments);
        Assert.False(settings.CleanThumbnailCache);
        Assert.False(settings.CleanMemoryDumps);
        Assert.False(settings.CleanChkdskFragments);
        Assert.False(settings.CleanWindowsUpdateCache);
        Assert.True(settings.ScanInstalledApps);
        Assert.Equal("en-US", settings.Language);
        Assert.NotNull(settings.ExcludePaths);
        Assert.Empty(settings.ExcludePaths);
        Assert.NotNull(settings.ExcludeRegistryKeys);
        Assert.Empty(settings.ExcludeRegistryKeys);
        Assert.NotNull(settings.IncludeCustomFolders);
        Assert.Empty(settings.IncludeCustomFolders);
        Assert.False(settings.SecureDelete);
        Assert.Equal(3, settings.SecureDeletePasses);
    }

    [Fact]
    public void AllBoolProperties_AreIndependentlySettable()
    {
        var settings = new AppSettings();

        settings.CleanTemporaryFiles = false;
        Assert.False(settings.CleanTemporaryFiles);

        settings.CleanBrowserCache = false;
        Assert.False(settings.CleanBrowserCache);

        settings.CleanRecycleBin = true;
        Assert.True(settings.CleanRecycleBin);

        settings.CleanDnsCache = true;
        Assert.True(settings.CleanDnsCache);

        settings.CleanClipboard = true;
        Assert.True(settings.CleanClipboard);

        settings.CleanMemoryDumps = true;
        Assert.True(settings.CleanMemoryDumps);
    }

    [Fact]
    public void ExcludePaths_CanBeModified()
    {
        var settings = new AppSettings();
        settings.ExcludePaths.Add(@"C:\Test");
        settings.ExcludePaths.Add(@"D:\Exclude");

        Assert.Equal(2, settings.ExcludePaths.Count);
        Assert.Contains(@"C:\Test", settings.ExcludePaths);
    }

    [Fact]
    public void IncludeCustomFolders_CanBeModified()
    {
        var settings = new AppSettings();
        settings.IncludeCustomFolders.Add(@"C:\MyCache");

        Assert.Single(settings.IncludeCustomFolders);
    }

    [Fact]
    public void Language_CanBeSetToRu()
    {
        var settings = new AppSettings { Language = "ru-RU" };
        Assert.Equal("ru-RU", settings.Language);
    }

    [Fact]
    public void SecureDeletePasses_AcceptsVariousValues()
    {
        var settings = new AppSettings { SecureDeletePasses = 7 };
        Assert.Equal(7, settings.SecureDeletePasses);

        settings.SecureDeletePasses = 0;
        Assert.Equal(0, settings.SecureDeletePasses);

        settings.SecureDeletePasses = 35;
        Assert.Equal(35, settings.SecureDeletePasses);
    }
}

public class InstalledAppTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var app = new InstalledApp();

        Assert.Equal(string.Empty, app.Name);
        Assert.Equal(string.Empty, app.Publisher);
        Assert.Equal(string.Empty, app.InstallLocation);
        Assert.Equal(string.Empty, app.UninstallString);
        Assert.Equal(0L, app.EstimatedSize);
        Assert.NotNull(app.Leftovers);
        Assert.Empty(app.Leftovers);
    }

    [Fact]
    public void Leftovers_CanBePopulated()
    {
        var app = new InstalledApp
        {
            Name = "TestApp",
            Leftovers = new List<LeftoverFile>
            {
                new() { Path = @"C:\test\file.log", Type = "File", Size = 1024 },
                new() { Path = @"C:\test\dir", Type = "Directory", Size = 4096 }
            }
        };

        Assert.Equal(2, app.Leftovers.Count);
        Assert.Equal("File", app.Leftovers[0].Type);
        Assert.Equal("Directory", app.Leftovers[1].Type);
    }
}

public class DiskInfoTests
{
    [Fact]
    public void UsedBytes_IsCalculatedCorrectly()
    {
        var disk = new DiskInfo { TotalBytes = 1000, FreeBytes = 300 };
        Assert.Equal(700, disk.UsedBytes);
    }

    [Fact]
    public void UsedPercent_IsCalculatedCorrectly()
    {
        var disk = new DiskInfo { TotalBytes = 1000, FreeBytes = 250 };
        Assert.Equal(75.0, disk.UsedPercent);
    }

    [Fact]
    public void UsedPercent_ReturnsZero_WhenTotalIsZero()
    {
        var disk = new DiskInfo { TotalBytes = 0, FreeBytes = 0 };
        Assert.Equal(0.0, disk.UsedPercent);
    }

    [Theory]
    [InlineData(500, "500 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1073741824, "1.00 GB")]
    public void TotalFormatted_FormatsBytesCorrectly(long bytes, string expected)
    {
        var disk = new DiskInfo { TotalBytes = bytes };
        Assert.Equal(expected, disk.TotalFormatted);
    }

    [Theory]
    [InlineData(100, "100 B")]
    [InlineData(2048, "2.0 KB")]
    [InlineData(2097152, "2.0 MB")]
    public void FreeFormatted_FormatsBytesCorrectly(long bytes, string expected)
    {
        var disk = new DiskInfo { FreeBytes = bytes };
        Assert.Equal(expected, disk.FreeFormatted);
    }

    [Fact]
    public void UsageFormatted_CombinesUsedAndTotal()
    {
        var disk = new DiskInfo { TotalBytes = 1048576, FreeBytes = 524288 };
        var usage = disk.UsageFormatted;
        Assert.Contains("MB", usage);
        Assert.Contains("/", usage);
    }

    [Fact]
    public void ZeroBytes_AllFormatsWork()
    {
        var disk = new DiskInfo { TotalBytes = 0, FreeBytes = 0 };
        Assert.Equal("0 B", disk.TotalFormatted);
        Assert.Equal("0 B", disk.FreeFormatted);
        Assert.Equal("0 B", disk.UsedFormatted);
    }

    [Fact]
    public void NegativeFreeBytes_DoesNotThrow()
    {
        var disk = new DiskInfo { TotalBytes = 100, FreeBytes = -50 };
        Assert.Equal(150, disk.UsedBytes);
    }
}

public class LeftoverFileTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var file = new LeftoverFile();

        Assert.Equal(string.Empty, file.Path);
        Assert.False(file.IsHidden);
        Assert.False(file.IsSystem);
        Assert.Equal(0L, file.Size);
        Assert.Equal(string.Empty, file.Type);
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        var file = new LeftoverFile
        {
            Path = @"C:\test\file.dll",
            IsHidden = true,
            IsSystem = true,
            Size = 1024000,
            Type = "File"
        };

        Assert.Equal(@"C:\test\file.dll", file.Path);
        Assert.True(file.IsHidden);
        Assert.True(file.IsSystem);
        Assert.Equal(1024000, file.Size);
        Assert.Equal("File", file.Type);
    }
}

public class DuplicateItemTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var item = new DuplicateItem();

        Assert.Equal(string.Empty, item.Path);
        Assert.Equal(0L, item.Size);
        Assert.Equal(string.Empty, item.Hash);
        Assert.Equal(string.Empty, item.GroupId);
    }
}

public class DuplicateGroupTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var group = new DuplicateGroup();

        Assert.Equal(string.Empty, group.Hash);
        Assert.Equal(0L, group.FileSize);
        Assert.Equal(0, group.Count);
        Assert.NotNull(group.Items);
        Assert.Empty(group.Items);
    }

    [Fact]
    public void Items_CanBePopulated()
    {
        var group = new DuplicateGroup
        {
            Hash = "abc123",
            FileSize = 4096,
            Count = 3,
            Items = new List<DuplicateItem>
            {
                new() { Path = "a.txt", Size = 4096, Hash = "abc123" },
                new() { Path = "b.txt", Size = 4096, Hash = "abc123" },
                new() { Path = "c.txt", Size = 4096, Hash = "abc123" }
            }
        };

        Assert.Equal(3, group.Items.Count);
        Assert.All(group.Items, i => Assert.Equal("abc123", i.Hash));
    }
}

public class DiskSpaceItemTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var item = new DiskSpaceItem();

        Assert.Equal(string.Empty, item.Extension);
        Assert.Equal(0L, item.TotalSize);
        Assert.Equal(0, item.FileCount);
    }

    [Theory]
    [InlineData(500, "500 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1073741824, "1.00 GB")]
    public void SizeFormatted_FormatsBytesCorrectly(long bytes, string expected)
    {
        var item = new DiskSpaceItem { TotalSize = bytes };
        Assert.Equal(expected, item.SizeFormatted);
    }
}

public class CleanOperationTests
{
    [Fact]
    public void AllEnumValues_Exist()
    {
        var values = Enum.GetValues<CleanOperation>();

        Assert.Contains(CleanOperation.TemporaryFiles, values);
        Assert.Contains(CleanOperation.BrowserCache, values);
        Assert.Contains(CleanOperation.SystemSettings, values);
        Assert.Contains(CleanOperation.Autostart, values);
        Assert.Contains(CleanOperation.RecycleBin, values);
        Assert.Contains(CleanOperation.SystemLogs, values);
        Assert.Contains(CleanOperation.DnsCache, values);
        Assert.Contains(CleanOperation.Clipboard, values);
        Assert.Contains(CleanOperation.RecentDocuments, values);
        Assert.Contains(CleanOperation.ThumbnailCache, values);
        Assert.Contains(CleanOperation.MemoryDumps, values);
        Assert.Contains(CleanOperation.ChkdskFragments, values);
        Assert.Contains(CleanOperation.WindowsUpdateCache, values);
    }

    [Fact]
    public void EnumCount_IsCorrect()
    {
        Assert.Equal(13, Enum.GetValues<CleanOperation>().Length);
    }
}

public class DriveWipeMethodTests
{
    [Fact]
    public void AllEnumValues_Exist()
    {
        Assert.True(Enum.IsDefined(typeof(DriveWipeMethod), DriveWipeMethod.ZeroFill));
        Assert.True(Enum.IsDefined(typeof(DriveWipeMethod), DriveWipeMethod.DoD522022M));
        Assert.True(Enum.IsDefined(typeof(DriveWipeMethod), DriveWipeMethod.Gutmann));
    }

    [Fact]
    public void EnumCount_IsCorrect()
    {
        Assert.Equal(3, Enum.GetValues<DriveWipeMethod>().Length);
    }
}

public class LogLevelTests
{
    [Fact]
    public void AllEnumValues_Exist()
    {
        Assert.True(Enum.IsDefined(typeof(LogLevel), LogLevel.Info));
        Assert.True(Enum.IsDefined(typeof(LogLevel), LogLevel.Warning));
        Assert.True(Enum.IsDefined(typeof(LogLevel), LogLevel.Error));
        Assert.True(Enum.IsDefined(typeof(LogLevel), LogLevel.Success));
    }
}
