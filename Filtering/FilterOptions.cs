namespace Dsk.Filtering;

/// <summary>
/// Contains all filter options for mount display.
/// </summary>
public sealed class FilterOptions
{
    /// <summary>
    /// Include pseudo, duplicate, inaccessible file systems
    /// </summary>
    public bool IncludeAll { get; set; }
    
    /// <summary>
    /// Hidden device types (local, network, fuse, special, loops, binds)
    /// </summary>
    public HashSet<string> HiddenDevices { get; set; } = [];
    
    /// <summary>
    /// Only show these device types
    /// </summary>
    public HashSet<string> OnlyDevices { get; set; } = [];
    
    /// <summary>
    /// Hidden filesystem types
    /// </summary>
    public HashSet<string> HiddenFilesystems { get; set; } = [];
    
    /// <summary>
    /// Only show these filesystem types
    /// </summary>
    public HashSet<string> OnlyFilesystems { get; set; } = [];
    
    /// <summary>
    /// Hidden mount points (supports wildcards)
    /// </summary>
    public HashSet<string> HiddenMountPoints { get; set; } = [];
    
    /// <summary>
    /// Only show these mount points (supports wildcards)
    /// </summary>
    public HashSet<string> OnlyMountPoints { get; set; } = [];
    
    /// <summary>
    /// Parse comma-separated values into a HashSet
    /// </summary>
    public static HashSet<string> ParseCommaSeparated(string? values)
    {
        if (string.IsNullOrWhiteSpace(values))
            return [];
            
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var value in values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            result.Add(value.ToLowerInvariant());
        }
        
        return result;
    }
}

