using SharpDX;
using System.Collections.Generic;

namespace VolumetricSelection2077.Models;

public class AbbrMesh
{
    public required AbbrSubMeshes[] SubMeshes;
}

public class AbbrSubMeshes
{
    public required Vector3[] Vertices;
    public required uint[] Indices;
    public required BoundingBox BoundingBox;
    public bool? IsConvexCollider;
}