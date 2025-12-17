using Spectre.Console;
using Spectre.Console.Rendering;
using Dsk.Models;
using Dsk.Services;
using Dsk.Utils;

namespace Dsk.Rendering;

/// <summary>
/// Column identifiers.
/// </summary>
public enum ColumnId
{
    Mountpoint = 1,
    Size = 2,
    Used = 3,
    Avail = 4,
    Usage = 5,
    Inodes = 6,
    InodesUsed = 7,
    InodesAvail = 8,
    InodesUsage = 9,
    Type = 10,
    Filesystem = 11,
    Trend = 12
}

/// <summary>
/// Sort column identifiers.
/// </summary>
public enum SortColumn
{
    Mountpoint,
    Size,
    Used,
    Avail,
    Usage,
    Inodes,
    InodesUsed,
    InodesAvail,
    InodesUsage,
    Type,
    Filesystem,
    Trend
}

/// <summary>
/// Table rendering options.
/// </summary>
public sealed class TableOptions
{
    public required List<ColumnId> Columns { get; init; }
    public required SortColumn SortBy { get; init; }
    public required string Style { get; init; }
    public required int Width { get; init; }
    public required Theme Theme { get; init; }
    public required Thresholds AvailThresholds { get; init; }
    public required UsageThresholds UsageThresholds { get; init; }
    public HistoryData? History { get; init; }
}

