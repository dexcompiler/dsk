using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dsk.Services;

/// <summary>
/// Source-generated JSON serializer context for AOT compatibility.
/// </summary>
[JsonSerializable(typeof(HistoryData))]
[JsonSerializable(typeof(UsageSnapshot))]
[JsonSerializable(typeof(Dictionary<string, List<UsageSnapshot>>))]
[JsonSourceGenerationOptions(WriteIndented = false)]
internal partial class HistoryJsonContext : JsonSerializerContext { }

/// <summary>
/// A single usage snapshot for a mount point.
/// </summary>
public sealed class UsageSnapshot
{
    [JsonPropertyName("t")]
    public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("u")]
    public double Usage { get; set; }
}

/// <summary>
/// History data for all tracked mount points.
/// </summary>
public sealed class HistoryData
{
    [JsonPropertyName("mounts")]
    public Dictionary<string, List<UsageSnapshot>> Mounts { get; set; } = [];
}

/// <summary>
/// Manages historical usage data for sparkline trends.
/// </summary>
public static class HistoryService
{
    private const int MaxDataPoints = 30;
    private const int MaxAgeDays = 90;
    
    private static readonly string HistoryPath = GetHistoryPath();
    
    private static string GetHistoryPath()
    {
        string baseDir;
        
        if (OperatingSystem.IsWindows())
        {
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        else
        {
            // XDG_DATA_HOME or ~/.local/share
            baseDir = Environment.GetEnvironmentVariable("XDG_DATA_HOME") 
                      ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        }
        
        return Path.Combine(baseDir, "dsk", "history.json");
    }
    
    /// <summary>
    /// Load history data from disk.
    /// </summary>
    public static HistoryData Load()
    {
        try
        {
            if (!File.Exists(HistoryPath))
                return new HistoryData();
                
            var json = File.ReadAllText(HistoryPath);
            return JsonSerializer.Deserialize(json, HistoryJsonContext.Default.HistoryData) ?? new HistoryData();
        }
        catch
        {
            return new HistoryData();
        }
    }
    
    /// <summary>
    /// Save current usage data to history.
    /// </summary>
    public static void Save(IEnumerable<(string Mountpoint, double Usage)> currentUsage)
    {
        var history = Load();
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-MaxAgeDays);
        
        foreach (var (mountpoint, usage) in currentUsage)
        {
            if (!history.Mounts.TryGetValue(mountpoint, out var snapshots))
            {
                snapshots = [];
                history.Mounts[mountpoint] = snapshots;
            }
            
            // Add new snapshot
            snapshots.Add(new UsageSnapshot { Timestamp = now, Usage = usage });
            
            // Remove old entries and trim to max size
            history.Mounts[mountpoint] = snapshots
                .Where(s => s.Timestamp > cutoff)
                .OrderByDescending(s => s.Timestamp)
                .Take(MaxDataPoints)
                .OrderBy(s => s.Timestamp)
                .ToList();
        }
        
        // Clean up mounts that no longer exist (keep for 30 days)
        var mountsToRemove = history.Mounts
            .Where(kv => kv.Value.Count == 0 || kv.Value.Max(s => s.Timestamp) < cutoff)
            .Select(kv => kv.Key)
            .ToList();
            
        foreach (var mount in mountsToRemove)
        {
            history.Mounts.Remove(mount);
        }
        
        // Save to disk
        try
        {
            var dir = Path.GetDirectoryName(HistoryPath)!;
            Directory.CreateDirectory(dir);
            
            var json = JsonSerializer.Serialize(history, HistoryJsonContext.Default.HistoryData);
            File.WriteAllText(HistoryPath, json);
        }
        catch
        {
            // Silently fail - history is non-critical
        }
    }
    
    /// <summary>
    /// Get usage history for a specific mount point.
    /// </summary>
    public static List<double> GetHistory(HistoryData data, string mountpoint, int maxPoints = 8)
    {
        if (!data.Mounts.TryGetValue(mountpoint, out var snapshots))
            return [];
            
        return snapshots
            .OrderBy(s => s.Timestamp)
            .TakeLast(maxPoints)
            .Select(s => s.Usage)
            .ToList();
    }
}

