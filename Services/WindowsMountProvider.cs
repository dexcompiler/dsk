using System.Runtime.InteropServices;
using Dsk.Models;
using Dsk.Interop.Windows;

namespace Dsk.Services;

/// <summary>
/// Windows mount provider - enumerates volumes using Win32 APIs.
/// </summary>
public sealed class WindowsMountProvider : IMountProvider
{
    public (List<Mount> Mounts, List<string> Warnings) GetMounts()
    {
        var mounts = new List<Mount>();
        var warnings = new List<string>();
        
        // Get local volumes
        GetLocalVolumes(mounts, warnings);
        
        // Get network drives
        GetNetworkDrives(mounts, warnings);
        
        // Get any additional logical drives not yet found
        GetLogicalDrives(mounts, warnings);
        
        return (mounts, warnings);
    }
    
    private static void GetLocalVolumes(List<Mount> mounts, List<string> warnings)
    {
        var volumeGuid = new char[WindowsNative.MAX_PATH + 1];
        
        var hFindVolume = WindowsNative.FindFirstVolume(volumeGuid, (uint)volumeGuid.Length);
        if (hFindVolume == nint.Zero || hFindVolume == new nint(-1))
        {
            warnings.Add($"FindFirstVolume failed: {Marshal.GetLastWin32Error()}");
            return;
        }
        
        try
        {
            do
            {
                var volumeName = new string(volumeGuid).TrimEnd('\0');
                var (mount, mountWarnings, skip) = GetMountFromVolume(volumeName);
                
                if (!skip && mount != null)
                {
                    mounts.Add(mount);
                }
                warnings.AddRange(mountWarnings);
                
                Array.Clear(volumeGuid);
            }
            while (WindowsNative.FindNextVolume(hFindVolume, volumeGuid, (uint)volumeGuid.Length));
        }
        finally
        {
            WindowsNative.FindVolumeClose(hFindVolume);
        }
    }
    
