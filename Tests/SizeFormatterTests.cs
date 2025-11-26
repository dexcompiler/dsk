using Dsk.Utils;
using Xunit;

namespace Dsk.Tests;

public class SizeFormatterTests
{
    [Theory]
    [InlineData(0UL, "0B")]
    [InlineData(512UL, "512B")]
    [InlineData(1024UL, "1.0K")]
    [InlineData(1536UL, "1.5K")]
    [InlineData(1048576UL, "1.0M")]
    [InlineData(1073741824UL, "1.0G")]
    [InlineData(1099511627776UL, "1.0T")]
    public void Format_ReturnsExpectedValue(ulong size, string expected)
    {
        var result = SizeFormatter.Format(size);
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData("1K", 1024UL)]
    [InlineData("10G", 10737418240UL)]
    [InlineData("1T", 1099511627776UL)]
    [InlineData("100M", 104857600UL)]
    public void TryParse_ValidInput_ReturnsExpectedValue(string input, ulong expected)
    {
        var success = SizeFormatter.TryParse(input, out var result);
        Assert.True(success);
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("10X")]
    public void TryParse_InvalidInput_ReturnsFalse(string input)
    {
        var success = SizeFormatter.TryParse(input, out _);
        Assert.False(success);
    }
}

