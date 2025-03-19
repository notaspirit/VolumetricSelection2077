using MessagePack;
using SharpDX;

namespace VolumetricSelection2077.Models;

[MessagePackObject]
public class AbbrMesh
{
    [Key(0)]
    public required AbbrSubMesh[] SubMeshes { get; set; }
}

[MessagePackObject]
public class AbbrSubMesh
{
    [Key(0)]
    public required BoundingBox BoundingBox { get; set; }
    [Key(1)]
    public required Polygon[] Polygons { get; set; }
}

[MessagePackObject]
public class Polygon
{
    [Key(0)]
    public required Vector3[] Vertices { get; set; }
    [Key(1)]
    public required Plane Plane { get; set; }
}
