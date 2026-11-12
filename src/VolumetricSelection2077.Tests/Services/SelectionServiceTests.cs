using MessagePack;
using Moq;
using SharpDX;
using VolumetricSelection2077.Core.Components;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Services;
using Result = VolumetricSelection2077.Core.Models.Result;
using WEnums = WolvenKit.RED4.Types.Enums;

namespace VolumetricSelection2077.Tests.Services;

public class SelectionServiceTests
{
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<IGameFileService> _gameFileService = new();
    private readonly ProgressService _progressService = new();

    public SelectionServiceTests()
    {
        _cacheService.Setup(x => x.StartListening()).Returns(new Result(ResultStatus.Ok));
        _cacheService.Setup(x => x.StopListening()).Returns(new Result(ResultStatus.Ok));
    }

    private SelectionService CreateService()
    {
        return new SelectionService(_cacheService.Object, _gameFileService.Object, _progressService);
    }

    private static VSQuery CreateQuery(IVSNodeQueryFilter[]? filters = null)
    {
        var bounds = new BoundingBox(new Vector3(-10), new Vector3(10));
        return new VSQuery
        {
            DataSource = WorldDataSource.All,
            AABB = bounds,
            OBB = new OrientedBoundingBox(new Vector3(-10), new Vector3(10)),
            Filters = filters?.ToList() ?? []
        };
    }

    private static VSTransform Transform(Vector3? position = null, Vector3? scale = null)
    {
        return new VSTransform
        {
            Position = position ?? Vector3.Zero,
            Rotation = Quaternion.Identity,
            Scale = scale ?? Vector3.One
        };
    }

    private static VSNode Node() => new()
    {
        Type = "worldStaticMeshNode"
    };

    private static VSNodeData NodeData() => new()
    {
        Transforms = [Transform()],
        NodeIndex = 0
    };

    private static VSSector Sector(VSNode node, VSNodeData nodeData) => new()
    {
        Nodes = [node],
        NodeData = [nodeData]
    };

    private static Result<T> Error<T>(string message)
    {
        var result = new Result<T> { Status = ResultStatus.Error };
        result.AddMessage(message);
        return result;
    }

    [Fact]
    public async Task QueryWorldData_ReturnsErrorWhenListeningFails()
    {
        _cacheService.Setup(x => x.StartListening())
            .Returns(new Result(ResultStatus.Error, "cannot listen"));

        var result = await CreateService().QueryWorldData(CreateQuery());

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("cannot listen", result.Message);
    }

    [Fact]
    public async Task QueryWorldData_ReturnsErrorWhenGettingCachedSectorsFails()
    {
        _cacheService.Setup(x => x.GetAllEntries(It.IsAny<CacheDatabase>()))
            .Returns(Error<KeyValuePair<string, byte[]>[]>("database failure"));

        var result = await CreateService().QueryWorldData(CreateQuery());

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Failed to get cached sectors", result.Message);
        Assert.Contains("database failure", result.Message);
        _cacheService.Verify(x => x.StopListening(), Times.Once);
    }

    [Fact]
    public async Task QueryWorldData_WithoutIntersectingSectors_ReturnsOkWithoutData()
    {
        _cacheService.Setup(x => x.GetAllEntries(It.IsAny<CacheDatabase>()))
            .Returns(new Result<KeyValuePair<string, byte[]>[]>
            {
                Status = ResultStatus.Ok,
                Data = []
            });

        var result = await CreateService().QueryWorldData(CreateQuery());

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Null(result.Data);
        _cacheService.Verify(x => x.StopListening(), Times.Once);
    }

