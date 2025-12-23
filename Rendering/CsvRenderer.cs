using Dsk.Models;
using Dsk.Utils;

namespace Dsk.Rendering;

/// <summary>
/// Renders mount information as CSV.
/// </summary>
public static class CsvRenderer
{
    /// <summary>
    /// Render mounts as CSV to stdout.
    /// </summary>
    public static void Render(List<Mount> mounts, List<ColumnId> columns)
    {
        // Header row
        var headers = columns.Select(GetColumnName);
        Console.WriteLine(string.Join(",", headers));
        
        // Data rows
        foreach (var mount in mounts)
        {
            var values = columns.Select(col => GetCellValue(mount, col));
            Console.WriteLine(string.Join(",", values));
        }
    }
    
    private static string GetColumnName(ColumnId column)
    {
        return column switch
        {
            ColumnId.Mountpoint => "mountpoint",
            ColumnId.Size => "size",
            ColumnId.Used => "used",
            ColumnId.Avail => "avail",
            ColumnId.Usage => "usage",
            ColumnId.Inodes => "inodes",
            ColumnId.InodesUsed => "inodes_used",
            ColumnId.InodesAvail => "inodes_avail",
            ColumnId.InodesUsage => "inodes_usage",
            ColumnId.Type => "type",
            ColumnId.Filesystem => "filesystem",
            ColumnId.Trend => "trend",
            _ => ""
        };
    }
    
    private static string GetCellValue(Mount mount, ColumnId column)
    {
        var value = column switch
        {
            ColumnId.Mountpoint => mount.Mountpoint,
            ColumnId.Size => mount.Total.ToString(),
            ColumnId.Used => mount.Used.ToString(),
            ColumnId.Avail => mount.Free.ToString(),
            ColumnId.Usage => (mount.Usage * 100).ToString("F1"),
            ColumnId.Inodes => mount.Inodes.ToString(),
            ColumnId.InodesUsed => mount.InodesUsed.ToString(),
            ColumnId.InodesAvail => mount.InodesFree.ToString(),
            ColumnId.InodesUsage => (mount.InodeUsage * 100).ToString("F1"),
            ColumnId.Type => mount.Fstype,
            ColumnId.Filesystem => mount.Device,
            ColumnId.Trend => "-", // Trend needs history context
            _ => ""
        };
        
        // Escape CSV values containing commas, quotes, or newlines
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            value = "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        
        return value;
    }
}

