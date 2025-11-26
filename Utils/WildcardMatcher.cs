namespace Dsk.Utils;

/// <summary>
/// Simple wildcard pattern matching (supports * and ? wildcards)
/// </summary>
public static class WildcardMatcher
{
    /// <summary>
    /// Match a pattern against a string.
    /// * matches any sequence of characters
    /// ? matches any single character
    /// </summary>
    public static bool Match(string pattern, string text)
    {
        return Match(pattern.AsSpan(), text.AsSpan());
    }
    
    private static bool Match(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text)
    {
        int patternIdx = 0;
        int textIdx = 0;
        int starIdx = -1;
        int matchIdx = 0;
        
        while (textIdx < text.Length)
        {
            if (patternIdx < pattern.Length && 
                (pattern[patternIdx] == '?' || char.ToLowerInvariant(pattern[patternIdx]) == char.ToLowerInvariant(text[textIdx])))
            {
                patternIdx++;
                textIdx++;
            }
            else if (patternIdx < pattern.Length && pattern[patternIdx] == '*')
            {
                starIdx = patternIdx;
                matchIdx = textIdx;
                patternIdx++;
            }
            else if (starIdx != -1)
            {
                patternIdx = starIdx + 1;
                matchIdx++;
                textIdx = matchIdx;
            }
            else
            {
                return false;
            }
        }
        
        while (patternIdx < pattern.Length && pattern[patternIdx] == '*')
        {
            patternIdx++;
        }
        
        return patternIdx == pattern.Length;
    }
}

