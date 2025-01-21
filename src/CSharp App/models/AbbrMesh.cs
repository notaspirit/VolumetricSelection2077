using BulletSharp.Math;
using System.Collections.Generic;
using BulletSharp;

namespace VolumetricSelection2077.Models;

public class AbbrMesh
{
    public required List<AbbrSubMeshes> SubMeshes;
}

public class AbbrSubMeshes
{
    public required List<Vector3> Vertices;
    public required IList<uint> Indices;
    public Aabb? BoundingBox;
    public bool? IsConvexCollider;
}