    [Fact]
    public async Task QueryWorldData_ReportsWarningWhenSectorQueryFails()
    {
        var boundingBox = new BoundingBox(new Vector3(-1), new Vector3(1));
        _cacheService.Setup(x => x.GetAllEntries(It.IsAny<CacheDatabase>()))
            .Returns(new Result<KeyValuePair<string, byte[]>[]>
            {
                Status = ResultStatus.Ok,
                Data = [new KeyValuePair<string, byte[]>("sector", MessagePackSerializer.Serialize(boundingBox))]
            });
        _gameFileService.Setup(x => x.GetResource<VSSector>("sector"))
            .Returns(Error<VSSector>("sector missing"));

        var result = await CreateService().QueryWorldData(CreateQuery());

        Assert.Equal(ResultStatus.Warning, result.Status);
        Assert.Contains("Failed to query sector with path sector", result.Message);
        Assert.Null(result.Data);
        _cacheService.Verify(x => x.StopListening(), Times.Once);
    }

    [Fact]
    public async Task QueryWorldData_ReturnsErrorWhenStopListeningFails()
    {
        _cacheService.Setup(x => x.GetAllEntries(It.IsAny<CacheDatabase>()))
            .Returns(new Result<KeyValuePair<string, byte[]>[]>
            {
                Status = ResultStatus.Ok,
                Data = []
            });
        _cacheService.Setup(x => x.StopListening())
            .Returns(new Result(ResultStatus.Error, "cannot stop"));

        var result = await CreateService().QueryWorldData(CreateQuery());

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("cannot stop", result.Message);
    }

    [Fact]
    public async Task QuerySector_ReturnsErrorWhenSectorCannotBeLoaded()
    {
        _gameFileService.Setup(x => x.GetResource<VSSector>("sector"))
            .Returns(Error<VSSector>("missing"));

        var result = await CreateService().QuerySector("sector", CreateQuery());

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Failed to get sector sector", result.Message);
    }

