using System.Collections.Frozen;
using System.Runtime.InteropServices;

namespace Dsk.Interop.Linux;

/// <summary>
/// Linux statfs structure and P/Invoke declarations.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct Statfs
{
    public long Type;          // f_type
    public long Bsize;         // f_bsize
    public ulong Blocks;       // f_blocks
    public ulong Bfree;        // f_bfree
    public ulong Bavail;       // f_bavail
    public ulong Files;        // f_files
    public ulong Ffree;        // f_ffree
    public long Fsid1;         // f_fsid (first part)
    public long Fsid2;         // f_fsid (second part)
    public long Namelen;       // f_namelen
    public long Frsize;        // f_frsize
    public long Flags;         // f_flags
    
    // Padding to match kernel struct size
    public long Spare0;
    public long Spare1;
    public long Spare2;
    public long Spare3;
}

internal static partial class LinuxNative
{
    [DllImport("libc", EntryPoint = "statfs", SetLastError = true)]
    internal static extern int Statfs([MarshalAs(UnmanagedType.LPStr)] string path, ref Statfs buf);
}

/// <summary>
/// Filesystem type magic numbers (from Linux kernel)
/// </summary>
internal static class FsTypeMagic
{
    public const long ADFS_SUPER_MAGIC = 0xadf5;
    public const long AFFS_SUPER_MAGIC = 0xADFF;
    public const long AFS_SUPER_MAGIC = 0x5346414F;
    public const long AUTOFS_SUPER_MAGIC = 0x0187;
    public const long BTRFS_SUPER_MAGIC = 0x9123683E;
    public const long CGROUP_SUPER_MAGIC = 0x27e0eb;
    public const long CGROUP2_SUPER_MAGIC = 0x63677270;
    public const long CIFS_MAGIC_NUMBER = 0xFF534D42;
    public const long CODA_SUPER_MAGIC = 0x73757245;
    public const long DEBUGFS_MAGIC = 0x64626720;
    public const long DEVPTS_SUPER_MAGIC = 0x1cd1;
    public const long ECRYPTFS_SUPER_MAGIC = 0xF15F;
    public const long EXT2_SUPER_MAGIC = 0xEF53;
    public const long EXT3_SUPER_MAGIC = 0xEF53;
    public const long EXT4_SUPER_MAGIC = 0xEF53;
    public const long FUSE_SUPER_MAGIC = 0x65735546;
    public const long FUSEBLK_SUPER_MAGIC = 0x65735546;
    public const long FUSECTL_SUPER_MAGIC = 0x65735543;
    public const long HFS_SUPER_MAGIC = 0x4244;
    public const long HFSPLUS_SUPER_MAGIC = 0x482b;
    public const long HUGETLBFS_MAGIC = 0x958458f6;
    public const long ISOFS_SUPER_MAGIC = 0x9660;
    public const long JFFS2_SUPER_MAGIC = 0x72b6;
    public const long MQUEUE_MAGIC = 0x19800202;
    public const long MSDOS_SUPER_MAGIC = 0x4d44;
    public const long NCP_SUPER_MAGIC = 0x564c;
    public const long NFS_SUPER_MAGIC = 0x6969;
    public const long NTFS_SB_MAGIC = 0x5346544e;
    public const long PROC_SUPER_MAGIC = 0x9fa0;
    public const long PSTOREFS_MAGIC = 0x6165676C;
    public const long RAMFS_MAGIC = 0x858458f6;
    public const long REISERFS_SUPER_MAGIC = 0x52654973;
    public const long SECURITYFS_SUPER_MAGIC = 0x73636673;
    public const long SMB_SUPER_MAGIC = 0x517B;
    public const long SMB2_MAGIC_NUMBER = 0xfe534d42;
    public const long SQUASHFS_MAGIC = 0x73717368;
    public const long SYSFS_MAGIC = 0x62656572;
    public const long TMPFS_MAGIC = 0x01021994;
    public const long TRACEFS_MAGIC = 0x74726163;
    public const long XFS_SUPER_MAGIC = 0x58465342;
    public const long ZFS_SUPER_MAGIC = 0x2FC12FC1;
    public const long BPF_FS_MAGIC = 0xcafe4a11;
    public const long CONFIGFS_MAGIC = 0x62656570;
    public const long EFIVARFS_MAGIC = 0xde5e81e4;
    
