using System.Runtime.InteropServices;

namespace Dsk.Interop.Darwin;

/// <summary>
/// macOS statfs structure - using separate byte arrays instead of fixed buffers for marshalling.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct Statfs
{
    public uint f_bsize;           // fundamental file system block size
    public int f_iosize;           // optimal transfer block size
    public ulong f_blocks;         // total data blocks in file system
    public ulong f_bfree;          // free blocks in fs
    public ulong f_bavail;         // free blocks avail to non-superuser
    public ulong f_files;          // total file nodes in file system
    public ulong f_ffree;          // free file nodes in fs
    public long f_fsid1;           // file system id (fsid_t part 1)
    public long f_fsid2;           // file system id (fsid_t part 2)  
    public uint f_owner;           // user that mounted the filesystem
    public uint f_type;            // type of filesystem
    public uint f_flags;           // copy of mount exported flags
    public uint f_fssubtype;       // fs sub-type (flavor)
    
    // f_fstypename[MFSTYPENAMELEN=16]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] f_fstypename;
    
    // f_mntonname[MAXPATHLEN=1024]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
    public byte[] f_mntonname;
    
    // f_mntfromname[MAXPATHLEN=1024]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
    public byte[] f_mntfromname;
    
    // reserved
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public byte[] f_reserved;
}

internal static class DarwinNative
{
    public const int MNT_WAIT = 1;
    public const int MNT_NOWAIT = 2;
    
    // Mount flags
    public const uint MNT_RDONLY = 0x00000001;
    public const uint MNT_SYNCHRONOUS = 0x00000002;
    public const uint MNT_NOEXEC = 0x00000004;
    public const uint MNT_NOSUID = 0x00000008;
    public const uint MNT_UNION = 0x00000020;
    public const uint MNT_ASYNC = 0x00000040;
    public const uint MNT_DONTBROWSE = 0x00100000;
    public const uint MNT_AUTOMOUNTED = 0x00400000;
    public const uint MNT_JOURNALED = 0x00800000;
    public const uint MNT_MULTILABEL = 0x04000000;
    public const uint MNT_NOATIME = 0x10000000;
    public const uint MNT_NODEV = 0x00000010;
    
    [DllImport("libSystem.B.dylib", EntryPoint = "getfsstat", SetLastError = true)]
    internal static extern int Getfsstat(nint buf, int bufsize, int mode);
    
    [DllImport("libSystem.B.dylib", EntryPoint = "getfsstat", SetLastError = true)]
    internal static extern int Getfsstat([In, Out] Statfs[] buf, int bufsize, int mode);
}
