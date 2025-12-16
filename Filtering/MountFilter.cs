using Dsk.Models;
using Dsk.Utils;

namespace Dsk.Filtering;

/// <summary>
/// Filters mounts based on filter options.
/// </summary>
public static class MountFilter
{
    /// <summary>
    /// Apply all filters to the mount list and group by device type.
    /// </summary>
    public static List<Mount> Apply(List<Mount> mounts, FilterOptions options)
    {
        var result = new List<Mount>();
        
        foreach (var mount in mounts)
        {
            if (ShouldInclude(mount, options))
            {
                result.Add(mount);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Filter mounts by specific paths (devices or mount points)
    /// </summary>
    public static List<Mount> FilterByPaths(List<Mount> mounts, string[] paths)
    {
        var result = new List<Mount>();
        var visited = new HashSet<string>();
        
        foreach (var path in paths)
        {
            var absolutePath = Path.GetFullPath(path);
            
            // Try to resolve symlinks
            try
            {
                var resolved = Path.GetFullPath(absolutePath);
                if (File.Exists(resolved) || Directory.Exists(resolved))
                {
                    absolutePath = resolved;
                }
            }
            catch
            {
                // Ignore resolution errors
            }
            
            // Find matching mounts
            var matches = FindMountsForPath(mounts, absolutePath);
            foreach (var mount in matches)
            {
                if (visited.Add(mount.Mountpoint))
                {
                    result.Add(mount);
                }
            }
        }
        
        return result;
    }
    
    private static List<Mount> FindMountsForPath(List<Mount> mounts, string path)
    {
        var result = new List<Mount>();
        
        // Check for exact device match
        foreach (var mount in mounts)
        {
            if (string.Equals(path, mount.Device, StringComparison.Ordinal))
            {
                return [mount];
            }
        }
        
        // Find closest mount point(s)
        foreach (var mount in mounts)
        {
            if (path.StartsWith(mount.Mountpoint, StringComparison.Ordinal))
            {
                // Keep only mounts that are as close or closer
                var newResult = new List<Mount>();
                foreach (var existing in result)
                {
                    if (existing.Mountpoint.Length >= mount.Mountpoint.Length)
                    {
                        newResult.Add(existing);
                    }
                }
                result = newResult;
                
                // Add if we haven't found something closer
                if (result.Count == 0 || mount.Mountpoint.Length >= result[0].Mountpoint.Length)
                {
                    result.Add(mount);
                }
            }
        }
        
        return result;
    }
    
    private static bool ShouldInclude(Mount mount, FilterOptions options)
    {
        // Check filesystem filter
        if (options.OnlyFilesystems.Count > 0)
        {
            if (!options.OnlyFilesystems.Contains(mount.Fstype.ToLowerInvariant()))
                return false;
        }
        else if (options.HiddenFilesystems.Contains(mount.Fstype.ToLowerInvariant()))
        {
            return false;
        }
        
        // Check mount point filter
        if (options.OnlyMountPoints.Count > 0)
        {
            if (!MatchesAnyPattern(mount.Mountpoint, options.OnlyMountPoints))
                return false;
        }
        else if (MatchesAnyPattern(mount.Mountpoint, options.HiddenMountPoints))
        {
            return false;
        }
        
        // Skip hidden filesystems unless --all
        if (!options.IncludeAll && IsHiddenFs(mount))
        {
            return false;
        }
        
        // Skip bind mounts
        if (mount.Opts.Contains("bind"))
        {
            bool hasBind = options.OnlyDevices.Contains(DeviceTypes.Binds);
            if (options.OnlyDevices.Count > 0 && !hasBind)
                return false;
            // Explicit device type filters always take precedence over --all
            if (options.HiddenDevices.Contains(DeviceTypes.Binds))
                return false;
        }
        
        // Skip loop devices
        if (mount.Device.StartsWith("/dev/loop", StringComparison.Ordinal))
        {
            bool hasLoops = options.OnlyDevices.Contains(DeviceTypes.Loops);
            if (options.OnlyDevices.Count > 0 && !hasLoops)
                return false;
            // Explicit device type filters always take precedence over --all
            if (options.HiddenDevices.Contains(DeviceTypes.Loops))
                return false;
        }
        
        // Skip special devices (zero blocks)
        if (mount.Blocks == 0 && !options.IncludeAll)
        {
            return false;
        }
        
        // Skip zero block size
        if (mount.BlockSize == 0 && !options.IncludeAll)
        {
            return false;
        }
        
        // Check device type filter
        if (options.OnlyDevices.Count > 0)
        {
            if (!options.OnlyDevices.Contains(mount.DeviceType))
                return false;
        }
        else if (options.HiddenDevices.Contains(mount.DeviceType))
        {
            return false;
        }
        
        return true;
    }
    
    private static bool MatchesAnyPattern(string value, HashSet<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (WildcardMatcher.Match(pattern, value))
                return true;
        }
        return false;
    }
    
    private static bool IsHiddenFs(Mount mount)
    {
        // Hidden device names
        if (mount.Device is "shm" or "overlay")
            return true;
            
        // Hidden filesystem types
        if (mount.Fstype == "autofs")
            return true;
            
        // Snap mounts
        if (mount.Fstype == "squashfs" && mount.Mountpoint.StartsWith("/snap", StringComparison.Ordinal))
            return true;
            
        return false;
    }
}

