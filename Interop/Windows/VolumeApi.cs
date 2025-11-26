using System.Runtime.InteropServices;

namespace Dsk.Interop.Windows;

/// <summary>
/// Windows Volume API P/Invoke declarations.
/// </summary>
internal static partial class WindowsNative
{
    public const int MAX_PATH = 260;
    public const uint ERROR_NO_MORE_FILES = 18;
    public const uint ERROR_MORE_DATA = 234;
    public const uint ERROR_NO_MORE_ITEMS = 259;
    public const uint NO_ERROR = 0;
    
    // Volume enumeration
    [LibraryImport("kernel32.dll", EntryPoint = "FindFirstVolumeW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint FindFirstVolume(
        [Out] char[] lpszVolumeName,
        uint cchBufferLength);
    
    [LibraryImport("kernel32.dll", EntryPoint = "FindNextVolumeW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool FindNextVolume(
        nint hFindVolume,
        [Out] char[] lpszVolumeName,
        uint cchBufferLength);
    
    [LibraryImport("kernel32.dll", EntryPoint = "FindVolumeClose", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool FindVolumeClose(nint hFindVolume);
    
    // Volume path names
    [LibraryImport("kernel32.dll", EntryPoint = "GetVolumePathNamesForVolumeNameW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetVolumePathNamesForVolumeName(
        string lpszVolumeName,
        [Out] char[] lpszVolumePathNames,
        uint cchBufferLength,
        out uint lpcchReturnLength);
    
    // Volume information
    [LibraryImport("kernel32.dll", EntryPoint = "GetVolumeInformationW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetVolumeInformation(
        string lpRootPathName,
        [Out] char[]? lpVolumeNameBuffer,
        uint nVolumeNameSize,
        out uint lpVolumeSerialNumber,
        out uint lpMaximumComponentLength,
        out uint lpFileSystemFlags,
        [Out] char[]? lpFileSystemNameBuffer,
        uint nFileSystemNameSize);
    
    // Disk space
    [LibraryImport("kernel32.dll", EntryPoint = "GetDiskFreeSpaceExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetDiskFreeSpaceEx(
        string lpDirectoryName,
        out ulong lpFreeBytesAvailableToCaller,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);
    
    [LibraryImport("kernel32.dll", EntryPoint = "GetDiskFreeSpaceW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetDiskFreeSpace(
        string lpRootPathName,
        out uint lpSectorsPerCluster,
        out uint lpBytesPerSector,
        out uint lpNumberOfFreeClusters,
        out uint lpTotalNumberOfClusters);
    
    // Logical drives
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial uint GetLogicalDrives();
    
    // Drive type
    [LibraryImport("kernel32.dll", EntryPoint = "GetDriveTypeW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint GetDriveType(string lpRootPathName);
    
    public const uint DRIVE_UNKNOWN = 0;
    public const uint DRIVE_NO_ROOT_DIR = 1;
    public const uint DRIVE_REMOVABLE = 2;
    public const uint DRIVE_FIXED = 3;
    public const uint DRIVE_REMOTE = 4;
    public const uint DRIVE_CDROM = 5;
    public const uint DRIVE_RAMDISK = 6;
}

/// <summary>
/// Windows Network API P/Invoke declarations.
/// </summary>
internal static partial class WindowsNetworkNative
{
    public const uint RESOURCE_CONNECTED = 0x00000001;
    public const uint RESOURCETYPE_DISK = 0x00000001;
    public const uint RESOURCEUSAGE_CONNECTABLE = 0x00000001;
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct NETRESOURCE
    {
        public uint dwScope;
        public uint dwType;
        public uint dwDisplayType;
        public uint dwUsage;
        public nint lpLocalName;
        public nint lpRemoteName;
        public nint lpComment;
        public nint lpProvider;
    }
    
    [LibraryImport("mpr.dll", EntryPoint = "WNetOpenEnumW", SetLastError = true)]
    internal static partial uint WNetOpenEnum(
        uint dwScope,
        uint dwType,
        uint dwUsage,
        nint lpNetResource,
        out nint lphEnum);
    
    [LibraryImport("mpr.dll", EntryPoint = "WNetEnumResourceW", SetLastError = true)]
    internal static partial uint WNetEnumResource(
        nint hEnum,
        ref uint lpcCount,
        nint lpBuffer,
        ref uint lpBufferSize);
    
    [LibraryImport("mpr.dll", EntryPoint = "WNetCloseEnum", SetLastError = true)]
    internal static partial uint WNetCloseEnum(nint hEnum);
}

