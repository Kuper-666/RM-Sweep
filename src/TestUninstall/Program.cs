using RMSweep.Cleaners;
using RMSweep.Models;

var cleaner = CleanerFactory.Create();

Console.WriteLine("=== RM-Sweep Auto Uninstall Test ===");
Console.WriteLine($"Platform: {cleaner.PlatformName}, Admin: {cleaner.IsRunningAsAdmin()}\n");

Console.WriteLine("Scanning installed apps...");
var progress = new Progress<CleanProgress>(p =>
    Console.Write($"\r  [{p.PercentComplete:F0}%] {p.CurrentOperation,-50}"));

var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
var apps = await cleaner.ScanInstalledAppsAsync(progress, cts.Token);
Console.WriteLine($"\nFound {apps.Count} apps\n");

// Find a small safe app to test uninstall
var testTargets = new[] { "REPO", "People Playground", "FreeZero" };
var target = apps.FirstOrDefault(a => testTargets.Any(t => a.Name.Contains(t, StringComparison.OrdinalIgnoreCase)));

if (target == null)
{
    Console.WriteLine("No test target found. Listing all apps:");
    for (int i = 0; i < Math.Min(apps.Count, 20); i++)
        Console.WriteLine($"  [{i}] {apps[i].Name} ({FormatBytes(apps[i].EstimatedSize)})");
    return;
}

Console.WriteLine($"Target: {target.Name}");
Console.WriteLine($"  Publisher: {target.Publisher}");
Console.WriteLine($"  Location: {target.InstallLocation}");
Console.WriteLine($"  Uninstall: {target.UninstallString}");
Console.WriteLine($"  Leftovers: {target.Leftovers.Count}");
foreach (var l in target.Leftovers)
    Console.WriteLine($"    [{l.Type}] {l.Path} ({FormatBytes(l.Size)})");

Console.WriteLine("\n--- Starting uninstall ---");
cts = new CancellationTokenSource(TimeSpan.FromSeconds(300));
var sw = System.Diagnostics.Stopwatch.StartNew();

var result = await cleaner.UninstallAppAsync(target, cts.Token);

sw.Stop();
Console.WriteLine($"\nResult: {(result ? "SUCCESS" : "PARTIAL")}");
Console.WriteLine($"Time: {sw.Elapsed.TotalSeconds:F1}s");

if (!string.IsNullOrEmpty(target.InstallLocation))
    Console.WriteLine($"Install dir exists: {Directory.Exists(target.InstallLocation)}");

Console.WriteLine("\n--- Post-uninstall rescan ---");
cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
var rescan = await cleaner.ScanInstalledAppsAsync(null, cts.Token);
var still = rescan.FirstOrDefault(a => a.Name == target.Name);
if (still != null)
{
    Console.WriteLine($"Still in registry: YES (leftovers: {still.Leftovers.Count})");
    foreach (var l in still.Leftovers)
        Console.WriteLine($"  [{l.Type}] {l.Path}");
}
else
{
    Console.WriteLine("Still in registry: NO (cleanly removed)");
}

Console.WriteLine("\nDone.");

static string FormatBytes(long bytes) => bytes switch
{
    < 1024 => $"{bytes} B",
    < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
    < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
    _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
};
