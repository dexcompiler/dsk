using Dsk.Models;
using Dsk.Services;
using Dsk.Rendering;
using Dsk.Filtering;
using ConsoleAppFramework;

namespace Dsk;

/// <summary>
/// dsk - Disk Usage/Free Utility
/// </summary>
public class DskCommands
{
    /// <summary>
    /// Display disk usage information.
    /// </summary>
    /// <param name="all">-a, Include pseudo, duplicate, inaccessible file systems</param>
    /// <param name="hide">Hide specific devices, separated with commas: local, network, fuse, special, loops, binds</param>
    /// <param name="hideFs">Hide specific filesystems, separated with commas</param>
    /// <param name="hideMp">Hide specific mount points, separated with commas (supports wildcards)</param>
    /// <param name="only">Show only specific devices, separated with commas: local, network, fuse, special, loops, binds</param>
    /// <param name="onlyFs">Only specific filesystems, separated with commas</param>
    /// <param name="onlyMp">Only specific mount points, separated with commas (supports wildcards)</param>
    /// <param name="output">-o, Output fields: mountpoint, size, used, avail, usage, inodes, inodes_used, inodes_avail, inodes_usage, type, filesystem, trend</param>
    /// <param name="sort">-s, Sort output by: mountpoint, size, used, avail, usage, inodes, inodes_used, inodes_avail, inodes_usage, type, filesystem</param>
    /// <param name="width">-w, Max output width</param>
    /// <param name="theme">-t, Color themes: dark, light, ansi</param>
    /// <param name="style">Style: unicode, ascii</param>
    /// <param name="availThreshold">Specifies the coloring threshold (yellow, red) of the avail column</param>
    /// <param name="usageThreshold">Specifies the coloring threshold (yellow, red) of the usage bars (0 to 1)</param>
    /// <param name="inodes">-i, List inode information instead of block usage</param>
    /// <param name="json">-j, Output all devices in JSON format (deprecated, use --format json)</param>
    /// <param name="format">-f, Output format: table, json, csv, markdown, html</param>
    /// <param name="warnings">Output all warnings to STDERR</param>
    /// <param name="noSave">Don't save usage data to history</param>
    /// <param name="paths">Specific devices or mount points to display</param>
    [Command("")]
    public void Run(
        bool all = false,
        string? hide = null,
        string? hideFs = null,
        string? hideMp = null,
        string? only = null,
        string? onlyFs = null,
        string? onlyMp = null,
        string? output = null,
        string sort = "mountpoint",
        uint width = 0,
        string theme = "dark",
        string style = "unicode",
        string availThreshold = "10G,1G",
        string usageThreshold = "0.5,0.9",
        bool inodes = false,
        bool json = false,
        string format = "table",
        bool warnings = false,
        bool noSave = false,
        [Argument] params string[] paths)
    {
        // Get mount provider for current platform
        var mountProvider = MountProviderFactory.Create();
        
        // Read mounts
        var (mounts, mountWarnings) = mountProvider.GetMounts();
        
        // Print warnings if requested
        if (warnings)
        {
            foreach (var warning in mountWarnings)
            {
                Console.Error.WriteLine(warning);
            }
        }
        
        // Build filter options
        var filterOptions = new FilterOptions
        {
            IncludeAll = all,
            HiddenDevices = FilterOptions.ParseCommaSeparated(hide),
            OnlyDevices = FilterOptions.ParseCommaSeparated(only),
            HiddenFilesystems = FilterOptions.ParseCommaSeparated(hideFs),
            OnlyFilesystems = FilterOptions.ParseCommaSeparated(onlyFs),
            HiddenMountPoints = FilterOptions.ParseCommaSeparated(hideMp),
            OnlyMountPoints = FilterOptions.ParseCommaSeparated(onlyMp),
        };
        
        // Filter mounts by specific paths if provided
        if (paths.Length > 0)
        {
            mounts = MountFilter.FilterByPaths(mounts, paths);
        }
        
        // Apply filters
        mounts = MountFilter.Apply(mounts, filterOptions);
        
        // Determine effective format (--json flag overrides --format for backwards compat)
        var effectiveFormat = json ? "json" : format.ToLowerInvariant();
        
        // Save current usage to history (skip for non-table formats)
        if (!noSave && effectiveFormat == "table")
        {
            var currentUsage = mounts.Select(m => (m.Mountpoint, m.Usage));
            HistoryService.Save(currentUsage);
        }
        var history = HistoryService.Load();
        
        // Parse output columns (used by multiple formats)
        var columns = TableRenderer.ParseColumns(output, inodes);
        
        // Handle different output formats
        switch (effectiveFormat)
        {
            case "json":
                JsonRenderer.Render(mounts);
                return;
                
            case "csv":
                CsvRenderer.Render(mounts, columns);
                return;
                
            case "markdown":
            case "md":
                MarkdownRenderer.Render(mounts, columns);
                return;
                
            case "html":
                HtmlRenderer.Render(mounts, columns);
                return;
        }
        
        // Default: table format
        var themeConfig = ThemeManager.LoadTheme(theme);
        var availThresholds = ThemeManager.ParseAvailThreshold(availThreshold);
        var usageThresholds = ThemeManager.ParseUsageThreshold(usageThreshold);
        var sortColumn = TableRenderer.ParseSortColumn(sort);
        
        // Determine terminal width
        int terminalWidth;
        if (width > 0)
        {
            terminalWidth = (int)width;
        }
        else
        {
            try
            {
                terminalWidth = Console.WindowWidth;
            }
            catch
            {
                terminalWidth = 80;
            }
        }
        if (terminalWidth <= 0) terminalWidth = 80;
        
        // Render table
        var tableOptions = new TableOptions
        {
            Columns = columns,
            SortBy = sortColumn,
            Style = style,
            Width = terminalWidth,
            Theme = themeConfig,
            AvailThresholds = availThresholds,
            UsageThresholds = usageThresholds,
            History = history,
        };
        
        TableRenderer.Render(mounts, tableOptions);
    }
}

