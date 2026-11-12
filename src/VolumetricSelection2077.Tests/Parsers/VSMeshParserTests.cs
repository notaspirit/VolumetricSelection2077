using Moq;
using SharpDX;
using VolumetricSelection2077.Core.Parsers;
using WolvenKit.Common.PhysX;
using WolvenKit.RED4.Archive;
using WolvenKit.RED4.Archive.CR2W;
using WolvenKit.RED4.Types;
using Vector3 = SharpDX.Vector3;
using WVector3 = WolvenKit.RED4.Types.Vector3;
using WVector4 = WolvenKit.RED4.Types.Vector4;
using WBox = WolvenKit.RED4.Types.Box;

namespace VolumetricSelection2077.Tests.Parsers;

public class VSMeshParserTests
{
    private class DummyPhysXMesh : PhysXMesh
    {
    }

    [Fact]
    public void GetPolygonIndices_ExtractsIndicesFromConvexHullData()
    {
        var polygon = new HullPolygonData { NbVerts = 4, VRef8 = 2 };
        var hullData = new ConvexHullData
        {
            VertexData8 = [10, 20, 30, 40, 50, 60]
        };

        var indices = VSMeshParser.GetPolygonIndices(polygon, hullData);

        Assert.Equal(new uint[] { 30, 40, 50, 60 }, indices);
    }

    [Fact]
    public void ParseFromPhysX_ConvexMesh_ParsesCorrectly()
    {
        var convexMesh = new ConvexMesh();
        convexMesh.HullData.AABB = new Bounds3 { Minimum = new Vector3(-1, -2, -3), Maximum = new Vector3(1, 2, 3) };
        convexMesh.HullData.HullVertices.AddRange([
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0)
        ]);
        convexMesh.HullData.VertexData8 = [0, 1, 2];
        convexMesh.HullData.Polygons.Add(new HullPolygonData { NbVerts = 3, VRef8 = 0 });

        var mesh = VSMeshParser.ParseFromPhysX(convexMesh);

