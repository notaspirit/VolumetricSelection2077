using MessagePack;
using SharpDX;
using WEnums = WolvenKit.RED4.Types.Enums;

namespace VolumetricSelection2077.Core.Models
{
    [MessagePackObject]
    public class VSSector
    {
        [Key(0)]
        public required VSSectorNode[] Nodes { get; set; }
    
        [Key(1)]
        public required VSNodeData[] NodeData { get; set; }
    }

    [MessagePackObject]
    public class VSTransform
    {
        [Key(0)]
        public Vector3 Position { get; set; }
    
        [Key(1)]
        public Quaternion Rotation { get; set; }
    
        [Key(2)]
        public Vector3 Scale { get; set; }
    }

    [MessagePackObject]
    public class VSNodeData
    {
        [Key(0)]
        public required VSTransform[] Transforms { get; set; }
    
        [Key(1)]
        public int NodeIndex { get; set; }
        
        [Key(2)]
        public BoundingBox? AABB { get; set; }
    }

    [MessagePackObject]
    public class VSSectorNode
    {
        [Key(0)]
        public string Type { get; set; }
    
        [Key(1)]
        public string? ResourcePath { get; set; }
    
        [Key(2)]
        public VSCollisionActor[]? Actors { get; set; }
    
        [Key(3)]
        public ulong? SectorHash { get; set; }
    
        [Key(4)]
        public string? DebugName { get; set; }
    }

    [MessagePackObject]
    public class VSCollisionActor
    {
        [Key(0)]
        public required VSTransform Transform { get; set; }
    
        [Key(1)]
        public VSActorShape[]? Shapes { get; set; }
    }

    [MessagePackObject]
    public class VSActorShape
    {
        [Key(0)]
        public required VSTransform Transform { get; set; }
    
        [Key(1)]
        public ulong? Hash { get; set; }
    
        [Key(2)]
        public WEnums.physicsShapeType ShapeType { get; set; }
    }

}