    [Fact]
    public async Task QuerySector_TransformNodeInsideSelection_IsDeleted()
    {
        var node = Node();
        _gameFileService.Setup(x => x.GetResource<VSSector>("sector"))
            .Returns(new Result<VSSector> { Status = ResultStatus.Ok, Data = Sector(node, NodeData()) });

        var result = await CreateService().QuerySector("sector", CreateQuery());

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.ExpectedNodes);
        var deletion = Assert.Single(result.Data.NodeDeletions);
        Assert.Equal(0, deletion.Index);
        Assert.Equal(node.Type, deletion.Type);
        Assert.Empty(deletion.ActorDeletions);
    }

    [Fact]
    public async Task QuerySector_TransformNodeOutsideSelection_IsNotDeleted()
    {
        var node = Node();
        var nodeData = new VSNodeData
        {
            Transforms = [Transform(new Vector3(100))],
            NodeIndex = 0
        };
        _gameFileService.Setup(x => x.GetResource<VSSector>("sector"))
            .Returns(new Result<VSSector> { Status = ResultStatus.Ok, Data = Sector(node, nodeData) });

        var result = await CreateService().QuerySector("sector", CreateQuery());

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task QuerySector_RequiredFilterThatDoesNotMatch_SkipsNode()
    {
        var node = Node();
        var filter = new Mock<IVSNodeQueryFilter>();
        filter.SetupGet(x => x.Required).Returns(true);
        filter.Setup(x => x.Filter(It.IsAny<VSNodeData>(), It.IsAny<VSNode>(), It.IsAny<int>())).Returns(false);
        _gameFileService.Setup(x => x.GetResource<VSSector>("sector"))
            .Returns(new Result<VSSector> { Status = ResultStatus.Ok, Data = Sector(node, NodeData()) });

        var result = await CreateService().QuerySector("sector", CreateQuery([filter.Object]));

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task QuerySector_OptionalFilterThatDoesNotMatch_SkipsNode()
    {
        var node = Node();
        var filter = new Mock<IVSNodeQueryFilter>();
        filter.SetupGet(x => x.Required).Returns(false);
        filter.Setup(x => x.Filter(It.IsAny<VSNodeData>(), It.IsAny<VSNode>(), It.IsAny<int>())).Returns(false);
        _gameFileService.Setup(x => x.GetResource<VSSector>("sector"))
            .Returns(new Result<VSSector> { Status = ResultStatus.Ok, Data = Sector(node, NodeData()) });

        var result = await CreateService().QuerySector("sector", CreateQuery([filter.Object]));

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task QuerySector_MeshNodeWithIntersectingMesh_IsDeleted()
    {
        var node = Node();
        node.ResourcePath = "base\\mesh.mesh";
        var mesh = new VSMesh
        {
            SubMeshes =
            [
                new VSSubMesh
                {
                    BoundingBox = new BoundingBox(new Vector3(-1), new Vector3(1)),
                    Vertices = null,
                    PolygonIndices = null
                }
            ]
        };
        _gameFileService.Setup(x => x.GetResource<VSSector>("sector"))
            .Returns(new Result<VSSector> { Status = ResultStatus.Ok, Data = Sector(node, NodeData()) });
        _gameFileService.Setup(x => x.GetResource<VSMesh>(node.ResourcePath))
            .Returns(new Result<VSMesh> { Status = ResultStatus.Ok, Data = mesh });

        var result = await CreateService().QuerySector("sector", CreateQuery());

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        var deletion = Assert.Single(result.Data.NodeDeletions);
        Assert.Equal(0, deletion.Index);
        Assert.Empty(deletion.ActorDeletions);
    }

    [Fact]
    public async Task QuerySector_MeshNodeWithMissingMesh_ProducesWarning()
    {
        var node = Node();
        node.ResourcePath = "base\\missing.mesh";
        _gameFileService.Setup(x => x.GetResource<VSSector>("sector"))
            .Returns(new Result<VSSector> { Status = ResultStatus.Ok, Data = Sector(node, NodeData()) });
        _gameFileService.Setup(x => x.GetResource<VSMesh>(node.ResourcePath))
            .Returns(Error<VSMesh>("mesh missing"));

        var result = await CreateService().QuerySector("sector", CreateQuery());

        Assert.Equal(ResultStatus.Warning, result.Status);
        Assert.Contains("Failed to query node at index 0", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task QuerySector_CollisionActorWithIntersectingSphere_IsDeleted()
    {
        var node = Node();
        node.SectorHash = 1;
        node.Actors =
        [
            new VSCollisionActor
            {
                Transform = Transform(),
                Shapes =
                [
                    new VSActorShape
                    {
                        Transform = Transform(scale: new Vector3(1)),
                        ShapeType = WEnums.physicsShapeType.Sphere
                    }
                ]
            }
        ];
        _gameFileService.Setup(x => x.GetResource<VSSector>("sector"))
            .Returns(new Result<VSSector> { Status = ResultStatus.Ok, Data = Sector(node, NodeData()) });

        var result = await CreateService().QuerySector("sector", CreateQuery());

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        var deletion = Assert.Single(result.Data.NodeDeletions);
        Assert.Equal(new[] { 0 }, deletion.ActorDeletions);
        Assert.Equal(1, deletion.ExpectedActors);
    }

    [Fact]
    public async Task QuerySector_CollisionMeshLoadFailure_ProducesWarning()
    {
        var node = Node();
        node.SectorHash = 1;
        node.Actors =
        [
            new VSCollisionActor
            {
                Transform = Transform(),
                Shapes =
                [
                    new VSActorShape
                    {
                        Transform = Transform(),
                        Hash = 2,
                        ShapeType = WEnums.physicsShapeType.TriangleMesh
                    }
                ]
            }
        ];
        _gameFileService.Setup(x => x.GetResource<VSSector>("sector"))
            .Returns(new Result<VSSector> { Status = ResultStatus.Ok, Data = Sector(node, NodeData()) });
        _gameFileService.Setup(x => x.GetPhysXMesh(1, 2))
            .ReturnsAsync(Error<VSMesh>("physx missing"));

        var result = await CreateService().QuerySector("sector", CreateQuery());

        Assert.Equal(ResultStatus.Warning, result.Status);
        Assert.Contains("Failed to get collision mesh", result.Message);
        Assert.Null(result.Data);
    }
}
