using SharpDX;
using System.Collections.Generic;
using MessagePack;

namespace VolumetricSelection2077.Models;

[MessagePackObject]
public class AbbrMesh
{
    [Key(0)]
    public required AbbrSubMeshes[] SubMeshes { get; set; }
}

[MessagePackObject]
public class AbbrSubMeshes
{
    [Key(0)]
    public required Vector3[] Vertices { get; set; }
    
    [Key(1)]
    public required uint[] Indices { get; set; }
    
    [Key(2)]
    public required BoundingBox BoundingBox { get; set; }
    
    [Key(3)]
    public bool? IsConvexCollider { get; set; }
}