    // FrozenDictionary/FrozenSet provide faster lookups for read-only static data
    private static readonly FrozenDictionary<long, string> TypeMap = new Dictionary<long, string>
    {
        [ADFS_SUPER_MAGIC] = "adfs",
        [AFFS_SUPER_MAGIC] = "affs",
        [AFS_SUPER_MAGIC] = "afs",
        [AUTOFS_SUPER_MAGIC] = "autofs",
        [BTRFS_SUPER_MAGIC] = "btrfs",
        [CGROUP_SUPER_MAGIC] = "cgroupfs",
        [CGROUP2_SUPER_MAGIC] = "cgroup2",
        [CIFS_MAGIC_NUMBER] = "cifs",
        [CODA_SUPER_MAGIC] = "coda",
        [DEBUGFS_MAGIC] = "debugfs",
        [DEVPTS_SUPER_MAGIC] = "devpts",
        [ECRYPTFS_SUPER_MAGIC] = "ecryptfs",
        [EXT2_SUPER_MAGIC] = "ext2/ext3",
        [FUSE_SUPER_MAGIC] = "fuse",
        [FUSECTL_SUPER_MAGIC] = "fusectl",
        [HFS_SUPER_MAGIC] = "hfs",
        [HFSPLUS_SUPER_MAGIC] = "hfsplus",
        [HUGETLBFS_MAGIC] = "hugetlbfs",
        [ISOFS_SUPER_MAGIC] = "isofs",
        [JFFS2_SUPER_MAGIC] = "jffs2",
        [MQUEUE_MAGIC] = "mqueue",
        [MSDOS_SUPER_MAGIC] = "msdos",
        [NCP_SUPER_MAGIC] = "novell",
        [NFS_SUPER_MAGIC] = "nfs",
        [NTFS_SB_MAGIC] = "ntfs",
        [PROC_SUPER_MAGIC] = "proc",
        [PSTOREFS_MAGIC] = "pstorefs",
        [RAMFS_MAGIC] = "ramfs",
        [REISERFS_SUPER_MAGIC] = "reiserfs",
        [SECURITYFS_SUPER_MAGIC] = "securityfs",
        [SMB_SUPER_MAGIC] = "smb",
        [SMB2_MAGIC_NUMBER] = "smb2",
        [SQUASHFS_MAGIC] = "squashfs",
        [SYSFS_MAGIC] = "sysfs",
        [TMPFS_MAGIC] = "tmpfs",
        [TRACEFS_MAGIC] = "tracefs",
        [XFS_SUPER_MAGIC] = "xfs",
        [ZFS_SUPER_MAGIC] = "zfs",
        [BPF_FS_MAGIC] = "bpf",
        [CONFIGFS_MAGIC] = "configfs",
        [EFIVARFS_MAGIC] = "efivarfs",
    }.ToFrozenDictionary();
    
    private static readonly FrozenSet<long> NetworkTypes = new HashSet<long>
    {
        CIFS_MAGIC_NUMBER,
        NFS_SUPER_MAGIC,
        SMB_SUPER_MAGIC,
        SMB2_MAGIC_NUMBER,
    }.ToFrozenSet();
    
    private static readonly FrozenSet<long> SpecialTypes = new HashSet<long>
    {
        AUTOFS_SUPER_MAGIC,
        BPF_FS_MAGIC,
        CGROUP_SUPER_MAGIC,
        CGROUP2_SUPER_MAGIC,
        CONFIGFS_MAGIC,
        DEBUGFS_MAGIC,
        DEVPTS_SUPER_MAGIC,
        EFIVARFS_MAGIC,
        FUSECTL_SUPER_MAGIC,
        HUGETLBFS_MAGIC,
        MQUEUE_MAGIC,
        PROC_SUPER_MAGIC,
        PSTOREFS_MAGIC,
        SECURITYFS_SUPER_MAGIC,
        SYSFS_MAGIC,
        TMPFS_MAGIC,
        TRACEFS_MAGIC,
    }.ToFrozenSet();
    
    public static string GetTypeName(long magic) =>
        TypeMap.TryGetValue(magic, out var name) ? name : string.Empty;
        
    public static bool IsNetwork(long magic) => NetworkTypes.Contains(magic);
    
    public static bool IsSpecial(long magic) => SpecialTypes.Contains(magic);
    
    public static bool IsFuse(long magic) => magic is FUSE_SUPER_MAGIC or FUSEBLK_SUPER_MAGIC;
}
