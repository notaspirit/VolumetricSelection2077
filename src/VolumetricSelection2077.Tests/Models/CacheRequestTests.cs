using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Tests.Models;

public class CacheRequestTests
{
    [Fact]
    public void ReadCacheRequest_DefaultsToVanillaDatabase()
    {
        var request = new ReadCacheRequest("path");

        Assert.Equal("path", request.Key);
        Assert.Equal(CacheDatabase.Vanilla, request.Database);
    }

    [Fact]
    public void ReadCacheRequest_UsesProvidedDatabase()
    {
        var request = new ReadCacheRequest("path", CacheDatabase.ModdedBounds);

        Assert.Equal("path", request.Key);
        Assert.Equal(CacheDatabase.ModdedBounds, request.Database);
    }

    [Fact]
    public void WriteCacheRequest_DefaultsToVanillaDatabase()
    {
        var data = new byte[] { 1, 2, 3 };

        var request = new WriteCacheRequest("path", data);

        Assert.Equal("path", request.Key);
        Assert.Same(data, request.Data);
        Assert.Equal(CacheDatabase.Vanilla, request.Database);
    }

    [Fact]
    public void WriteCacheRequest_UsesProvidedDatabase()
    {
        var request = new WriteCacheRequest("path", [1, 2, 3], CacheDatabase.VanillaBounds);

        Assert.Equal("path", request.Key);
        Assert.Equal(CacheDatabase.VanillaBounds, request.Database);
    }

    [Fact]
    public void AxlRemovalNodeDeletion_DefaultActorDeletionsIsEmpty()
    {
        var deletion = new AxlRemovalNodeDeletion
        {
            Type = "worldStaticMeshNode",
            Index = 3
        };

        Assert.Empty(deletion.ActorDeletions);
        Assert.Equal(0, deletion.ExpectedActors);
    }
}
