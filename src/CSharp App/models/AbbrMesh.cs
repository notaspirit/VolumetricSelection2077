using SharpDX;
using System.Collections.Generic;

namespace VolumetricSelection2077.Models;

public class AbbrMesh
{
    public required AbbrSubMesh[] SubMeshes { get; set; }
}

public class AbbrSubMesh
{
    public required BoundingBox BoundingBox { get; set; }
}

public class TriangleSubMesh : AbbrSubMesh
{
    public required Vector3[] Vertices { get; set; }
    public required uint[] Indices { get; set; }
}

public class ConvexSubMesh : AbbrSubMesh
{
    public required Vector3[] Vertices { get; set; }
    public required Plane[] Planes { get; set; }
}