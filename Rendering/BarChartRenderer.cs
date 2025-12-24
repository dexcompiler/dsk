using Spectre.Console;
using Dsk.Models;
using Dsk.Utils;

namespace Dsk.Rendering;

/// <summary>
/// Options for bar chart rendering.
/// </summary>
public sealed class BarChartOptions
{
    public Theme Theme { get; init; } = new();
    public UsageThresholds UsageThresholds { get; init; } = new(0.5, 0.9);
    public int BarWidth { get; init; } = 30;
}

/// <summary>
/// Renders mount information as horizontal stacked bar charts.
/// </summary>
public static class BarChartRenderer
{
    private const char FilledChar = '█';
    private const char EmptyChar = '░';
    
    /// <summary>
    /// Render mounts as horizontal bar charts grouped by device type.
    /// </summary>
    public static void Render(List<Mount> mounts, BarChartOptions options)
    {
        // Group mounts by device type
        var groups = mounts
            .GroupBy(m => m.DeviceType)
            .OrderBy(g => GetDeviceTypeOrder(g.Key));
        
        foreach (var group in groups)
        {
            // Sort by usage descending (fullest first)
            var sortedMounts = group.OrderByDescending(m => m.Usage).ToList();
            if (sortedMounts.Count == 0)
                continue;
                
            RenderGroup(group.Key, sortedMounts, options);
        }
    }
    
    private static void RenderGroup(string deviceType, List<Mount> mounts, BarChartOptions options)
    {
        var theme = options.Theme;
        var count = mounts.Count;
        var label = $"{count} {deviceType} device{(count == 1 ? "" : "s")}";
        
        // Render group header with rule
        var rule = new Rule($"[bold]{label}[/]")
        {
            Justification = Justify.Left,
            Style = new Style(foreground: theme.ColorGray)
        };
        AnsiConsole.Write(rule);
        
        // Find the longest mount point for alignment
        var maxMountLen = mounts.Max(m => m.Mountpoint.Length);
        maxMountLen = Math.Min(maxMountLen, 30); // Cap at 30 chars
        
        foreach (var mount in mounts)
        {
            RenderBar(mount, options, maxMountLen);
        }
        
        AnsiConsole.WriteLine();
    }
    
    private static void RenderBar(Mount mount, BarChartOptions options, int mountPadding)
    {
        var theme = options.Theme;
        var thresholds = options.UsageThresholds;
        var barWidth = options.BarWidth;
        
        // Truncate mount point if too long
        var mountpoint = mount.Mountpoint;
        if (mountpoint.Length > mountPadding)
        {
            mountpoint = "…" + mountpoint[^(mountPadding - 1)..];
        }
        
        // Get threshold color for used portion
        var usageColor = GetUsageColor(mount.Usage, thresholds, theme);
        
        // Calculate bar segments
        var filledCount = (int)Math.Round(mount.Usage * barWidth);
        filledCount = Math.Clamp(filledCount, 0, barWidth);
        var emptyCount = barWidth - filledCount;
        
        // Build the bar
        var filledBar = new string(FilledChar, filledCount);
        var emptyBar = new string(EmptyChar, emptyCount);
        
        // Format size info (escape brackets for Markup)
        var usedSize = SizeFormatter.Format(mount.Used);
        var totalSize = SizeFormatter.Format(mount.Total);
        var sizeInfo = $"[[{usedSize} / {totalSize}]]";
        
        // Format percentage
        var percentage = $"{mount.Usage * 100:F1}%";
        
        // Build the output line
        var paddedMount = mountpoint.PadRight(mountPadding);
        
        // Create markup with colors
        var markup = new Markup(
            $"[{ToHex(theme.ColorBlue)}]{Markup.Escape(paddedMount)}[/]  " +
            $"[{ToHex(theme.ColorGray)}]{sizeInfo,-20}[/]  " +
            $"[{ToHex(usageColor)}]{filledBar}[/]" +
            $"[{ToHex(theme.ColorGray)}]{emptyBar}[/] " +
            $"[{ToHex(usageColor)}]{percentage,6}[/]"
        );
        
        AnsiConsole.Write(markup);
        AnsiConsole.WriteLine();
    }
    
    private static Color GetUsageColor(double usage, UsageThresholds thresholds, Theme theme)
    {
        if (usage >= thresholds.Red)
            return theme.ColorRed;
        if (usage >= thresholds.Yellow)
            return theme.ColorYellow;
        return theme.ColorGreen;
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
    
    private static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}

