using VolumetricSelection2077.Core.Components;
using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Tests.Components;

public class NodeFiltersTests
{
    private static VSNodeData NodeData() => new()
    {
        Transforms = [],
        NodeIndex = 0
    };

    private static VSNode Node(string? resourcePath = null, string? debugName = null) => new()
    {
        Type = "worldStaticMeshNode",
        ResourcePath = resourcePath,
        DebugName = debugName
    };

    [Fact]
    public void DebugNameFilter_NoPatterns_MatchesEverything()
    {
        var filter = new DebugNameFilter();

        Assert.True(filter.Filter(NodeData(), Node(debugName: null), 0));
    }

    [Theory]
    [InlineData("foo", ".*foo.*", true)]
    [InlineData("Foo", "^foo$", false)]
    [InlineData(null, "^foo$", false)]
    public void DebugNameFilter_MatchesAgainstDebugName(string? debugName, string pattern, bool expected)
    {
        var filter = new DebugNameFilter
        {
            DebugNameRegexPatterns = [pattern]
        };

        Assert.Equal(expected, filter.Filter(NodeData(), Node(debugName: debugName), 0));
    }

    [Fact]
    public void DebugNameFilter_MatchesAnyPattern()
    {
        var filter = new DebugNameFilter
        {
            DebugNameRegexPatterns = ["^missing$", "foo"]
        };

        Assert.True(filter.Filter(NodeData(), Node(debugName: "some_foo_node"), 0));
    }

    [Fact]
    public void ResourcePathFilter_NoPatterns_MatchesEverything()
    {
        var filter = new ResourcePathFilter();

        Assert.True(filter.Filter(NodeData(), Node(resourcePath: null), 0));
    }

    [Theory]
    [InlineData("base\\worlds\\foo.mesh", @".*\.mesh$", true)]
    [InlineData("base\\worlds\\foo.w2mesh", @".*\.mesh$", false)]
    [InlineData(null, @".*\.mesh$", false)]
    public void ResourcePathFilter_MatchesAgainstResourcePath(string? resourcePath, string pattern, bool expected)
    {
        var filter = new ResourcePathFilter
        {
            ResourcePathRegexPatterns = [pattern]
        };

        Assert.Equal(expected, filter.Filter(NodeData(), Node(resourcePath: resourcePath), 0));
    }

    [Fact]
    public void Filters_AreNotRequiredByDefault()
    {
        Assert.False(new DebugNameFilter().Required);
        Assert.False(new ResourcePathFilter().Required);
    }
}
