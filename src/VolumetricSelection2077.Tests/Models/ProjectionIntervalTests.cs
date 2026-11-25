using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Tests.Models;

public class ProjectionIntervalTests
{
    [Fact]
    public void Constructor_StoresBounds()
    {
        var interval = new ProjectionInterval(-2, 3);

        Assert.Equal(-2, interval.Min);
        Assert.Equal(3, interval.Max);
    }

    [Theory]
    [InlineData(0, 10, 5, 15, true)]
    [InlineData(0, 10, 10, 20, true)]
    [InlineData(0, 10, 11, 20, false)]
    [InlineData(0, 10, -5, 0, true)]
    [InlineData(0, 10, -5, -1, false)]
    [InlineData(0, 10, 2, 8, true)]
    [InlineData(0, 10, 8, 2, true)]
    public void Overlaps_ReturnsExpectedResult(float min, float max, float otherMin, float otherMax, bool expected)
    {
        var interval = new ProjectionInterval(min, max);
        var other = new ProjectionInterval(otherMin, otherMax);

        Assert.Equal(expected, interval.Overlaps(other));
    }
}
