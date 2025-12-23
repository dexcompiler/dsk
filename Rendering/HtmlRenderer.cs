using System.Text;
using System.Web;
using Dsk.Models;
using Dsk.Services;
using Dsk.Utils;

namespace Dsk.Rendering;

/// <summary>
/// Renders mount information as styled HTML table.
/// </summary>
public static class HtmlRenderer
{
    /// <summary>
    /// Render mounts as HTML table to stdout.
    /// </summary>
    public static void Render(List<Mount> mounts, List<ColumnId> columns, HistoryData? history = null)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("  <title>Disk Usage - dsk</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine(GetStyles());
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"container\">");
        sb.AppendLine("    <h1>Disk Usage</h1>");
        sb.AppendLine($"    <p class=\"timestamp\">Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        
        // Group by device type (same order as TableRenderer)
        var groups = mounts.GroupBy(m => m.DeviceType).OrderBy(g => GetDeviceTypeOrder(g.Key));
        
        foreach (var group in groups)
        {
            sb.AppendLine($"    <h2>{group.Count()} {group.Key} device{(group.Count() == 1 ? "" : "s")}</h2>");
            sb.AppendLine("    <table>");
            
            // Header
            sb.AppendLine("      <thead><tr>");
            foreach (var col in columns)
            {
                var align = GetAlignment(col);
                sb.AppendLine($"        <th class=\"{align}\">{HttpUtility.HtmlEncode(GetColumnHeader(col))}</th>");
            }
            sb.AppendLine("      </tr></thead>");
            
            // Body
            sb.AppendLine("      <tbody>");
            foreach (var mount in group)
            {
                sb.AppendLine("        <tr>");
                foreach (var col in columns)
                {
                    var (value, cssClass) = GetCellValueWithClass(mount, col, history);
                    var align = GetAlignment(col);
                    sb.AppendLine($"          <td class=\"{align} {cssClass}\">{HttpUtility.HtmlEncode(value)}</td>");
                }
                sb.AppendLine("        </tr>");
            }
            sb.AppendLine("      </tbody>");
            sb.AppendLine("    </table>");
        }
        
        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        
        Console.Write(sb.ToString());
    }
    
    private static string GetStyles()
    {
        return """
            :root {
              --bg: #1a1b26;
              --fg: #c0caf5;
              --accent: #7aa2f7;
              --green: #9ece6a;
              --yellow: #e0af68;
              --red: #f7768e;
              --gray: #565f89;
              --border: #3b4261;
            }
            * { box-sizing: border-box; margin: 0; padding: 0; }
            body {
              font-family: 'SF Mono', 'Cascadia Code', 'Consolas', monospace;
              background: var(--bg);
              color: var(--fg);
              padding: 2rem;
              line-height: 1.6;
            }
            .container { max-width: 1200px; margin: 0 auto; }
            h1 { color: var(--accent); margin-bottom: 0.5rem; }
            h2 { color: var(--gray); margin: 2rem 0 1rem; font-size: 1rem; font-weight: normal; }
            .timestamp { color: var(--gray); font-size: 0.85rem; margin-bottom: 1rem; }
            table {
              width: 100%;
              border-collapse: collapse;
              background: #24283b;
              border-radius: 8px;
              overflow: hidden;
            }
            th, td { padding: 0.75rem 1rem; border-bottom: 1px solid var(--border); }
            th { background: #1f2335; font-weight: 600; text-transform: uppercase; font-size: 0.75rem; letter-spacing: 0.05em; }
            tr:last-child td { border-bottom: none; }
            tr:hover td { background: #292e42; }
            .left { text-align: left; }
            .right { text-align: right; }
            .center { text-align: center; }
            .mountpoint { color: var(--accent); }
            .usage-low { color: var(--green); }
            .usage-med { color: var(--yellow); }
            .usage-high { color: var(--red); }
            .muted { color: var(--gray); }
    """;
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
    
    private static string GetAlignment(ColumnId column)
    {
        return column switch
        {
            ColumnId.Size or ColumnId.Used or ColumnId.Avail or 
            ColumnId.Inodes or ColumnId.InodesUsed or ColumnId.InodesAvail => "right",
            ColumnId.Usage or ColumnId.InodesUsage => "center",
            _ => "left"
        };
    }
    
    private static (string Value, string CssClass) GetCellValueWithClass(Mount mount, ColumnId column, HistoryData? history)
    {
        return column switch
        {
            ColumnId.Mountpoint => (mount.Mountpoint, "mountpoint"),
            ColumnId.Size => (SizeFormatter.Format(mount.Total), ""),
            ColumnId.Used => (SizeFormatter.Format(mount.Used), ""),
            ColumnId.Avail => (SizeFormatter.Format(mount.Free), GetAvailClass(mount.Free)),
            ColumnId.Usage => ($"{mount.Usage * 100:F1}%", GetUsageClass(mount.Usage)),
            ColumnId.Inodes => (mount.Inodes.ToString("N0"), ""),
            ColumnId.InodesUsed => (mount.InodesUsed.ToString("N0"), ""),
            ColumnId.InodesAvail => (mount.InodesFree.ToString("N0"), ""),
            ColumnId.InodesUsage => ($"{mount.InodeUsage * 100:F1}%", GetUsageClass(mount.InodeUsage)),
            ColumnId.Type => (mount.Fstype, "muted"),
            ColumnId.Filesystem => (mount.Device, "muted"),
            ColumnId.Trend => GetTrendValue(mount, history),
            _ => ("", "")
        };
    }
    
    private static (string Value, string CssClass) GetTrendValue(Mount mount, HistoryData? history)
    {
        if (history == null)
            return ("-", "muted");
            
        var historyPoints = HistoryService.GetHistory(history, mount.Mountpoint);
        if (historyPoints.Count == 0)
            return ("-", "muted");
            
        // Use Unicode sparkline for HTML
        var sparkline = SparklineRenderer.Render(historyPoints, width: 8, useAscii: false);
        return (sparkline, "trend");
    }
    
    private static string GetUsageClass(double usage)
    {
        if (usage >= 0.9) return "usage-high";
        if (usage >= 0.5) return "usage-med";
        return "usage-low";
    }
    
    private static string GetAvailClass(ulong free)
    {
        if (free < 1UL << 30) return "usage-high";  // < 1GB
        if (free < 10UL << 30) return "usage-med";  // < 10GB
        return "usage-low";
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
}

