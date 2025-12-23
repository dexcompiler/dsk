using Dsk.Models;
using Dsk.Utils;

namespace Dsk.Rendering;

/// <summary>
/// Renders mount information as Markdown table.
/// </summary>
public static class MarkdownRenderer
{
    /// <summary>
    /// Render mounts as Markdown table to stdout.
    /// </summary>
    public static void Render(List<Mount> mounts, List<ColumnId> columns)
    {
        // Header row
        var headers = columns.Select(GetColumnHeader);
        Console.WriteLine("| " + string.Join(" | ", headers) + " |");
        
        // Separator row with alignment
        var separators = columns.Select(col => GetSeparator(col));
        Console.WriteLine("| " + string.Join(" | ", separators) + " |");
        
        // Data rows
        foreach (var mount in mounts)
        {
            var values = columns.Select(col => GetCellValue(mount, col));
            Console.WriteLine("| " + string.Join(" | ", values) + " |");
        }
    }
    
    private static string GetColumnHeader(ColumnId column)
    {
        return column switch
        {
            ColumnId.Mountpoint => "Mounted on",
            ColumnId.Size => "Size",
            ColumnId.Used => "Used",
            ColumnId.Avail => "Avail",
            ColumnId.Usage => "Use%",
            ColumnId.Inodes => "Inodes",
            ColumnId.InodesUsed => "IUsed",
            ColumnId.InodesAvail => "IAvail",
            ColumnId.InodesUsage => "IUse%",
            ColumnId.Type => "Type",
            ColumnId.Filesystem => "Filesystem",
            ColumnId.Trend => "Trend",
            _ => ""
        };
    }
    
    private static string GetSeparator(ColumnId column)
    {
        // Right-align numeric columns
        return column switch
        {
            ColumnId.Size or ColumnId.Used or ColumnId.Avail or 
            ColumnId.Inodes or ColumnId.InodesUsed or ColumnId.InodesAvail => "--:",
            ColumnId.Usage or ColumnId.InodesUsage => ":-:",
            _ => "---"
        };
    }
    
    private static string GetCellValue(Mount mount, ColumnId column)
    {
        var value = column switch
        {
            ColumnId.Mountpoint => EscapePipes(mount.Mountpoint),
            ColumnId.Size => SizeFormatter.Format(mount.Total),
            ColumnId.Used => SizeFormatter.Format(mount.Used),
            ColumnId.Avail => SizeFormatter.Format(mount.Free),
            ColumnId.Usage => $"{mount.Usage * 100:F1}%",
            ColumnId.Inodes => mount.Inodes.ToString("N0"),
            ColumnId.InodesUsed => mount.InodesUsed.ToString("N0"),
            ColumnId.InodesAvail => mount.InodesFree.ToString("N0"),
            ColumnId.InodesUsage => $"{mount.InodeUsage * 100:F1}%",
            ColumnId.Type => EscapePipes(mount.Fstype),
            ColumnId.Filesystem => EscapePipes(mount.Device),
            ColumnId.Trend => "-", // Trend needs history context
            _ => ""
        };
        
        return value;
    }
    
    private static string EscapePipes(string value)
    {
        return value.Replace("|", "\\|");
    }
}

