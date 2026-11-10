using MessagePack;
using SharpDX;

namespace VolumetricSelection2077.Core.Models;

[MessagePackObject]
public class VSMesh
{
    [Key(0)]
    public required VSSubMesh[] SubMeshes { get; set; }
}

[MessagePackObject]
public class VSSubMesh
{
    [Key(0)]
    public required BoundingBox BoundingBox { get; set; }
    [Key(1)]
    public Vector3[]? Vertices { get; set; }
    [Key(2)]
    public uint[][]? PolygonIndices { get; set; }
}