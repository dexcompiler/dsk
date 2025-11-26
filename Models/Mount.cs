using System.Text.Json.Serialization;

namespace Dsk.Models;

/// <summary>
/// Contains all metadata for a single filesystem mount.
/// </summary>
public sealed class Mount
{
    [JsonPropertyName("device")]
    public string Device { get; set; } = string.Empty;
    
    [JsonPropertyName("device_type")]
    public string DeviceType { get; set; } = string.Empty;
    
    [JsonPropertyName("mount_point")]
    public string Mountpoint { get; set; } = string.Empty;
    
    [JsonPropertyName("fs_type")]
    public string Fstype { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("opts")]
    public string Opts { get; set; } = string.Empty;
    
    [JsonPropertyName("total")]
    public ulong Total { get; set; }
    
    [JsonPropertyName("free")]
    public ulong Free { get; set; }
    
    [JsonPropertyName("used")]
    public ulong Used { get; set; }
    
    [JsonPropertyName("inodes")]
    public ulong Inodes { get; set; }
    
    [JsonPropertyName("inodes_free")]
    public ulong InodesFree { get; set; }
    
    [JsonPropertyName("inodes_used")]
    public ulong InodesUsed { get; set; }
    
    [JsonPropertyName("blocks")]
    public ulong Blocks { get; set; }
    
    [JsonPropertyName("block_size")]
    public ulong BlockSize { get; set; }
    
    /// <summary>
    /// Platform-specific metadata (not serialized to JSON)
    /// </summary>
    [JsonIgnore]
    public object? Metadata { get; set; }
    
    /// <summary>
    /// Calculate usage percentage (0.0 to 1.0)
    /// </summary>
    public double Usage => Total > 0 ? Math.Min(1.0, (double)Used / Total) : 0.0;
    
    /// <summary>
    /// Calculate inode usage percentage (0.0 to 1.0)
    /// </summary>
    public double InodeUsage => Inodes > 0 ? Math.Min(1.0, (double)InodesUsed / Inodes) : 0.0;
}

/// <summary>
/// Device type constants
/// </summary>
public static class DeviceTypes
{
    public const string Local = "local";
    public const string Network = "network";
    public const string Fuse = "fuse";
    public const string Special = "special";
    public const string Loops = "loops";
    public const string Binds = "binds";
    
    public static readonly string[] All = [Local, Network, Fuse, Special, Loops, Binds];
}

