using RMSweep.Models;
using RMSweep.Services;

namespace RMSweep.Tests.EdgeCases;

public class EdgeCaseTests
{
    [Fact]
    public void DiskInfo_ExtremeValues_DoesNotOverflow()
    {
        var disk = new DiskInfo
        {
            TotalBytes = long.MaxValue,
            FreeBytes = long.MaxValue / 2
        };

        Assert.Equal(long.MaxValue / 2 + 1, disk.UsedBytes);
        Assert.InRange(disk.UsedPercent, 49, 51);
    }

    [Fact]
    public void DiskInfo_FormatsLargeValues()
    {
        var disk = new DiskInfo
        {
            TotalBytes = 10L * 1024 * 1024 * 1024, // 10 GB
            FreeBytes = 5L * 1024 * 1024 * 1024     // 5 GB
        };

        Assert.Contains("GB", disk.TotalFormatted);
        Assert.Contains("GB", disk.FreeFormatted);
        Assert.Contains("GB", disk.UsedFormatted);
    }

    [Fact]
    public void CleanResult_CanAccumulateMultipleErrors()
    {
        var result = new CleanResult();
        for (int i = 0; i < 1000; i++)
        {
            result.Errors.Add($"Error {i}");
        }

        Assert.Equal(1000, result.Errors.Count);
    }

    [Fact]
    public void InstalledApp_EmptyStrings_DoNotThrow()
    {
        var app = new InstalledApp
        {
            Name = "",
            Publisher = "",
            InstallLocation = "",
            UninstallString = ""
        };

        Assert.Equal("", app.Name);
        Assert.Equal(0L, app.EstimatedSize);
    }

    [Fact]
    public void InstalledApp_VeryLongStrings_DoesNotThrow()
    {
        var longString = new string('A', 10000);
        var app = new InstalledApp
        {
            Name = longString,
            Publisher = longString,
            InstallLocation = longString,
            UninstallString = longString
        };

        Assert.Equal(10000, app.Name.Length);
    }

    [Fact]
    public void DuplicateGroup_VeryLargeItemCount()
    {
        var group = new DuplicateGroup
        {
            Count = 10000,
            Items = Enumerable.Range(0, 10000)
                .Select(i => new DuplicateItem { Path = $"file{i}.txt", Size = 100 })
                .ToList()
        };

        Assert.Equal(10000, group.Items.Count);
    }

    [Fact]
    public void DiskSpaceItem_ZeroSize_FormatsCorrectly()
    {
        var item = new DiskSpaceItem { TotalSize = 0 };
        Assert.Equal("0 B", item.SizeFormatted);
    }

    [Fact]
    public void DiskSpaceItem_NegativeSize_DoesNotThrow()
    {
        var item = new DiskSpaceItem { TotalSize = -100 };
        var formatted = item.SizeFormatted;
        Assert.NotNull(formatted);
    }

    [Fact]
    public void LogEntry_VeryLongMessage_DoesNotThrow()
    {
        var entry = new LogEntry
        {
            Details = new string('X', 100000),
            Operation = new string('Y', 100000)
        };

        Assert.Equal(100000, entry.Details.Length);
    }

    [Fact]
    public void AppSettings_ExcludePaths_CanHoldManyItems()
    {
        var settings = new AppSettings();
        for (int i = 0; i < 1000; i++)
        {
            settings.ExcludePaths.Add($@"C:\Path{i}");
        }

        Assert.Equal(1000, settings.ExcludePaths.Count);
    }

    [Fact]
    public void AppSettings_SecureDeletePasses_NegativeValue()
    {
        var settings = new AppSettings { SecureDeletePasses = -1 };
        Assert.Equal(-1, settings.SecureDeletePasses);
    }

    [Fact]
    public void AppSettings_SecureDeletePasses_VeryLargeValue()
    {
        var settings = new AppSettings { SecureDeletePasses = int.MaxValue };
        Assert.Equal(int.MaxValue, settings.SecureDeletePasses);
    }

    [Fact]
    public void LeftoverFile_VeryLargeSize()
    {
        var file = new LeftoverFile { Size = long.MaxValue };
        Assert.Equal(long.MaxValue, file.Size);
    }

