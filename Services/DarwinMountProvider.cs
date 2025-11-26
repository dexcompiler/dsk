using System.Runtime.InteropServices;
using System.Text;
using Dsk.Models;
using Dsk.Interop.Darwin;

namespace Dsk.Services;

/// <summary>
/// macOS mount provider - uses getfsstat to enumerate filesystems.
/// </summary>
public sealed class DarwinMountProvider : IMountProvider
{
    // Network filesystem types on macOS
    private static readonly HashSet<string> NetworkFsTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "nfs", "smbfs", "afpfs", "webdav", "cifs", "ftp"
    };
    
    // Special filesystem types on macOS
    private static readonly HashSet<string> SpecialFsTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "devfs", "autofs", "nullfs", "fdesc"
    };
    
    public (List<Mount> Mounts, List<string> Warnings) GetMounts()
    {
        var mounts = new List<Mount>();
        var warnings = new List<string>();
        
        // First call to get count
        int count = DarwinNative.Getfsstat(nint.Zero, 0, DarwinNative.MNT_WAIT);
        if (count <= 0)
        {
            return (mounts, [$"getfsstat failed: {Marshal.GetLastWin32Error()}"]);
        }
        
        // Allocate buffer for all statfs structures
        var statfsArray = new Statfs[count];
        var bufferSize = count * Marshal.SizeOf<Statfs>();
        
        // Get all filesystem stats
        int result = DarwinNative.Getfsstat(statfsArray, bufferSize, DarwinNative.MNT_WAIT);
        if (result <= 0)
        {
            return (mounts, [$"getfsstat failed: {Marshal.GetLastWin32Error()}"]);
        }
        
        for (int i = 0; i < result; i++)
        {
            ref var stat = ref statfsArray[i];
            
            var device = GetString(stat.f_mntfromname);
            var mountPoint = GetString(stat.f_mntonname);
            var fsType = GetString(stat.f_fstypename);
            
            if (string.IsNullOrEmpty(device))
                continue;
            
            // Build mount options string
            var opts = BuildMountOptions(stat.f_flags);
            
            var mount = new Mount
            {
                Device = device,
                Mountpoint = mountPoint,
                Fstype = fsType,
                Type = fsType,
                Opts = opts,
                Total = stat.f_blocks * stat.f_bsize,
                Free = stat.f_bavail * stat.f_bsize,
                Used = (stat.f_blocks - stat.f_bfree) * stat.f_bsize,
                Inodes = stat.f_files,
                InodesFree = stat.f_ffree,
                InodesUsed = stat.f_files - stat.f_ffree,
                Blocks = stat.f_blocks,
                BlockSize = stat.f_bsize,
            };
            
            mount.DeviceType = DetermineDeviceType(mount);
            mounts.Add(mount);
        }
        
        return (mounts, warnings);
    }
    
    private static string GetString(byte[]? buffer)
    {
        if (buffer == null || buffer.Length == 0)
            return string.Empty;
            
        // Find null terminator
        int length = 0;
        while (length < buffer.Length && buffer[length] != 0)
        {
            length++;
        }
        
        if (length == 0)
            return string.Empty;
            
        return Encoding.UTF8.GetString(buffer, 0, length);
    }
    
    private static string BuildMountOptions(uint flags)
    {
        var opts = new List<string>();
        
        if ((flags & DarwinNative.MNT_RDONLY) != 0)
            opts.Add("ro");
        else
            opts.Add("rw");
            
        if ((flags & DarwinNative.MNT_SYNCHRONOUS) != 0)
            opts.Add("sync");
        if ((flags & DarwinNative.MNT_NOEXEC) != 0)
            opts.Add("noexec");
        if ((flags & DarwinNative.MNT_NOSUID) != 0)
            opts.Add("nosuid");
        if ((flags & DarwinNative.MNT_UNION) != 0)
            opts.Add("union");
        if ((flags & DarwinNative.MNT_ASYNC) != 0)
            opts.Add("async");
        if ((flags & DarwinNative.MNT_DONTBROWSE) != 0)
            opts.Add("nobrowse");
        if ((flags & DarwinNative.MNT_AUTOMOUNTED) != 0)
            opts.Add("automounted");
        if ((flags & DarwinNative.MNT_JOURNALED) != 0)
            opts.Add("journaled");
        if ((flags & DarwinNative.MNT_MULTILABEL) != 0)
            opts.Add("multilabel");
        if ((flags & DarwinNative.MNT_NOATIME) != 0)
            opts.Add("noatime");
        if ((flags & DarwinNative.MNT_NODEV) != 0)
            opts.Add("nodev");
            
        return string.Join(",", opts);
    }
    
    private static string DetermineDeviceType(Mount mount)
    {
        if (NetworkFsTypes.Contains(mount.Fstype))
            return DeviceTypes.Network;
            
        if (SpecialFsTypes.Contains(mount.Fstype))
            return DeviceTypes.Special;
            
        // FUSE filesystems
        if (mount.Fstype.StartsWith("fuse", StringComparison.OrdinalIgnoreCase) ||
            mount.Fstype.Contains("fuse", StringComparison.OrdinalIgnoreCase))
            return DeviceTypes.Fuse;
            
        return DeviceTypes.Local;
    }
}
