namespace Dsk.Utils;

/// <summary>
/// Formats byte sizes into human-readable strings.
/// </summary>
public static class SizeFormatter
{
    private const ulong KB = 1UL << 10;
    private const ulong MB = 1UL << 20;
    private const ulong GB = 1UL << 30;
    private const ulong TB = 1UL << 40;
    private const ulong PB = 1UL << 50;
    private const ulong EB = 1UL << 60;
    
    /// <summary>
    /// Format a byte size into a human-readable string.
    /// </summary>
    public static string Format(ulong size)
    {
        return size switch
        {
            >= EB => $"{(double)size / EB:F1}E",
            >= PB => $"{(double)size / PB:F1}P",
            >= TB => $"{(double)size / TB:F1}T",
            >= GB => $"{(double)size / GB:F1}G",
            >= MB => $"{(double)size / MB:F1}M",
            >= KB => $"{(double)size / KB:F1}K",
            _ => $"{size}B"
        };
    }
    
    /// <summary>
    /// Parse a size string with optional SI prefix (e.g., "10G", "1T") into bytes.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> input, out ulong size)
    {
        size = 0;
        
        if (input.IsEmpty)
            return false;
            
        // Find where the number ends
        int numberEnd = 0;
        while (numberEnd < input.Length && char.IsDigit(input[numberEnd]))
        {
            numberEnd++;
        }
        
        if (numberEnd == 0)
            return false;
            
        if (!ulong.TryParse(input[..numberEnd], out var number))
            return false;
            
        // Get the suffix if present
        if (numberEnd >= input.Length)
        {
            size = number;
            return true;
        }
        
        char suffix = char.ToUpperInvariant(input[numberEnd]);
        size = suffix switch
        {
            'K' => number * KB,
            'M' => number * MB,
            'G' => number * GB,
            'T' => number * TB,
            'P' => number * PB,
            'E' => number * EB,
            _ => 0
        };
        
        return size > 0 || (suffix is 'K' or 'M' or 'G' or 'T' or 'P' or 'E') && number == 0;
    }
}