    private static (Mount? Mount, List<string> Warnings, bool Skip) GetMountFromVolume(string volumeGuid)
    {
        var warnings = new List<string>();
        
        // Get mount point
        var pathNames = new char[WindowsNative.MAX_PATH + 1];
        if (!WindowsNative.GetVolumePathNamesForVolumeName(volumeGuid, pathNames, (uint)pathNames.Length, out uint returnLength))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == (int)WindowsNative.ERROR_MORE_DATA)
            {
                // Retry with larger buffer
                pathNames = new char[returnLength];
                if (!WindowsNative.GetVolumePathNamesForVolumeName(volumeGuid, pathNames, (uint)pathNames.Length, out _))
                {
                    var retryError = Marshal.GetLastWin32Error();
                    warnings.Add($"{volumeGuid}: GetVolumePathNamesForVolumeName retry failed ({retryError})");
                    // Clear buffer to ensure we skip this volume
                    Array.Clear(pathNames);
                }
            }
            else
            {
                warnings.Add($"{volumeGuid}: GetVolumePathNamesForVolumeName failed ({error})");
            }
        }
        
        var mountPoint = new string(pathNames).Split('\0', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        
        // Skip unmounted volumes
        if (string.IsNullOrEmpty(mountPoint))
        {
            return (null, warnings, true);
        }
        
        return GetMountFromPath(mountPoint, volumeGuid, warnings);
    }
    
    private static (Mount? Mount, List<string> Warnings, bool Skip) GetMountFromPath(string mountPoint, string volumeGuid, List<string> warnings)
    {
        // Get volume information
        var volumeNameBuffer = new char[WindowsNative.MAX_PATH + 1];
        var fsNameBuffer = new char[WindowsNative.MAX_PATH + 1];
        string volumeLabel = string.Empty;
        string fsType = string.Empty;
        
        if (WindowsNative.GetVolumeInformation(
            mountPoint,
            volumeNameBuffer, (uint)volumeNameBuffer.Length,
            out _, out _, out _,
            fsNameBuffer, (uint)fsNameBuffer.Length))
        {
            volumeLabel = new string(volumeNameBuffer).TrimEnd('\0');
            fsType = new string(fsNameBuffer).TrimEnd('\0');
        }
        else
        {
            warnings.Add($"{mountPoint}: GetVolumeInformation failed ({Marshal.GetLastWin32Error()})");
        }
        
        // Get disk space
        ulong totalBytes = 0, freeBytes = 0;
        if (!WindowsNative.GetDiskFreeSpaceEx(mountPoint, out _, out totalBytes, out freeBytes))
        {
            warnings.Add($"{mountPoint}: GetDiskFreeSpaceEx failed ({Marshal.GetLastWin32Error()})");
        }
        
        // Get cluster info
        uint totalClusters = 0, clusterSize = 0;
        if (WindowsNative.GetDiskFreeSpace(mountPoint, out uint sectorsPerCluster, out uint bytesPerSector, out _, out totalClusters))
        {
            clusterSize = sectorsPerCluster * bytesPerSector;
        }
        
        // Determine device type
        var driveType = WindowsNative.GetDriveType(mountPoint);
        var deviceType = driveType switch
        {
            WindowsNative.DRIVE_REMOTE => DeviceTypes.Network,
            WindowsNative.DRIVE_RAMDISK => DeviceTypes.Special,
            WindowsNative.DRIVE_CDROM => DeviceTypes.Special,
            _ => DeviceTypes.Local
        };
        
        var mount = new Mount
        {
            Device = string.IsNullOrEmpty(volumeLabel) ? volumeGuid : volumeLabel,
            DeviceType = deviceType,
            Mountpoint = mountPoint,
            Fstype = fsType,
            Type = fsType,
            Opts = string.Empty,
            Total = totalBytes,
            Free = freeBytes,
            Used = totalBytes - freeBytes,
            Blocks = totalClusters,
            BlockSize = clusterSize,
            // Windows doesn't expose inode counts
            Inodes = 0,
            InodesFree = 0,
            InodesUsed = 0,
        };
        
        return (mount, warnings, false);
    }
    
    private static void GetNetworkDrives(List<Mount> mounts, List<string> warnings)
    {
        var result = WindowsNetworkNative.WNetOpenEnum(
            WindowsNetworkNative.RESOURCE_CONNECTED,
            WindowsNetworkNative.RESOURCETYPE_DISK,
            WindowsNetworkNative.RESOURCEUSAGE_CONNECTABLE,
            nint.Zero,
            out nint hEnum);
            
        if (result != WindowsNative.NO_ERROR)
        {
            return; // Network enumeration not available
        }
        
        try
        {
            const int bufferSize = 16384;
            var buffer = Marshal.AllocHGlobal(bufferSize);
            
            try
            {
                while (true)
                {
                    uint count = 0xFFFFFFFF; // Request maximum entries
                    uint size = bufferSize;
                    
                    result = WindowsNetworkNative.WNetEnumResource(hEnum, ref count, buffer, ref size);
                    
                    if (result == WindowsNative.ERROR_NO_MORE_ITEMS)
                        break;
                        
                    if (result != WindowsNative.NO_ERROR)
                    {
                        warnings.Add($"WNetEnumResource failed: {result}");
                        break;
                    }
                    
                    // Process each network resource
                    var resourceSize = Marshal.SizeOf<WindowsNetworkNative.NETRESOURCE>();
                    for (uint i = 0; i < count; i++)
                    {
                        var resourcePtr = buffer + (int)(i * resourceSize);
                        var resource = Marshal.PtrToStructure<WindowsNetworkNative.NETRESOURCE>(resourcePtr);
                        
                        var localName = resource.lpLocalName != nint.Zero 
                            ? Marshal.PtrToStringUni(resource.lpLocalName) 
                            : null;
                        var remoteName = resource.lpRemoteName != nint.Zero 
                            ? Marshal.PtrToStringUni(resource.lpRemoteName) 
                            : null;
                            
                        if (string.IsNullOrEmpty(localName))
                            continue;
                            
                        // Make sure path ends with backslash
                        var mountPoint = localName.EndsWith('\\') ? localName : localName + "\\";
                        
                        // Check if already found
                        if (mounts.Any(m => m.Mountpoint.Equals(mountPoint, StringComparison.OrdinalIgnoreCase)))
                            continue;
                        
                        var (mount, mountWarnings, skip) = GetMountFromPath(mountPoint, remoteName ?? localName, []);
                        
                        if (!skip && mount != null)
                        {
                            mount.Device = remoteName ?? localName;
                            mount.DeviceType = DeviceTypes.Network;
                            mounts.Add(mount);
                        }
                        warnings.AddRange(mountWarnings);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            WindowsNetworkNative.WNetCloseEnum(hEnum);
        }
    }
    
    private static void GetLogicalDrives(List<Mount> mounts, List<string> warnings)
    {
        var driveBits = WindowsNative.GetLogicalDrives();
        
        for (int i = 0; i < 26; i++)
        {
            if ((driveBits & (1u << i)) == 0)
                continue;
                
            var driveLetter = (char)('A' + i);
            var mountPoint = $"{driveLetter}:\\";
            
            // Skip if already found
            if (mounts.Any(m => m.Mountpoint.Equals(mountPoint, StringComparison.OrdinalIgnoreCase)))
                continue;
            
            var (mount, mountWarnings, skip) = GetMountFromPath(mountPoint, mountPoint, []);
            
            if (!skip && mount != null)
            {
                mounts.Add(mount);
            }
            warnings.AddRange(mountWarnings);
        }
    }
}

