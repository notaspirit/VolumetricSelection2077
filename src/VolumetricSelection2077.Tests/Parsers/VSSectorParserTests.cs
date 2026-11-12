using Moq;
using SharpDX;
using VolumetricSelection2077.Core.Converters.Simple;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Parsers;
using VolumetricSelection2077.Core.Services;
using WolvenKit.RED4.Archive;
using WolvenKit.RED4.Archive.Buffer;
using WolvenKit.RED4.Archive.CR2W;
using WolvenKit.RED4.Types;
using Vector3 = SharpDX.Vector3;
using WVector3 = WolvenKit.RED4.Types.Vector3;
using WVector4 = WolvenKit.RED4.Types.Vector4;
using WBox = WolvenKit.RED4.Types.Box;
using WQuaternion = WolvenKit.RED4.Types.Quaternion;
using worldNodeData = WolvenKit.RED4.Archive.Buffer.worldNodeData;
using Result = VolumetricSelection2077.Core.Models.Result;
using WEnums = WolvenKit.RED4.Types.Enums;

namespace VolumetricSelection2077.Tests.Parsers;

public class VSSectorParserTests
{
    private readonly Mock<IGameFileService> _gameFileServiceMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private class TestNodeDataArray : CArray<worldNodeData>, IParseableBuffer
    {
        public IRedType? Data { get; set; }
        public byte[] GetBytes() => [];
    }

    private class TestCollisionBuffer : CollisionBuffer, IParseableBuffer
    {
        public IRedType? Data { get; set; }
        public byte[] GetBytes() => [];
    }

    private VSSectorParser CreateParser()
    {
        return new VSSectorParser(_gameFileServiceMock.Object, _cacheServiceMock.Object);
    }

    private static WolvenKit.RED4.Types.DataBuffer CreateDataBuffer(IParseableBuffer data)
    {
        return new WolvenKit.RED4.Types.DataBuffer
        {
            Buffer = new RedBuffer
            {
                Data = data
            }
        };
    }

    [Fact]
    public void ParseFromCR2W_RootChunkNotWorldStreamingSector_ReturnsError()
    {
        var cr2w = new CR2WFile();
        var result = CreateParser().ParseFromCR2W(cr2w);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Input file is not a world streaming sector", result.Message);
    }

    [Fact]
    public void ParseFromCR2W_WithEmbeddedMesh_ParsesAndWritesToCache()
    {
        var sector = new worldStreamingSector();
        sector.NodeData = CreateDataBuffer(new TestNodeDataArray());

        var cmesh = new CMesh
        {
            BoundingBox = new WBox
            {
                Min = new WVector4 { X = -1, Y = -1, Z = -1 },
                Max = new WVector4 { X = 1, Y = 1, Z = 1 }
            }
        };

        var cr2w = new CR2WFile();
        cr2w.RootChunk = sector;

        var efileMock = new Mock<ICR2WEmbeddedFile>();
        efileMock.SetupGet(e => e.FileName).Returns("embedded.mesh");
        efileMock.SetupGet(e => e.Content).Returns(cmesh);

        cr2w.EmbeddedFiles.Add(efileMock.Object);

        _cacheServiceMock.Setup(c => c.WriteSingleEntry(It.Is<WriteCacheRequest>(r => r.Key == "embedded.mesh")))
            .Returns(new Result(ResultStatus.Ok));

        var result = CreateParser().ParseFromCR2W(cr2w);

        Assert.Equal(ResultStatus.Ok, result.Status);
        _cacheServiceMock.Verify(c => c.WriteSingleEntry(It.Is<WriteCacheRequest>(r => r.Key == "embedded.mesh")), Times.Once);
    }

