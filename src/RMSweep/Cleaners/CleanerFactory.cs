using System.Runtime.InteropServices;
using RMSweep.Interfaces;

namespace RMSweep.Cleaners;

/// <summary>
/// Factory that returns the correct platform cleaner at runtime.
/// </summary>
public static class CleanerFactory
{
    public static ISystemCleaner Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsCleaner();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new MacCleaner();

        throw new PlatformNotSupportedException(
            "This platform is not supported. RM-Sweep runs on Windows and macOS only.");
    }
}
