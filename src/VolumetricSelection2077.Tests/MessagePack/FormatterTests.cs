using MessagePack;
using SharpDX;
using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Tests.MessagePack;

public class FormatterTests
{
    [Fact]
    public void Vector3_RoundTrips()
    {
        var value = new Vector3(1.5f, -2.25f, 3f);

        var result = MessagePackSerializer.Deserialize<Vector3>(MessagePackSerializer.Serialize(value));

        Assert.Equal(value, result);
    }

    [Fact]
    public void BoundingBox_RoundTrips()
    {
        var value = new BoundingBox(new Vector3(-1, -2, -3), new Vector3(1, 2, 3));

        var result = MessagePackSerializer.Deserialize<BoundingBox>(MessagePackSerializer.Serialize(value));

        Assert.Equal(value.Minimum, result.Minimum);
        Assert.Equal(value.Maximum, result.Maximum);
    }

    [Fact]
    public void Quaternion_RoundTrips()
    {
        var value = new Quaternion(0.1f, 0.2f, 0.3f, 0.4f);

        var result = MessagePackSerializer.Deserialize<Quaternion>(MessagePackSerializer.Serialize(value));

        Assert.Equal(value, result);
    }

    [Fact]
    public void Plane_RoundTrips()
    {
        var value = new Plane(1, 2, 3, 4);

        var result = MessagePackSerializer.Deserialize<Plane>(MessagePackSerializer.Serialize(value));

        Assert.Equal(value.Normal, result.Normal);
        Assert.Equal(value.D, result.D);
    }

    [Fact]
    public void VSMesh_RoundTrips()
    {
        var value = new VSMesh
        {
            SubMeshes =
            [
                new VSSubMesh
                {
                    BoundingBox = new BoundingBox(new Vector3(-1), new Vector3(1)),
                    Vertices = [new Vector3(0), new Vector3(1)],
                    PolygonIndices = [new uint[] { 0, 1 }]
                }
            ]
        };

        var result = MessagePackSerializer.Deserialize<VSMesh>(MessagePackSerializer.Serialize(value));

        var subMesh = Assert.Single(result.SubMeshes);
        Assert.Equal(value.SubMeshes[0].BoundingBox.Minimum, subMesh.BoundingBox.Minimum);
        Assert.Equal(value.SubMeshes[0].Vertices, subMesh.Vertices);
        Assert.NotNull(subMesh.PolygonIndices);
        Assert.Equal(value.SubMeshes[0].PolygonIndices![0], subMesh.PolygonIndices![0]);
    }

    [Fact]
    public void VSSector_RoundTrips()
    {
        var value = new VSSector
        {
            Nodes =
            [
                new VSNode
                {
                    Type = "worldStaticMeshNode",
                    ResourcePath = "base\\mesh.mesh",
                    DebugName = "node",
                    SectorHash = 42,
                    Actors =
                    [
                        new VSCollisionActor
                        {
                            Transform = new VSTransform
                            {
                                Position = new Vector3(1, 2, 3),
                                Rotation = Quaternion.Identity,
                                Scale = Vector3.One
                            },
                            Shapes =
                            [
                                new VSActorShape
                                {
                                    Transform = new VSTransform
                                    {
                                        Position = Vector3.Zero,
                                        Rotation = Quaternion.Identity,
                                        Scale = Vector3.One
                                    },
                                    Hash = 7,
                                    ShapeType = WolvenKit.RED4.Types.Enums.physicsShapeType.Box
                                }
                            ]
                        }
                    ]
                }
            ],
            NodeData =
            [
                new VSNodeData
                {
                    Transforms =
                    [
                        new VSTransform
                        {
                            Position = new Vector3(4, 5, 6),
                            Rotation = Quaternion.Identity,
                            Scale = Vector3.One
                        }
                    ],
                    NodeIndex = 0,
                    AABB = new BoundingBox(new Vector3(-1), new Vector3(1))
                }
            ]
        };

        var result = MessagePackSerializer.Deserialize<VSSector>(MessagePackSerializer.Serialize(value));

        var node = Assert.Single(result.Nodes);
        Assert.Equal("worldStaticMeshNode", node.Type);
        Assert.Equal("base\\mesh.mesh", node.ResourcePath);
        Assert.Equal("node", node.DebugName);
        Assert.Equal(42UL, node.SectorHash!.Value);
        var actor = Assert.Single(node.Actors!);
        Assert.Equal(new Vector3(1, 2, 3), actor.Transform.Position);
        var shape = Assert.Single(actor.Shapes);
        Assert.Equal(7UL, shape.Hash!.Value);

        var nodeData = Assert.Single(result.NodeData);
        Assert.Equal(0, nodeData.NodeIndex);
        Assert.Equal(new Vector3(4, 5, 6), nodeData.Transforms[0].Position);
        Assert.NotNull(nodeData.AABB);
        Assert.Equal(value.NodeData[0].AABB!.Value.Minimum, nodeData.AABB!.Value.Minimum);
    }
}
