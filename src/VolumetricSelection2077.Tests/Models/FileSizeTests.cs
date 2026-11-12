using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Tests.Models;

public class FileSizeTests
{
    [Theory]
    [InlineData(0UL, 0)]
    [InlineData(1024UL, 1)]
    [InlineData(1536UL, 1.5)]
    public void ToKilobytes_ConvertsBytes(ulong bytes, double expected)
    {
        Assert.Equal(expected, new FileSize(bytes).ToKilobytes());
    }

    [Theory]
    [InlineData(0UL, 0)]
    [InlineData(1048576UL, 1)]
    [InlineData(1572864UL, 1.5)]
    public void ToMegabytes_ConvertsBytes(ulong bytes, double expected)
    {
        Assert.Equal(expected, new FileSize(bytes).ToMegabytes());
    }

    [Theory]
    [InlineData(0UL, 0)]
    [InlineData(1073741824UL, 1)]
    [InlineData(1610612736UL, 1.5)]
    public void ToGigabytes_ConvertsBytes(ulong bytes, double expected)
    {
        Assert.Equal(expected, new FileSize(bytes).ToGigabytes());
    }

    [Theory]
    [InlineData(0UL, "0 B")]
    [InlineData(1023UL, "1023 B")]
    [InlineData(1024UL, "1 KB")]
    [InlineData(1536UL, "1,5 KB")]
    [InlineData(1048576UL, "1 MB")]
    [InlineData(1572864UL, "1,5 MB")]
    [InlineData(1073741824UL, "1 GB")]
    [InlineData(1610612736UL, "1,5 GB")]
    public void GetFormattedSize_UsesLargestApplicableUnit(ulong bytes, string expected)
    {
        Assert.Equal(expected, new FileSize(bytes).GetFormattedSize());
    }
}
