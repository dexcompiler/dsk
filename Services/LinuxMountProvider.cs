using System.Text;
using System.Text.RegularExpressions;
using Dsk.Models;
using Dsk.Interop.Linux;

namespace Dsk.Services;

/// <summary>
/// Linux mount provider - parses /proc/self/mountinfo and uses statfs for disk stats.
/// </summary>
public sealed partial class LinuxMountProvider : IMountProvider
{
    private const string MountInfoPath = "/proc/self/mountinfo";
    
    // Mountinfo field indices (after parsing)
    private const int MountInfoMountPoint = 4;
    private const int MountInfoMountOpts = 5;
    private const int MountInfoOptionalFields = 6;
    private const int MountInfoFsType = 8;
    private const int MountInfoMountSource = 9;
    
    public (List<Mount> Mounts, List<string> Warnings) GetMounts()
    {
        var mounts = new List<Mount>();
        var warnings = new List<string>();
        
        string[] lines;
        try
        {
            lines = File.ReadAllLines(MountInfoPath);
        }
        catch (Exception ex)
        {
            return (mounts, [$"Error reading {MountInfoPath}: {ex.Message}"]);
        }
        
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;
                
            var (fieldCount, fields) = ParseMountInfoLine(line);
            
            if (fieldCount < 10 || fieldCount > 11)
            {
                warnings.Add($"Invalid mountinfo line: {line}");
                continue;
            }
            
            var mountPoint = UnescapeFstab(fields[MountInfoMountPoint]);
            var mountOpts = fields[MountInfoMountOpts];
            var fsType = UnescapeFstab(fields[MountInfoFsType]);
            var device = UnescapeFstab(fields[MountInfoMountSource]);
            
            // Get filesystem stats
            var stat = new Statfs();
            int statResult = -1;
            
            try
            {
                statResult = LinuxNative.Statfs(mountPoint, ref stat);
            }
            catch
            {
                // Ignore statfs errors for inaccessible mounts
            }
            
            if (statResult != 0)
            {
                warnings.Add($"{mountPoint}: Unable to get filesystem stats");
                stat = new Statfs();
            }
            
            var mount = new Mount
            {
                Device = device,
                Mountpoint = mountPoint,
                Fstype = fsType,
                Type = FsTypeMagic.GetTypeName(stat.Type),
                Opts = mountOpts,
                Metadata = stat,
                Total = stat.Blocks * (ulong)stat.Bsize,
                Free = stat.Bavail * (ulong)stat.Bsize,
                Used = (stat.Blocks - stat.Bfree) * (ulong)stat.Bsize,
                Inodes = stat.Files,
                InodesFree = stat.Ffree,
                InodesUsed = stat.Files - stat.Ffree,
                Blocks = stat.Blocks,
                BlockSize = (ulong)stat.Bsize,
            };
            
            mount.DeviceType = DetermineDeviceType(mount, stat.Type);
            
            // Resolve /dev/mapper/* device names
            if (device.StartsWith("/dev/mapper/", StringComparison.Ordinal))
            {
                mount.Device = ResolveMapperDevice(device);
            }
            
            mounts.Add(mount);
        }
        
        return (mounts, warnings);
    }
    
    private static string DetermineDeviceType(Mount mount, long fsType)
    {
        if (FsTypeMagic.IsNetwork(fsType))
            return DeviceTypes.Network;
        if (FsTypeMagic.IsSpecial(fsType))
            return DeviceTypes.Special;
        if (FsTypeMagic.IsFuse(fsType))
            return DeviceTypes.Fuse;
            
        return DeviceTypes.Local;
    }
    
    private static string ResolveMapperDevice(string device)
    {
        // Try to resolve /dev/mapper/vg-lv to /dev/vg/lv
        var match = MapperRegex().Match(device);
        if (match is { Success: true, Groups.Count: 3 })
        {
            return Path.Combine("/dev", match.Groups[1].Value, match.Groups[2].Value);
        }
        return device;
    }
    
    [GeneratedRegex(@"^/dev/mapper/(.*)-(.*)$")]
    private static partial Regex MapperRegex();
    
    private static (int FieldCount, string[] Fields) ParseMountInfoLine(string line)
    {
        var fields = new string[11];
        var allFields = SplitMountInfoFields(line);
        
        int i = 0;
        bool sawSep = false;
        
        foreach (var field in allFields)
        {
            if (i >= fields.Length)
                break;
                
            if (i == MountInfoOptionalFields)
            {
                // Optional fields continue until we see "-"
                if (field != "-")
                {
                    fields[i] = string.IsNullOrEmpty(fields[i]) 
                        ? field 
                        : fields[i] + " " + field;
                    continue;
                }
                // Found separator
                sawSep = true;
                i++;
                fields[i] = field;
                i++;
                continue;
            }
            
            fields[i] = field;
            i++;
        }
        
        if (!sawSep && allFields.Count > MountInfoOptionalFields)
        {
            i = MountInfoOptionalFields;
        }
        
        return (i, fields);
    }
    
    private static List<string> SplitMountInfoFields(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            // Space or tab separates fields
            if (c is ' ' or '\t')
            {
                if (current.Length > 0)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }
            
            // Handle octal escape sequences
            if (c == '\\' && i + 3 < line.Length)
            {
                var oct = line.Substring(i + 1, 3);
                if (int.TryParse(oct, System.Globalization.NumberStyles.None, null, out int val) &&
                    oct.All(ch => ch >= '0' && ch <= '7'))
                {
                    // This is an octal escape
                    char decoded = (char)Convert.ToInt32(oct, 8);
                    if (decoded is ' ' or '\t' or '\n')
                    {
                        current.Append(decoded);
                        i += 3;
                        continue;
                    }
                    // Keep unknown escapes as-is
                    current.Append('\\');
                    current.Append(oct);
                    i += 3;
                    continue;
                }
            }
            
            current.Append(c);
        }
        
        if (current.Length > 0)
        {
            fields.Add(current.ToString());
        }
        
        return fields;
    }
    
    private static string UnescapeFstab(string path)
    {
        if (string.IsNullOrEmpty(path) || !path.Contains('\\'))
            return path;
            
        var result = new StringBuilder(path.Length);
        
        for (int i = 0; i < path.Length; i++)
        {
            if (path[i] == '\\' && i + 3 < path.Length)
            {
                var oct = path.Substring(i + 1, 3);
                if (oct.All(c => c >= '0' && c <= '7'))
                {
                    try
                    {
                        char decoded = (char)Convert.ToInt32(oct, 8);
                        result.Append(decoded);
                        i += 3;
                        continue;
                    }
                    catch
                    {
                        // Fall through to append backslash
                    }
                }
            }
            result.Append(path[i]);
        }
        
        return result.ToString();
    }
}