/// <summary>
/// Renders mount information as formatted tables using Spectre.Console.
/// </summary>
public static class TableRenderer
{
    private static readonly Dictionary<string, ColumnId> ColumnMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mountpoint"] = ColumnId.Mountpoint,
        ["size"] = ColumnId.Size,
        ["used"] = ColumnId.Used,
        ["avail"] = ColumnId.Avail,
        ["usage"] = ColumnId.Usage,
        ["inodes"] = ColumnId.Inodes,
        ["inodes_used"] = ColumnId.InodesUsed,
        ["inodes_avail"] = ColumnId.InodesAvail,
        ["inodes_usage"] = ColumnId.InodesUsage,
        ["type"] = ColumnId.Type,
        ["filesystem"] = ColumnId.Filesystem,
        ["trend"] = ColumnId.Trend,
    };
    
    private static readonly Dictionary<string, SortColumn> SortMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mountpoint"] = SortColumn.Mountpoint,
        ["size"] = SortColumn.Size,
        ["used"] = SortColumn.Used,
        ["avail"] = SortColumn.Avail,
        ["usage"] = SortColumn.Usage,
        ["inodes"] = SortColumn.Inodes,
        ["inodes_used"] = SortColumn.InodesUsed,
        ["inodes_avail"] = SortColumn.InodesAvail,
        ["inodes_usage"] = SortColumn.InodesUsage,
        ["type"] = SortColumn.Type,
        ["filesystem"] = SortColumn.Filesystem,
        ["trend"] = SortColumn.Trend,
    };
    
    /// <summary>
    /// Parse column specification string into column IDs.
    /// </summary>
    public static List<ColumnId> ParseColumns(string? columnSpec, bool inodes)
    {
        if (!string.IsNullOrWhiteSpace(columnSpec))
        {
            var columns = new List<ColumnId>();
            foreach (var col in columnSpec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (ColumnMap.TryGetValue(col, out var columnId))
                {
                    columns.Add(columnId);
                }
            }
            if (columns.Count > 0)
                return columns;
        }
        
        // Default columns
        if (inodes)
        {
            return [ColumnId.Mountpoint, ColumnId.Inodes, ColumnId.InodesUsed, 
                    ColumnId.InodesAvail, ColumnId.InodesUsage, ColumnId.Type, ColumnId.Filesystem];
        }
        
        return [ColumnId.Mountpoint, ColumnId.Size, ColumnId.Used, 
                ColumnId.Avail, ColumnId.Usage, ColumnId.Type, ColumnId.Filesystem];
    }
    
    /// <summary>
    /// Parse sort column specification.
    /// </summary>
    public static SortColumn ParseSortColumn(string sortSpec)
    {
        if (SortMap.TryGetValue(sortSpec.Trim(), out var column))
            return column;
        return SortColumn.Mountpoint;
    }
    
    /// <summary>
    /// Render mounts grouped by device type.
    /// </summary>
    public static void Render(List<Mount> mounts, TableOptions options)
    {
        // Group mounts by device type
        var groups = mounts
            .GroupBy(m => m.DeviceType)
            .OrderBy(g => GetDeviceTypeOrder(g.Key));
        
        foreach (var group in groups)
        {
            var deviceMounts = SortMounts([.. group], options);
            if (deviceMounts.Count == 0)
                continue;
                
            RenderTable(group.Key, deviceMounts, options);
        }
    }
    
    private static int GetDeviceTypeOrder(string deviceType)
    {
        return deviceType switch
        {
            DeviceTypes.Local => 0,
            DeviceTypes.Network => 1,
            DeviceTypes.Fuse => 2,
            DeviceTypes.Special => 3,
            DeviceTypes.Loops => 4,
            DeviceTypes.Binds => 5,
            _ => 99
        };
    }
    
    private static List<Mount> SortMounts(List<Mount> mounts, TableOptions options)
    {
        return options.SortBy switch
        {
            SortColumn.Size => [.. mounts.OrderBy(m => m.Total)],
            SortColumn.Used => [.. mounts.OrderBy(m => m.Used)],
            SortColumn.Avail => [.. mounts.OrderBy(m => m.Free)],
            SortColumn.Usage => [.. mounts.OrderBy(m => m.Usage)],
            SortColumn.Inodes => [.. mounts.OrderBy(m => m.Inodes)],
            SortColumn.InodesUsed => [.. mounts.OrderBy(m => m.InodesUsed)],
            SortColumn.InodesAvail => [.. mounts.OrderBy(m => m.InodesFree)],
            SortColumn.InodesUsage => [.. mounts.OrderBy(m => m.InodeUsage)],
            SortColumn.Type => [.. mounts.OrderBy(m => m.Fstype)],
            SortColumn.Filesystem => [.. mounts.OrderBy(m => m.Device)],
            SortColumn.Trend => SortByTrend(mounts, options),
            _ => [.. mounts.OrderBy(m => m.Mountpoint)],
        };
    }
    
    private static List<Mount> SortByTrend(List<Mount> mounts, TableOptions options)
    {
        if (options.History == null)
            return [.. mounts.OrderBy(m => m.Mountpoint)];
        
        // Sort by trend direction: Up (filling) first, then Stable, then Down (freeing)
        return [.. mounts.OrderByDescending(m =>
        {
            var history = HistoryService.GetHistory(options.History, m.Mountpoint);
            var trend = SparklineRenderer.GetTrend(history);
            return trend switch
            {
                TrendDirection.Up => 2,     // Filling up = highest priority
                TrendDirection.Stable => 1,
                TrendDirection.Down => 0,   // Freeing = lowest priority
                _ => 1
            };
        }).ThenByDescending(m => m.Usage)]; // Secondary sort by current usage
    }
    
    private static void RenderTable(string deviceType, List<Mount> mounts, TableOptions options)
    {
        var table = new Table();
        
        // Set table style
        if (options.Style == "ascii")
        {
            table.Border(TableBorder.Ascii);
        }
        else
        {
            table.Border(TableBorder.Rounded);
        }
        
        // Set width
        table.Width(options.Width);
        
        // Add title
        var suffix = mounts.Count == 1 ? "device" : "devices";
        table.Title($"{mounts.Count} {deviceType} {suffix}");
        
        // Add columns
        foreach (var col in options.Columns)
        {
            var header = GetColumnHeader(col);
            var column = new TableColumn(header);
            
            // Right-align numeric columns
            if (col is ColumnId.Size or ColumnId.Used or ColumnId.Avail or 
                ColumnId.Inodes or ColumnId.InodesUsed or ColumnId.InodesAvail)
            {
                column.RightAligned();
            }
            // Usage bars with percentages look better left-aligned
            
            table.AddColumn(column);
        }
        
        // Add rows
        foreach (var mount in mounts)
        {
            var cells = new List<IRenderable>();
            
            foreach (var col in options.Columns)
            {
                cells.Add(GetCellValue(mount, col, options));
            }
            
            table.AddRow(cells);
        }
        
        AnsiConsole.Write(table);
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
    
    private static IRenderable GetCellValue(Mount mount, ColumnId column, TableOptions options)
    {
        var theme = options.Theme;
        
        return column switch
        {
            ColumnId.Mountpoint => new Text(mount.Mountpoint, new Style(foreground: theme.ColorBlue)),
            ColumnId.Size => new Text(SizeFormatter.Format(mount.Total)),
            ColumnId.Used => new Text(SizeFormatter.Format(mount.Used)),
            ColumnId.Avail => GetAvailCell(mount.Free, options),
            ColumnId.Usage => GetUsageBar(mount.Usage, options),
            ColumnId.Inodes => new Text(mount.Inodes.ToString("N0")),
            ColumnId.InodesUsed => new Text(mount.InodesUsed.ToString("N0")),
            ColumnId.InodesAvail => new Text(mount.InodesFree.ToString("N0")),
            ColumnId.InodesUsage => GetUsageBar(mount.InodeUsage, options),
            ColumnId.Type => new Text(mount.Fstype, new Style(foreground: theme.ColorGray)),
            ColumnId.Filesystem => new Text(mount.Device, new Style(foreground: theme.ColorGray)),
            ColumnId.Trend => GetTrendCell(mount, options),
            _ => Text.Empty
        };
    }
    
    private static IRenderable GetTrendCell(Mount mount, TableOptions options)
    {
        var theme = options.Theme;
        var useAscii = options.Style == "ascii";
        
        if (options.History == null)
            return new Text(new string(useAscii ? '-' : '·', 8), new Style(foreground: theme.ColorGray));
        
        var history = HistoryService.GetHistory(options.History, mount.Mountpoint);
        if (history.Count == 0)
            return new Text(new string(useAscii ? '-' : '·', 8), new Style(foreground: theme.ColorGray));
        
        var sparkline = SparklineRenderer.Render(history, 8, useAscii);
        var trend = SparklineRenderer.GetTrend(history);
        
        var color = trend switch
        {
            TrendDirection.Up => theme.ColorYellow,   // Filling up = warning
            TrendDirection.Down => theme.ColorGreen,  // Freeing up = good
            _ => theme.ColorGray
        };
        
        return new Text(sparkline, new Style(foreground: color));
    }
    
    private static IRenderable GetAvailCell(ulong free, TableOptions options)
    {
        var theme = options.Theme;
        var thresholds = options.AvailThresholds;
        
        Color color;
        if (free < thresholds.Red)
            color = theme.ColorRed;
        else if (free < thresholds.Yellow)
            color = theme.ColorYellow;
        else
            color = theme.ColorGreen;
            
        return new Text(SizeFormatter.Format(free), new Style(foreground: color));
    }
    
    private static IRenderable GetUsageBar(double usage, TableOptions options)
    {
        const int barWidth = 8;
        
        var theme = options.Theme;
        var thresholds = options.UsageThresholds;
        
        // Get color based on usage
        Color fgColor;
        if (usage >= thresholds.Red)
            fgColor = theme.ColorRed;
        else if (usage >= thresholds.Yellow)
            fgColor = theme.ColorYellow;
        else
            fgColor = theme.ColorGreen;
        
        // Calculate filled/empty segments
        var filledCount = (int)Math.Round(usage * barWidth);
        filledCount = Math.Clamp(filledCount, 0, barWidth);
        var emptyCount = barWidth - filledCount;
        
        // Choose characters based on style
        char filledChar, emptyChar;
        if (options.Style == "ascii")
        {
            filledChar = '#';
            emptyChar = '-';
        }
        else
        {
            filledChar = '█';
            emptyChar = '░';
        }
        
        // Build the bar
        var filled = new string(filledChar, filledCount);
        var empty = new string(emptyChar, emptyCount);
        var percent = (usage * 100).ToString("F1");
        
        // Create composite renderable (escape [ and ] as [[ and ]] for Spectre markup)
        var color = ToSpectreColor(fgColor);
        return new Markup($"[[[{color}]{filled}[/][grey]{empty}[/]]] [{color}]{percent}%[/]");
    }
    
    private static string ToSpectreColor(Color color)
    {
        // Convert Spectre.Console Color to markup color string
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}

