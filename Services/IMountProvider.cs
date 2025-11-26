using Dsk.Models;

namespace Dsk.Services;

/// <summary>
/// Platform abstraction for retrieving mount information.
/// </summary>
public interface IMountProvider
{
    /// <summary>
    /// Get all mounts on the system.
    /// </summary>
    /// <returns>List of mounts and any warnings encountered.</returns>
    (List<Mount> Mounts, List<string> Warnings) GetMounts();
}

/// <summary>
/// Factory to create the appropriate mount provider for the current platform.
/// </summary>
public static class MountProviderFactory
{
    public static IMountProvider Create()
    {
        // Runtime detection - providers are conditionally compiled per platform
        if (OperatingSystem.IsWindows())
            return CreateWindowsProvider();
        if (OperatingSystem.IsLinux())
            return CreateLinuxProvider();
        if (OperatingSystem.IsMacOS())
            return CreateDarwinProvider();
            
        throw new PlatformNotSupportedException("Unsupported operating system");
    }
    
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static IMountProvider CreateWindowsProvider() => new WindowsMountProvider();
    
    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    private static IMountProvider CreateLinuxProvider() => new LinuxMountProvider();
    
    [System.Runtime.Versioning.SupportedOSPlatform("macos")]
    private static IMountProvider CreateDarwinProvider() => new DarwinMountProvider();
}