    [Fact]
    public void ParseFromCR2W_WithEmbeddedMesh_CacheWriteFailure_ProducesWarning()
    {
        var sector = new worldStreamingSector();
        sector.NodeData = CreateDataBuffer(new TestNodeDataArray());

        var cmesh = new CMesh
        {
            BoundingBox = new WBox
            {
                Min = new WVector4 { X = -1, Y = -1, Z = -1 },
                Max = new WVector4 { X = 1, Y = 1, Z = 1 }
            }
        };

        var cr2w = new CR2WFile();
        cr2w.RootChunk = sector;

        var efileMock = new Mock<ICR2WEmbeddedFile>();
        efileMock.SetupGet(e => e.FileName).Returns("embedded.mesh");
        efileMock.SetupGet(e => e.Content).Returns(cmesh);

        cr2w.EmbeddedFiles.Add(efileMock.Object);

        _cacheServiceMock.Setup(c => c.WriteSingleEntry(It.IsAny<WriteCacheRequest>()))
            .Returns(new Result(ResultStatus.Error, "disk full"));

        var result = CreateParser().ParseFromCR2W(cr2w);

        Assert.Equal(ResultStatus.Warning, result.Status);
        Assert.Contains("Failed to write embedded mesh embedded.mesh to cache: disk full", result.Message);
    }

    [Fact]
    public void ParseFromCR2W_WithValidSector_ParsesMeshNodesAndDefaultNodeData()
    {
        var sector = new worldStreamingSector();
        var meshNode = new worldMeshNode
        {
            Mesh = new CResourceAsyncReference<CMesh>("base\\worlds\\test.mesh"),
            DebugName = "mesh_test_node"
        };
        sector.Nodes.Add(new CHandle<worldNode>(meshNode));

        var nodeDataArray = new TestNodeDataArray
        {
            new worldNodeData
            {
                NodeIndex = 0,
                Position = new WVector4 { X = 10f, Y = 20f, Z = 30f, W = 1f },
                Orientation = new WolvenKit.RED4.Types.Quaternion { I = 0, J = 0, K = 0, R = 1 },
                Scale = new WolvenKit.RED4.Types.Vector3 { X = 1, Y = 1, Z = 1 }
            }
        };

        sector.NodeData = CreateDataBuffer(nodeDataArray);

        var cr2w = new CR2WFile();
        cr2w.RootChunk = sector;

        var result = CreateParser().ParseFromCR2W(cr2w);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        var parsedNode = Assert.Single(result.Data.Nodes);
        Assert.Equal("base\\worlds\\test.mesh", parsedNode.ResourcePath);
        Assert.Equal("mesh_test_node", parsedNode.DebugName);
        Assert.Equal("worldMeshNode", parsedNode.Type);

        var parsedNodeData = Assert.Single(result.Data.NodeData);
        Assert.Equal(0, parsedNodeData.NodeIndex);
        var transform = Assert.Single(parsedNodeData.Transforms);
        Assert.Equal(new Vector3(10f, 20f, 30f), transform.Position);
    }

