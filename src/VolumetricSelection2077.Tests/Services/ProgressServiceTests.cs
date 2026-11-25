using VolumetricSelection2077.Core.Components;
using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.Tests.Services;

public class ProgressServiceTests
{
    [Fact]
    public void Progress_WithoutSections_IsZero()
    {
        var service = new ProgressService();

        Assert.Equal(0f, service.Progress);
    }

    [Fact]
    public void Progress_IsWeightedAverageOfSections()
    {
        var service = new ProgressService();
        var first = new ProgressSection("first", 1) { Target = 10, Current = 5 };
        var second = new ProgressSection("second", 3) { Target = 10, Current = 10 };
        service.AddSection(first);
        service.AddSection(second);

        Assert.Equal(87.5f, service.Progress);
    }

    [Fact]
    public void AddSection_DuplicateId_IsIgnored()
    {
        var service = new ProgressService();
        service.AddSection(new ProgressSection("section", 1) { Target = 10 });
        service.AddSection(new ProgressSection("section", 100) { Target = 10, Current = 10 });

        service.AddProgress("section", 5);

        Assert.Equal(50f, service.Progress);
    }

    [Fact]
    public void AddProgress_UnknownSection_IsIgnored()
    {
        var service = new ProgressService();
        service.AddSection(new ProgressSection("section", 1) { Target = 10 });

        service.AddProgress("unknown", 10);

        Assert.Equal(0f, service.Progress);
    }

    [Fact]
    public void AddTarget_UnknownSection_IsIgnored()
    {
        var service = new ProgressService();
        var section = new ProgressSection("section", 1) { Target = 10 };
        service.AddSection(section);

        service.AddTarget("unknown", 10);

        Assert.Equal(10U, section.Target);
    }

    [Fact]
    public void AddTargetAndProgress_Accumulate()
    {
        var service = new ProgressService();
        var section = new ProgressSection("section", 1);
        service.AddSection(section);

        service.AddTarget("section", 10);
        service.AddTarget("section", 10);
        service.AddProgress("section", 5);
        service.AddProgress("section", 5);

        Assert.Equal(20U, section.Target);
        Assert.Equal(10U, section.Current);
        Assert.Equal(50f, service.Progress);
    }

    [Fact]
    public void Progress_IncompleteBlockingSection_StopsDownstreamContribution()
    {
        var service = new ProgressService();
        var blocking = new ProgressSection("blocking", 1, true) { Target = 10, Current = 5 };
        var downstream = new ProgressSection("downstream", 1) { Target = 10, Current = 10 };
        service.AddSection(blocking);
        service.AddSection(downstream);

        Assert.Equal(25f, service.Progress);
    }

    [Fact]
    public void Progress_CompletedBlockingSection_AllowsDownstreamContribution()
    {
        var service = new ProgressService();
        var blocking = new ProgressSection("blocking", 1, true) { Target = 10, Current = 10 };
        var downstream = new ProgressSection("downstream", 1) { Target = 10, Current = 10 };
        service.AddSection(blocking);
        service.AddSection(downstream);

        Assert.Equal(100f, service.Progress);
    }

    [Fact]
    public void Reset_ZeroesTargetAndCurrentWithoutRemovingSections()
    {
        var service = new ProgressService();
        var section = new ProgressSection("section", 1) { Target = 10, Current = 5 };
        service.AddSection(section);

        service.Reset();

        Assert.Equal(0U, section.Target);
        Assert.Equal(0U, section.Current);

        service.AddTarget("section", 10);
        service.AddProgress("section", 5);
        Assert.Equal(50f, service.Progress);
    }

    [Fact]
    public void Clear_RemovesAllSections()
    {
        var service = new ProgressService();
        var section = new ProgressSection("section", 1) { Target = 10, Current = 10 };
        service.AddSection(section);

        service.Clear();
        service.AddProgress("section", 10);

        Assert.Equal(0f, service.Progress);
    }
}
