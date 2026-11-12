using MessagePack;
using Moq;
using SharpDX;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Parsers;
using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.Tests.Services;

public class GameFileServiceTests
{
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private GameFileService CreateService(bool useModded = false)
    {
        return new GameFileService(_cacheServiceMock.Object, "C:\\Cyberpunk2077\\bin\\x64\\Cyberpunk2077.exe", useModded);
    }

    [Fact]
    public void IsInitialized_InitiallyFalse()
    {
        var service = CreateService();
        Assert.False(service.IsInitialized);
    }

    [Fact]
    public void GetResource_NonGeneric_UnsupportedType_ReturnsError()
    {
        var service = CreateService();

        var result = service.GetResource("some/path", typeof(int));

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Unsupported resource type: Int32", result.Message);
    }

    [Fact]
    public void GetResource_Generic_UnsupportedType_ReturnsError()
    {
        var service = CreateService();

        var result = service.GetResource<int>("some/path");

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Unsupported resource type: Int32", result.Message);
    }

    [Fact]
    public void GetResource_CacheHit_VSMesh_ReturnsCachedMesh()
    {
        var service = CreateService();
        var cachedMesh = new VSMesh
        {
            SubMeshes =
            [
                new VSSubMesh
                {
                    BoundingBox = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1))
                }
            ]
        };
        var bytes = MessagePackSerializer.Serialize(cachedMesh);

        _cacheServiceMock.Setup(c => c.GetEntry(It.Is<ReadCacheRequest>(r => r.Key == "base\\mesh.mesh")))
            .Returns(new Result<byte[]> { Status = ResultStatus.Ok, Data = bytes });

        var result = service.GetResource<VSMesh>("base\\mesh.mesh");

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        var subMesh = Assert.Single(result.Data.SubMeshes);
        Assert.Equal(new Vector3(-1, -1, -1), subMesh.BoundingBox.Minimum);
    }

    [Fact]
    public void GetResource_CacheHit_VSSector_ReturnsCachedSector()
    {
        var service = CreateService();
        var cachedSector = new VSSector
        {
            Nodes =
            [
                new VSNode
                {
                    ResourcePath = "base\\mesh.mesh",
                    Type = "worldMeshNode"
                }
            ],
            NodeData = []
        };
        var bytes = MessagePackSerializer.Serialize(cachedSector);

        _cacheServiceMock.Setup(c => c.GetEntry(It.Is<ReadCacheRequest>(r => r.Key == "base\\sector.streamingsector")))
            .Returns(new Result<byte[]> { Status = ResultStatus.Ok, Data = bytes });

        var result = service.GetResource<VSSector>("base\\sector.streamingsector");

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        var node = Assert.Single(result.Data.Nodes);
        Assert.Equal("base\\mesh.mesh", node.ResourcePath);
    }

    [Fact]
    public void NonGenericGetResource_CallsGenericUnderlyingResource()
    {
        var service = CreateService();
        var cachedMesh = new VSMesh
        {
            SubMeshes =
            [
                new VSSubMesh
                {
                    BoundingBox = new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))
                }
            ]
        };
        var bytes = MessagePackSerializer.Serialize(cachedMesh);

        _cacheServiceMock.Setup(c => c.GetEntry(It.Is<ReadCacheRequest>(r => r.Key == "base\\mesh.mesh")))
            .Returns(new Result<byte[]> { Status = ResultStatus.Ok, Data = bytes });

        var result = service.GetResource("base\\mesh.mesh", typeof(VSMesh));

        Assert.Equal(ResultStatus.Ok, result.Status);
        var genericResult = Assert.IsType<Result<VSMesh>>(result);
        Assert.NotNull(genericResult.Data);
    }

    [Fact]
    public void GetResource_CacheMiss_UninitializedArchiveManager_ReturnsError()
    {
        var service = CreateService();

        _cacheServiceMock.Setup(c => c.GetEntry(It.IsAny<ReadCacheRequest>()))
            .Returns(new Result<byte[]> { Status = ResultStatus.Error });

        var result = service.GetResource<VSMesh>("nonexistent.mesh");

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Failed to get CR2W file", result.Message);
    }
}