    [Fact]
    public void ParseFromCR2W_ParsesVariousNodeTypes()
    {
        var sector = new worldStreamingSector();
        sector.Nodes.Add(new CHandle<worldNode>(new worldInstancedMeshNode { Mesh = new CResourceAsyncReference<CMesh>("base\\instanced.mesh") }));
        sector.Nodes.Add(new CHandle<worldNode>(new worldInstancedOccluderNode { Mesh = new CResourceAsyncReference<CMesh>("base\\occluder.mesh") }));
        sector.Nodes.Add(new CHandle<worldNode>(new worldTerrainMeshNode { MeshRef = new CResourceAsyncReference<CMesh>("base\\terrain.mesh") }));
        sector.Nodes.Add(new CHandle<worldNode>(new worldBendedMeshNode { Mesh = new CResourceAsyncReference<CMesh>("base\\bended.mesh") }));
        sector.Nodes.Add(new CHandle<worldNode>(new worldPhysicalDestructionNode { Mesh = new CResourceAsyncReference<CMesh>("base\\destruction.mesh") }));
        sector.Nodes.Add(new CHandle<worldNode>(new worldFoliageNode { Mesh = new CResourceAsyncReference<CMesh>("base\\foliage.mesh") }));
        sector.Nodes.Add(new CHandle<worldNode>(new worldStaticOccluderMeshNode { Mesh = new CResourceAsyncReference<CMesh>("base\\static_occluder.mesh") }));
        sector.Nodes.Add(new CHandle<worldNode>(new worldStaticDecalNode { Material = new CResourceAsyncReference<IMaterial>("base\\decal.mt") }));
        sector.Nodes.Add(new CHandle<worldNode>(new worldEffectNode { Effect = new CResourceAsyncReference<worldEffect>("base\\effect.effect") }));
        sector.Nodes.Add(new CHandle<worldNode>(new worldStaticParticleNode { ParticleSystem = new CResourceAsyncReference<CParticleSystem>("base\\particle.particle") }));
        sector.Nodes.Add(new CHandle<worldNode>(new worldPrefabNode { Prefab = new CResourceAsyncReference<worldPrefab>("base\\prefab.prefab") }));
        sector.Nodes.Add(new CHandle<worldNode>(new worldEntityNode { EntityTemplate = new CResourceAsyncReference<entEntityTemplate>("base\\entity.ent") }));

        sector.NodeData = CreateDataBuffer(new TestNodeDataArray());

        var cr2w = new CR2WFile();
        cr2w.RootChunk = sector;

        var result = CreateParser().ParseFromCR2W(cr2w);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal(12, result.Data.Nodes.Length);
        Assert.Equal("base\\instanced.mesh", result.Data.Nodes[0].ResourcePath);
        Assert.Equal("base\\occluder.mesh", result.Data.Nodes[1].ResourcePath);
        Assert.Equal("base\\terrain.mesh", result.Data.Nodes[2].ResourcePath);
        Assert.Equal("base\\bended.mesh", result.Data.Nodes[3].ResourcePath);
        Assert.Equal("base\\destruction.mesh", result.Data.Nodes[4].ResourcePath);
        Assert.Equal("base\\foliage.mesh", result.Data.Nodes[5].ResourcePath);
        Assert.Equal("base\\static_occluder.mesh", result.Data.Nodes[6].ResourcePath);
        Assert.Equal("base\\decal.mt", result.Data.Nodes[7].ResourcePath);
        Assert.Equal("base\\effect.effect", result.Data.Nodes[8].ResourcePath);
        Assert.Equal("base\\particle.particle", result.Data.Nodes[9].ResourcePath);
        Assert.Equal("base\\prefab.prefab", result.Data.Nodes[10].ResourcePath);
        Assert.Equal("base\\entity.ent", result.Data.Nodes[11].ResourcePath);
    }

    [Fact]
    public void ParseFromCR2W_ParsesCollisionNodeWithActorsAndShapes()
    {
        var sector = new worldStreamingSector();
        var collisionNode = new worldCollisionNode
        {
            SectorHash = 12345UL,
            NumActors = 1
        };

        var collisionBuffer = new TestCollisionBuffer();
        var collisionActor = new CollisionActor
        {
            Position = new WorldPosition
            {
                X = new FixedPoint { Bits = FixedPointVector3Converter.FromFloat(1f) },
                Y = new FixedPoint { Bits = FixedPointVector3Converter.FromFloat(2f) },
                Z = new FixedPoint { Bits = FixedPointVector3Converter.FromFloat(3f) }
            },
            Orientation = new WolvenKit.RED4.Types.Quaternion { I = 0, J = 0, K = 0, R = 1 },
            Scale = new WolvenKit.RED4.Types.Vector3 { X = 1, Y = 1, Z = 1 }
        };

        var boxShape = new CollisionShapeSimple
        {
            ShapeType = WEnums.physicsShapeType.Box,
            Position = new WolvenKit.RED4.Types.Vector3 { X = 0, Y = 0, Z = 0 },
            Rotation = new WolvenKit.RED4.Types.Quaternion { I = 0, J = 0, K = 0, R = 1 },
            Size = new WolvenKit.RED4.Types.Vector3 { X = 2, Y = 3, Z = 4 }
        };

        var meshShape = new CollisionShapeMesh
        {
            ShapeType = WEnums.physicsShapeType.ConvexMesh,
            Position = new WolvenKit.RED4.Types.Vector3 { X = 5, Y = 6, Z = 7 },
            Rotation = new WolvenKit.RED4.Types.Quaternion { I = 0, J = 0, K = 0, R = 1 },
            Hash = 999UL
        };

        collisionActor.Shapes.Add(boxShape);
        collisionActor.Shapes.Add(meshShape);
        collisionBuffer.Actors.Add(collisionActor);

        collisionNode.CompiledData = CreateDataBuffer(collisionBuffer);

        sector.Nodes.Add(new CHandle<worldNode>(collisionNode));
        sector.NodeData = CreateDataBuffer(new TestNodeDataArray());

        var cr2w = new CR2WFile();
        cr2w.RootChunk = sector;

        var result = CreateParser().ParseFromCR2W(cr2w);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        var parsedNode = Assert.Single(result.Data.Nodes);
        Assert.Equal(12345UL, parsedNode.SectorHash);
        Assert.NotNull(parsedNode.Actors);
        var actor = Assert.Single(parsedNode.Actors);
        Assert.Equal(new Vector3(1, 2, 3), actor.Transform.Position);
        Assert.Equal(2, actor.Shapes.Length);

        Assert.Equal(WEnums.physicsShapeType.Box, actor.Shapes[0].ShapeType);
        Assert.Equal(new Vector3(2, 3, 4), actor.Shapes[0].Transform.Scale);

        Assert.Equal(WEnums.physicsShapeType.ConvexMesh, actor.Shapes[1].ShapeType);
        Assert.Equal(999UL, actor.Shapes[1].Hash);
    }

