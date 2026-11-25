using SharpDX;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.Tests.Services;

public class CollisionCheckServiceTests
{
    private static readonly BoundingBox SelectionAabb =
        new(new Vector3(-5), new Vector3(5));

    private static readonly OrientedBoundingBox SelectionObb =
        new(new Vector3(-5), new Vector3(5));

    private static VSTransform Transform(Vector3? position = null, Vector3? scale = null)
    {
        return new VSTransform
        {
            Position = position ?? Vector3.Zero,
            Rotation = Quaternion.Identity,
            Scale = scale ?? Vector3.One
        };
    }

    private static VSMesh UnitMesh() => new()
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

    [Fact]
    public void IsCollisionBoxInsideSelectionBox_TrueWhenOverlapping()
    {
        var shape = new VSActorShape
        {
            Transform = Transform(),
            ShapeType = WolvenKit.RED4.Types.Enums.physicsShapeType.Box
        };

        Assert.True(CollisionCheckService.IsCollisionBoxInsideSelectionBox(
            shape, Transform(), SelectionAabb, SelectionObb));
    }

    [Fact]
    public void IsCollisionBoxInsideSelectionBox_FalseWhenDisjoint()
    {
        var shape = new VSActorShape
        {
            Transform = Transform(),
            ShapeType = WolvenKit.RED4.Types.Enums.physicsShapeType.Box
        };

        Assert.False(CollisionCheckService.IsCollisionBoxInsideSelectionBox(
            shape, Transform(new Vector3(100)), SelectionAabb, SelectionObb));
    }

    [Fact]
    public void IsCollisionCapsuleInsideSelectionBox_TrueWhenOverlapping()
    {
        var shape = new VSActorShape
        {
            Transform = Transform(scale: new Vector3(1, 2, 1)),
            ShapeType = WolvenKit.RED4.Types.Enums.physicsShapeType.Capsule
        };

        Assert.True(CollisionCheckService.IsCollisionCapsuleInsideSelectionBox(
            shape, Transform(), SelectionAabb, SelectionObb));
    }

    [Fact]
    public void IsCollisionCapsuleInsideSelectionBox_FalseWhenDisjoint()
    {
        var shape = new VSActorShape
        {
            Transform = Transform(scale: new Vector3(1, 2, 1)),
            ShapeType = WolvenKit.RED4.Types.Enums.physicsShapeType.Capsule
        };

        Assert.False(CollisionCheckService.IsCollisionCapsuleInsideSelectionBox(
            shape, Transform(new Vector3(100)), SelectionAabb, SelectionObb));
    }

    [Fact]
    public void IsCollisionSphereInsideSelectionBox_TrueWhenOverlapping()
    {
        var shape = new VSActorShape
        {
            Transform = Transform(scale: new Vector3(1)),
            ShapeType = WolvenKit.RED4.Types.Enums.physicsShapeType.Sphere
        };

        Assert.True(CollisionCheckService.IsCollisionSphereInsideSelectionBox(
            shape, Transform(), SelectionObb));
    }

    [Fact]
    public void IsCollisionSphereInsideSelectionBox_FalseWhenDisjoint()
    {
        var shape = new VSActorShape
        {
            Transform = Transform(scale: new Vector3(1)),
            ShapeType = WolvenKit.RED4.Types.Enums.physicsShapeType.Sphere
        };

        Assert.False(CollisionCheckService.IsCollisionSphereInsideSelectionBox(
            shape, Transform(new Vector3(100)), SelectionObb));
    }

    [Fact]
    public void IsMeshInsideBox_WithMatrix_TrueWhenOverlapping()
    {
        Assert.True(CollisionCheckService.IsMeshInsideBox(UnitMesh(), SelectionObb, SelectionAabb, Matrix.Identity));
    }

    [Fact]
    public void IsMeshInsideBox_WithMatrix_FalseWhenDisjoint()
    {
        Assert.False(CollisionCheckService.IsMeshInsideBox(UnitMesh(), SelectionObb, SelectionAabb,
            Matrix.Translation(new Vector3(100))));
    }

    [Fact]
    public void IsMeshInsideBox_WithTransforms_ReturnsIndicesOfMatchingTransforms()
    {
        VSTransform[] transforms =
        [
            Transform(new Vector3(100)),
            Transform(),
            Transform()
        ];

        var result = CollisionCheckService.IsMeshInsideBox(UnitMesh(), SelectionObb, SelectionAabb, transforms);

        Assert.Equal(new[] { 1, 2 }, result);
    }

    [Fact]
    public void IsMeshInsideBox_WithTransforms_ReturnsEmptyWhenNoTransformMatches()
    {
        VSTransform[] transforms =
        [
            Transform(new Vector3(100)),
            Transform(new Vector3(-100))
        ];

        var result = CollisionCheckService.IsMeshInsideBox(UnitMesh(), SelectionObb, SelectionAabb, transforms);

        Assert.Empty(result);
    }

    [Fact]
    public void IsCollisonMeshInsideSelectionBox_AppliesActorAndShapeTransforms()
    {
        Assert.True(CollisionCheckService.IsCollisonMeshInsideSelectionBox(
            UnitMesh(), SelectionObb, SelectionAabb, Transform(), Transform()));

        Assert.False(CollisionCheckService.IsCollisonMeshInsideSelectionBox(
            UnitMesh(), SelectionObb, SelectionAabb, Transform(new Vector3(100)), Transform()));
    }
}
