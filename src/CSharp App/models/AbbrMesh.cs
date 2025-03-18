using SharpDX;
using System.Collections.Generic;

namespace VolumetricSelection2077.Models;

public class AbbrMesh
{
    public required AbbrSubMeshes[] SubMeshes { get; set; }
}

public class AbbrSubMeshes
{
    public required Vector3[] Vertices { get; set; }
    public required uint[] Indices { get; set; }
    public required BoundingBox BoundingBox { get; set; }
    public bool? IsConvexCollider { get; set; }
}