        Assert.NotNull(mesh);
        var subMesh = Assert.Single(mesh.SubMeshes);
        Assert.Equal(new Vector3(-1, -2, -3), subMesh.BoundingBox.Minimum);
        Assert.Equal(new Vector3(1, 2, 3), subMesh.BoundingBox.Maximum);
        Assert.Equal(3, subMesh.Vertices!.Length);
        Assert.Single(subMesh.PolygonIndices!);
        Assert.Equal(new uint[] { 0, 1, 2 }, subMesh.PolygonIndices![0]);
    }

    [Fact]
    public void ParseFromPhysX_BV4TriangleMesh_ParsesCorrectly()
    {
        var triangleMesh = new BV4TriangleMesh();
        triangleMesh.AABB = new Bounds3 { Minimum = new Vector3(-5, -5, -5), Maximum = new Vector3(5, 5, 5) };
        triangleMesh.Vertices.AddRange([
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0)
        ]);
        triangleMesh.Triangles.Add(new List<uint> { 0, 1, 2 });

        var mesh = VSMeshParser.ParseFromPhysX(triangleMesh);

        Assert.NotNull(mesh);
        var subMesh = Assert.Single(mesh.SubMeshes);
        Assert.Equal(new Vector3(-5, -5, -5), subMesh.BoundingBox.Minimum);
        Assert.Equal(new Vector3(5, 5, 5), subMesh.BoundingBox.Maximum);
        Assert.Equal(3, subMesh.Vertices!.Length);
        Assert.Single(subMesh.PolygonIndices!);
        Assert.Equal(new uint[] { 0, 1, 2 }, subMesh.PolygonIndices![0]);
    }

    [Fact]
    public void ParseFromPhysX_InvalidType_ThrowsArgumentException()
    {
        var dummyMesh = new DummyPhysXMesh();

        Assert.Throws<ArgumentException>(() => VSMeshParser.ParseFromPhysX(dummyMesh));
    }

    [Fact]
    public void ParseFromEmbedded_NotCMesh_ReturnsNull()
    {
        var efileMock = new Mock<ICR2WEmbeddedFile>();
        efileMock.SetupGet(e => e.Content).Returns(new worldStreamingSector());

        var result = VSMeshParser.ParseFromEmbedded(efileMock.Object);

        Assert.Null(result);
    }

    [Fact]
    public void ParseFromEmbedded_CMeshWithoutRenderBlob_ReturnsBoundingBoxMesh()
    {
        var cmesh = new CMesh
        {
            BoundingBox = new WBox
            {
                Min = new WVector4 { X = -10, Y = -20, Z = -30 },
                Max = new WVector4 { X = 10, Y = 20, Z = 30 }
            }
        };

        var efileMock = new Mock<ICR2WEmbeddedFile>();
        efileMock.SetupGet(e => e.Content).Returns(cmesh);

        var result = VSMeshParser.ParseFromEmbedded(efileMock.Object);

        Assert.NotNull(result);
        var subMesh = Assert.Single(result.SubMeshes);
        Assert.Equal(new Vector3(-10, -20, -30), subMesh.BoundingBox.Minimum);
        Assert.Equal(new Vector3(10, 20, 30), subMesh.BoundingBox.Maximum);
        Assert.Null(subMesh.Vertices);
        Assert.Null(subMesh.PolygonIndices);
    }

    [Fact]
    public void ParseFromCR2W_RootChunkNotCMesh_ReturnsNull()
    {
        var cr2w = new CR2WFile();

        var result = VSMeshParser.ParseFromCR2W(cr2w);

        Assert.Null(result);
    }

    [Fact]
    public void ParseFromCR2W_RootChunkCMesh_ReturnsParsedMesh()
    {
        var cr2w = new CR2WFile();
        var cmesh = new CMesh
        {
            BoundingBox = new WBox
            {
                Min = new WVector4 { X = -1, Y = -2, Z = -3 },
                Max = new WVector4 { X = 1, Y = 2, Z = 3 }
            }
        };
        cr2w.RootChunk = cmesh;

        var result = VSMeshParser.ParseFromCR2W(cr2w);

        Assert.NotNull(result);
        var subMesh = Assert.Single(result.SubMeshes);
        Assert.Equal(new Vector3(-1, -2, -3), subMesh.BoundingBox.Minimum);
        Assert.Equal(new Vector3(1, 2, 3), subMesh.BoundingBox.Maximum);
    }

    [Fact]
    public void ParseFromCMesh_WithRenderResourceBlob_ParsesVerticesAndIndices()
    {
        var byteOffsets = new CStatic<CUInt32>(1);
        byteOffsets.Add(6);
        var slotStrides = new CStatic<CUInt8>(1);
        slotStrides.Add(6);

        var rendBlob = new rendRenderMeshBlob();
        rendBlob.Header = new rendRenderMeshBlobHeader
        {
            QuantizationScale = new WVector4 { X = 1, Y = 1, Z = 1, W = 1 },
            QuantizationOffset = new WVector4 { X = 0, Y = 0, Z = 0, W = 0 },
            IndexBufferOffset = 0,
            RenderChunkInfos = new CArray<rendChunk>
            {
                new rendChunk
                {
                    LodMask = 1,
                    NumVertices = 3,
                    NumIndices = 3,
                    ChunkVertices = new rendVertexBufferChunk
                    {
                        ByteOffsets = byteOffsets,
                        VertexLayout = new GpuWrapApiVertexLayoutDesc
                        {
                            SlotStrides = slotStrides // 3 * short (2 bytes) = 6 bytes per vertex
                        }
                    },
                    ChunkIndices = new rendIndexBufferChunk
                    {
                        TeOffset = 0
                    }
                }
            }
        };

        // Write vertices and indices into memory stream
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Index buffer at offset 0: 3 uint16 indices: 0, 1, 2
        bw.Write((ushort)0);
        bw.Write((ushort)1);
        bw.Write((ushort)2);

        // Vertex buffer: 3 vertices with (x, y, z) as int16 (18 bytes total)
        // Vertex 0: (0, 0, 0)
        bw.Write((short)0);
        bw.Write((short)0);
        bw.Write((short)0);

        // Vertex 1: (32767, 0, 0) => 1.0f
        bw.Write((short)32767);
        bw.Write((short)0);
        bw.Write((short)0);

        // Vertex 2: (0, 32767, 0) => 1.0f
        bw.Write((short)0);
        bw.Write((short)32767);
        bw.Write((short)0);

        var bufferBytes = ms.ToArray();
        var redBuffer = new RedBuffer();
        redBuffer.SetBytes(bufferBytes);

        rendBlob.RenderBuffer = new WolvenKit.RED4.Types.DataBuffer
        {
            Buffer = redBuffer
        };

        var cmesh = new CMesh
        {
            RenderResourceBlob = new CHandle<IRenderResourceBlob>(rendBlob)
        };

        var result = VSMeshParser.ParseFromCMesh(cmesh);

        Assert.NotNull(result);
        var subMesh = Assert.Single(result.SubMeshes);
        Assert.Equal(3, subMesh.Vertices!.Length);
        Assert.Single(subMesh.PolygonIndices!);
        Assert.Equal(new uint[] { 0, 1, 2 }, subMesh.PolygonIndices![0]);
        Assert.Equal(0f, subMesh.Vertices[0].X, 2);
        Assert.Equal(1f, subMesh.Vertices[1].X, 2);
        Assert.Equal(1f, subMesh.Vertices[2].Y, 2);
    }
}
