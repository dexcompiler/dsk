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
    /// <param name="output">-o, Output fields: mountpoint, size, used, avail, usage, inodes, inodes_used, inodes_avail, inodes_usage, type, filesystem</param>
    /// <param name="sort">-s, Sort output by: mountpoint, size, used, avail, usage, inodes, inodes_used, inodes_avail, inodes_usage, type, filesystem</param>
    /// <param name="width">-w, Max output width</param>
    /// <param name="theme">-t, Color themes: dark, light, ansi</param>
    /// <param name="style">Style: unicode, ascii</param>
    /// <param name="availThreshold">Specifies the coloring threshold (yellow, red) of the avail column</param>
    /// <param name="usageThreshold">Specifies the coloring threshold (yellow, red) of the usage bars (0 to 1)</param>
    /// <param name="inodes">-i, List inode information instead of block usage</param>
    /// <param name="json">-j, Output all devices in JSON format</param>
    /// <param name="warnings">Output all warnings to STDERR</param>
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
        bool warnings = false,
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
        
        // JSON output
        if (json)
        {
            JsonRenderer.Render(mounts);
            return;
        }
        
        // Load theme
        var themeConfig = ThemeManager.LoadTheme(theme);
        
        // Parse thresholds
        var availThresholds = ThemeManager.ParseAvailThreshold(availThreshold);
        var usageThresholds = ThemeManager.ParseUsageThreshold(usageThreshold);
        
        // Parse output columns
        var columns = TableRenderer.ParseColumns(output, inodes);
        
        // Parse sort column
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
        
        // Render tables
        var tableOptions = new TableOptions
        {
            Columns = columns,
            SortBy = sortColumn,
            Style = style,
            Width = terminalWidth,
            Theme = themeConfig,
            AvailThresholds = availThresholds,
            UsageThresholds = usageThresholds,
        };
        
        TableRenderer.Render(mounts, tableOptions);
    }
}