    [Fact]
    public void DuplicateItem_VeryLongPath()
    {
        var item = new DuplicateItem
        {
            Path = new string('C', 10000) + ".txt",
            Hash = new string('A', 64)
        };

        Assert.Equal(10004, item.Path.Length);
        Assert.Equal(64, item.Hash.Length);
    }
}

public class ConcurrencyTests
{
    [Fact]
    public void LogService_ConcurrentAdds_DoNotCorrupt()
    {
        var logService = new LogService();
        var exceptions = new List<Exception>();

        Parallel.For(0, 1000, i =>
        {
            try
            {
                logService.AddInfo("Op", $"Message {i}");
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
        Assert.Equal(1000, logService.Entries.Count);
    }

    [Fact]
    public void LogService_ConcurrentAddAndClear_DoesNotThrow()
    {
        var logService = new LogService();
        var exceptions = new List<Exception>();

        var addTask = Task.Run(() =>
        {
            for (int i = 0; i < 500; i++)
            {
                try { logService.AddInfo("Op", $"msg {i}"); }
                catch (Exception ex) { lock (exceptions) exceptions.Add(ex); }
            }
        });

        var clearTask = Task.Run(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                try { logService.Clear(); }
                catch (Exception ex) { lock (exceptions) exceptions.Add(ex); }
            }
        });

        Task.WaitAll(addTask, clearTask);
        Assert.Empty(exceptions);
    }

    [Fact]
    public void LogService_ConcurrentReadAndWrite_DoesNotThrow()
    {
        var logService = new LogService();
        var exceptions = new List<Exception>();

        var writeTask = Task.Run(() =>
        {
            for (int i = 0; i < 500; i++)
            {
                try { logService.AddInfo("Op", $"msg {i}"); }
                catch (Exception ex) { lock (exceptions) exceptions.Add(ex); }
            }
        });

        var readTask = Task.Run(() =>
        {
            for (int i = 0; i < 500; i++)
            {
                try
                {
                    var entries = logService.Entries;
                    Assert.NotNull(entries);
                }
                catch (Exception ex) { lock (exceptions) exceptions.Add(ex); }
            }
        });

        Task.WaitAll(writeTask, readTask);
        Assert.Empty(exceptions);
    }
}

public class StringFormattingTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1, "1 B")]
    [InlineData(512, "512 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1073741824, "1.00 GB")]
    public void DiskInfo_FormatBytes_CorrectOutput(long bytes, string expected)
    {
        var disk = new DiskInfo { TotalBytes = bytes };
        Assert.Equal(expected, disk.TotalFormatted);
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1048576, "1.0 MB")]
    public void DiskSpaceItem_FormatBytes_CorrectOutput(long bytes, string expected)
    {
        var item = new DiskSpaceItem { TotalSize = bytes };
        Assert.Equal(expected, item.SizeFormatted);
    }
}

public class JsonSerializationTests
{
    [Fact]
    public void AppSettings_SerializesAndDeserializes()
    {
        var settings = new AppSettings
        {
            Language = "ru-RU",
            CleanRecycleBin = true,
            CleanDnsCache = true,
            SecureDeletePasses = 5,
            ExcludePaths = new List<string> { @"C:\Test", @"D:\Exclude" },
            IncludeCustomFolders = new List<string> { @"E:\Custom" }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("ru-RU", deserialized!.Language);
        Assert.True(deserialized.CleanRecycleBin);
        Assert.True(deserialized.CleanDnsCache);
        Assert.Equal(5, deserialized.SecureDeletePasses);
        Assert.Equal(2, deserialized.ExcludePaths.Count);
        Assert.Single(deserialized.IncludeCustomFolders);
    }

    [Fact]
    public void AppSettings_DeserializesPartialJson()
    {
        var json = @"{""Language"":""de-DE"",""CleanRecycleBin"":true}";
        var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(settings);
        Assert.Equal("de-DE", settings!.Language);
        Assert.True(settings.CleanRecycleBin);
        // Other values should be defaults
        Assert.True(settings.CleanTemporaryFiles);
        Assert.Equal(3, settings.SecureDeletePasses);
    }

    [Fact]
    public void AppSettings_DeserializesEmptyJson()
    {
        var json = "{}";
        var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(settings);
        Assert.Equal("en-US", settings!.Language);
        Assert.True(settings.CleanTemporaryFiles);
    }
}
