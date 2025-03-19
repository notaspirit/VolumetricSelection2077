using SharpDX;

namespace VolumetricSelection2077.Models;
public class AbbrMesh
{
    public required AbbrSubMesh[] SubMeshes { get; set; }
}

public class AbbrSubMesh
{
    public required BoundingBox BoundingBox { get; set; }
    public required Polygon[] Polygons { get; set; }
}

public class Polygon
{
    public required Vector3[] Vertices { get; set; }
    public required Plane Plane { get; set; }
}
