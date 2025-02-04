using SharpDX;
using System.Collections.Generic;

namespace VolumetricSelection2077.Models;

public class AbbrMesh
{
    public required List<AbbrSubMeshes> SubMeshes;
}

public class AbbrSubMeshes
{
    public required List<Vector3> Vertices;
    public required IList<uint> Indices;
    public required BoundingBox BoundingBox;
    public bool? IsConvexCollider;
}