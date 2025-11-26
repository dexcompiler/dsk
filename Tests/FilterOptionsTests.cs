using Dsk.Filtering;
using Xunit;

namespace Dsk.Tests;

public class FilterOptionsTests
{
    [Fact]
    public void ParseCommaSeparated_Null_ReturnsEmptySet()
    {
        var result = FilterOptions.ParseCommaSeparated(null);
        Assert.Empty(result);
    }
    
    [Fact]
    public void ParseCommaSeparated_Empty_ReturnsEmptySet()
    {
        var result = FilterOptions.ParseCommaSeparated("");
        Assert.Empty(result);
    }
    
    [Fact]
    public void ParseCommaSeparated_SingleValue_ReturnsSingleItem()
    {
        var result = FilterOptions.ParseCommaSeparated("local");
        Assert.Single(result);
        Assert.Contains("local", result);
    }
    
    [Fact]
    public void ParseCommaSeparated_MultipleValues_ReturnsAllItems()
    {
        var result = FilterOptions.ParseCommaSeparated("local,network,fuse");
        Assert.Equal(3, result.Count);
        Assert.Contains("local", result);
        Assert.Contains("network", result);
        Assert.Contains("fuse", result);
    }
    
    [Fact]
    public void ParseCommaSeparated_ValuesWithSpaces_TrimsAndReturnsItems()
    {
        var result = FilterOptions.ParseCommaSeparated(" local , network ");
        Assert.Equal(2, result.Count);
        Assert.Contains("local", result);
        Assert.Contains("network", result);
    }
    
    [Fact]
    public void ParseCommaSeparated_CaseInsensitive()
    {
        var result = FilterOptions.ParseCommaSeparated("LOCAL,Network,FUSE");
        Assert.Contains("local", result);
        Assert.Contains("network", result);
        Assert.Contains("fuse", result);
    }
}