    [Fact]
    public void ParseFromCR2W_WithInstancedOccluderTransforms_DecomposesMatrixTransforms()
    {
        var sector = new worldStreamingSector();
        var occluderNode = new worldInstancedOccluderNode
        {
            Mesh = new CResourceAsyncReference<CMesh>("base\\occluder.mesh"),
            Buffer = new CArray<worldInstancedOccluderNode_Buffer>
            {
                new worldInstancedOccluderNode_Buffer
                {
                    Unknown1 = new WVector4 { X = 1, Y = 0, Z = 0, W = 0 },
                    Unknown2 = new WVector4 { X = 0, Y = 1, Z = 0, W = 0 },
                    Unknown3 = new WVector4 { X = 0, Y = 0, Z = 1, W = 0 },
                    Unknown4 = new WVector4 { X = 10, Y = 20, Z = 30, W = 1 }
                }
            }
        };

        sector.Nodes.Add(new CHandle<worldNode>(occluderNode));

        var nodeDataArray = new TestNodeDataArray
        {
            new worldNodeData
            {
                NodeIndex = 0,
                Position = new WVector4(),
                Orientation = new WolvenKit.RED4.Types.Quaternion { R = 1 },
                Scale = new WolvenKit.RED4.Types.Vector3 { X = 1, Y = 1, Z = 1 }
            }
        };

        sector.NodeData = CreateDataBuffer(nodeDataArray);

        var cr2w = new CR2WFile();
        cr2w.RootChunk = sector;

        var result = CreateParser().ParseFromCR2W(cr2w);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        var nodeData = Assert.Single(result.Data.NodeData);
        var transform = Assert.Single(nodeData.Transforms);
        Assert.Equal(new Vector3(10, 20, 30), transform.Position);
    }

    [Fact]
    public void ParseFromCR2W_WithFoliageNode_UninitializedGameFileService_ProducesWarning()
    {
        var sector = new worldStreamingSector();
        var foliageNode = new worldFoliageNode
        {
            FoliageResource = new CResourceAsyncReference<worldFoliageCompiledResource>("base\\foliage.foliage")
        };

        sector.Nodes.Add(new CHandle<worldNode>(foliageNode));

        var nodeDataArray = new TestNodeDataArray
        {
            new worldNodeData
            {
                NodeIndex = 0,
                Position = new WVector4(),
                Orientation = new WolvenKit.RED4.Types.Quaternion { R = 1 },
                Scale = new WolvenKit.RED4.Types.Vector3 { X = 1, Y = 1, Z = 1 }
            }
        };

        sector.NodeData = CreateDataBuffer(nodeDataArray);

        var cr2w = new CR2WFile();
        cr2w.RootChunk = sector;

        _gameFileServiceMock.SetupGet(g => g.IsInitialized).Returns(false);

        var result = CreateParser().ParseFromCR2W(cr2w);

        Assert.Equal(ResultStatus.Warning, result.Status);
        Assert.Contains("game file service is not initialized", result.Message);
    }
}
