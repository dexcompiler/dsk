using Spectre.Console;
using Dsk.Utils;

namespace Dsk.Rendering;

/// <summary>
/// Color theme configuration.
/// </summary>
public sealed class Theme
{
    public Color ColorRed { get; init; } = Color.Red;
    public Color ColorYellow { get; init; } = Color.Yellow;
    public Color ColorGreen { get; init; } = Color.Green;
    public Color ColorBlue { get; init; } = Color.Blue;
    public Color ColorGray { get; init; } = Color.Grey;
    public Color ColorMagenta { get; init; } = Color.Magenta1;
    public Color ColorCyan { get; init; } = Color.Cyan1;
    
    public Color ColorBgRed { get; init; } = Color.DarkRed;
    public Color ColorBgYellow { get; init; } = Color.Olive;
    public Color ColorBgGreen { get; init; } = Color.DarkGreen;
}

/// <summary>
/// Availability and usage thresholds for color coding.
/// </summary>
public record struct Thresholds(ulong Yellow, ulong Red);

/// <summary>
/// Usage thresholds as percentages (0.0 to 1.0).
/// </summary>
public record struct UsageThresholds(double Yellow, double Red);

/// <summary>
/// Manages color themes.
/// </summary>
public static class ThemeManager
{
    private static readonly Theme DarkTheme = new()
    {
        ColorRed = new Color(232, 131, 136),      // #E88388
        ColorYellow = new Color(219, 171, 121),   // #DBAB79
        ColorGreen = new Color(168, 204, 140),    // #A8CC8C
        ColorBlue = new Color(113, 190, 242),     // #71BEF2
        ColorGray = new Color(185, 191, 202),     // #B9BFCA
        ColorMagenta = new Color(210, 144, 228),  // #D290E4
        ColorCyan = new Color(102, 194, 205),     // #66C2CD
        ColorBgRed = new Color(45, 27, 27),       // #2d1b1b
        ColorBgYellow = new Color(45, 45, 27),    // #2d2d1b
        ColorBgGreen = new Color(27, 45, 27),     // #1b2d1b
    };
    
    private static readonly Theme LightTheme = new()
    {
        ColorRed = new Color(215, 0, 0),          // #D70000
        ColorYellow = new Color(255, 175, 0),     // #FFAF00
        ColorGreen = new Color(0, 95, 0),         // #005F00
        ColorBlue = new Color(0, 0, 135),         // #000087
        ColorGray = new Color(48, 48, 48),        // #303030
        ColorMagenta = new Color(175, 0, 255),    // #AF00FF
        ColorCyan = new Color(0, 135, 255),       // #0087FF
        ColorBgRed = new Color(255, 222, 222),    // #ffdede
        ColorBgYellow = new Color(255, 244, 208), // #fff4d0
        ColorBgGreen = new Color(230, 255, 230),  // #e6ffe6
    };
    
    private static readonly Theme AnsiTheme = new()
    {
        ColorRed = new Color(255, 0, 0),          // Bright red
        ColorYellow = new Color(255, 255, 0),     // Bright yellow
        ColorGreen = new Color(0, 255, 0),        // Bright green
        ColorBlue = new Color(0, 0, 255),         // Bright blue
        ColorGray = new Color(192, 192, 192),     // Light gray
        ColorMagenta = new Color(255, 0, 255),    // Bright magenta
        ColorCyan = new Color(0, 255, 255),       // Bright cyan
        ColorBgRed = new Color(128, 0, 0),        // Dark red
        ColorBgYellow = new Color(128, 128, 0),   // Olive
        ColorBgGreen = new Color(0, 128, 0),      // Dark green
    };
    
    /// <summary>
    /// Load a theme by name.
    /// </summary>
    public static Theme LoadTheme(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "light" => LightTheme,
            "ansi" => AnsiTheme,
            _ => DarkTheme
        };
    }
    
    /// <summary>
    /// Parse availability threshold string (e.g., "10G,1G").
    /// </summary>
    public static Thresholds ParseAvailThreshold(string threshold)
    {
        var parts = threshold.Split(',', StringSplitOptions.TrimEntries);
        
        ulong yellow = 10UL * 1024 * 1024 * 1024; // 10G default
        ulong red = 1UL * 1024 * 1024 * 1024;     // 1G default
        
        if (parts.Length >= 1 && SizeFormatter.TryParse(parts[0], out var y))
            yellow = y;
        if (parts.Length >= 2 && SizeFormatter.TryParse(parts[1], out var r))
            red = r;
            
        return new Thresholds(yellow, red);
    }
    
    /// <summary>
    /// Parse usage threshold string (e.g., "0.5,0.9").
    /// </summary>
    public static UsageThresholds ParseUsageThreshold(string threshold)
    {
        var parts = threshold.Split(',', StringSplitOptions.TrimEntries);
        
        double yellow = 0.5;
        double red = 0.9;
        
        if (parts.Length >= 1 && double.TryParse(parts[0], out var y))
            yellow = y;
        if (parts.Length >= 2 && double.TryParse(parts[1], out var r))
            red = r;
            
        return new UsageThresholds(yellow, red);
    }
}

