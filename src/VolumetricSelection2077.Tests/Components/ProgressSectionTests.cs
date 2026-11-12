using VolumetricSelection2077.Core.Components;

namespace VolumetricSelection2077.Tests.Components;

public class ProgressSectionTests
{
    [Fact]
    public void Constructor_SetsIdWeightAndBlockingFlag()
    {
        var section = new ProgressSection("section", 3, true);

        Assert.Equal("section", section.Id);
        Assert.Equal(3U, section.Weight);
        Assert.True(section.BlockingDownstreamUpdates);
        Assert.Equal(0U, section.Target);
        Assert.Equal(0U, section.Current);
    }

    [Theory]
    [InlineData(0U, 10U, 0f)]
    [InlineData(5U, 10U, 50f)]
    [InlineData(10U, 10U, 100f)]
    [InlineData(20U, 10U, 200f)]
    public void Progress_IsCurrentOverTargetAsPercentage(uint current, uint target, float expected)
    {
        var section = new ProgressSection("section", 1)
        {
            Current = current,
            Target = target
        };

        Assert.Equal(expected, section.Progress);
    }

    [Theory]
    [InlineData(0U, 1U, false)]
    [InlineData(1U, 1U, true)]
    [InlineData(2U, 1U, true)]
    public void IsCompleted_IsTrueWhenCurrentReachesTarget(uint current, uint target, bool expected)
    {
        var section = new ProgressSection("section", 1)
        {
            Current = current,
            Target = target
        };

        Assert.Equal(expected, section.IsCompleted);
    }
}
