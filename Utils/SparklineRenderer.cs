using Dsk.Services;

namespace Dsk.Utils;

/// <summary>
/// Renders sparkline charts from data series.
/// </summary>
public static class SparklineRenderer
{
    /// <summary>
    /// Render a sparkline for a mount point from history data.
    /// Returns "-" if no history is available.
    /// </summary>
    public static string RenderForMount(HistoryData? history, string mountpoint, bool useAscii = false)
    {
        if (history == null)
            return "-";
            
        var historyPoints = HistoryService.GetHistory(history, mountpoint);
        if (historyPoints.Count == 0)
            return "-";
            
        return Render(historyPoints, width: 8, useAscii: useAscii);
    }

    // Unicode block elements for sparklines (8 levels)
    private static readonly char[] SparkChars = ['▁', '▂', '▃', '▄', '▅', '▆', '▇', '█'];
    
    // ASCII fallback
    private static readonly char[] AsciiChars = ['_', '.', '-', '=', '+', '*', '#', '@'];
    
    /// <summary>
    /// Render a sparkline from a series of values.
    /// </summary>
    /// <param name="values">Data points (0.0 to 1.0 for usage percentages)</param>
    /// <param name="width">Number of characters in output</param>
    /// <param name="useAscii">Use ASCII characters instead of Unicode</param>
    /// <returns>Sparkline string</returns>
    public static string Render(IReadOnlyList<double> values, int width = 8, bool useAscii = false)
    {
        if (values.Count == 0)
            return new string(useAscii ? '-' : '·', width);
            
        var chars = useAscii ? AsciiChars : SparkChars;
        var result = new char[width];
        
        // If we have fewer values than width, pad with the oldest value
        var paddedValues = new List<double>();
        if (values.Count < width)
        {
            // Pad left with empty indicators
            for (int i = 0; i < width - values.Count; i++)
            {
                paddedValues.Add(-1); // Sentinel for "no data"
            }
        }
        paddedValues.AddRange(values.TakeLast(width));
        
        // Find min/max for scaling (only from actual data)
        var actualValues = paddedValues.Where(v => v >= 0).ToList();
        if (actualValues.Count == 0)
            return new string(useAscii ? '-' : '·', width);
            
        var min = actualValues.Min();
        var max = actualValues.Max();
        var range = max - min;
        
        // If all values are the same, show middle height
        if (range < 0.001)
        {
            var midIndex = (int)(actualValues[0] * (chars.Length - 1));
            midIndex = Math.Clamp(midIndex, 0, chars.Length - 1);
            var midChar = chars[midIndex];
            
            for (int i = 0; i < width; i++)
            {
                result[i] = paddedValues[i] < 0 ? (useAscii ? ' ' : '·') : midChar;
            }
            return new string(result);
        }
        
        // Map each value to a character
        for (int i = 0; i < width; i++)
        {
            if (paddedValues[i] < 0)
            {
                result[i] = useAscii ? ' ' : '·';
                continue;
            }
            
            var normalized = (paddedValues[i] - min) / range;
            var charIndex = (int)(normalized * (chars.Length - 1));
            charIndex = Math.Clamp(charIndex, 0, chars.Length - 1);
            result[i] = chars[charIndex];
        }
        
        return new string(result);
    }
    
    /// <summary>
    /// Get a trend indicator (up, down, stable) based on recent values.
    /// </summary>
    public static TrendDirection GetTrend(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
            return TrendDirection.Stable;
            
        var recent = values.TakeLast(3).ToList();
        var first = recent.First();
        var last = recent.Last();
        var diff = last - first;
        
        if (diff > 0.02) return TrendDirection.Up;
        if (diff < -0.02) return TrendDirection.Down;
        return TrendDirection.Stable;
    }
}

public enum TrendDirection
{
    Stable,
    Up,
    Down
}

