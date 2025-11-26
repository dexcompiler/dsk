using Dsk.Utils;
using Xunit;

namespace Dsk.Tests;

public class WildcardMatcherTests
{
    [Theory]
    [InlineData("*", "anything", true)]
    [InlineData("*.txt", "file.txt", true)]
    [InlineData("*.txt", "file.doc", false)]
    [InlineData("file*", "filename", true)]
    [InlineData("file*", "document", false)]
    [InlineData("/dev/*", "/dev/sda1", true)]
    [InlineData("/dev/*", "/home/user", false)]
    [InlineData("?ello", "hello", true)]
    [InlineData("?ello", "jello", true)]
    [InlineData("?ello", "ello", false)]
    [InlineData("/sys/*", "/sys/fs/cgroup", true)]
    [InlineData("/snap*", "/snap/core/123", true)]
    public void Match_ReturnsExpectedResult(string pattern, string text, bool expected)
    {
        var result = WildcardMatcher.Match(pattern, text);
        Assert.Equal(expected, result);
    }
}

