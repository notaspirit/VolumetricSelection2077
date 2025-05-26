using MessagePack;
using SharpDX;
using VolumetricSelection2077.Resources;

namespace VolumetricSelection2077.Models
{
    [MessagePackObject]
    public class AbbrSector
    {
        [Key(0)]
        public required AbbrStreamingSectorNodesEntry[] Nodes { get; set; }
    
        [Key(1)]
        public required AbbrStreamingSectorNodeDataEntry[] NodeData { get; set; }
    }

    [MessagePackObject]
    public class AbbrSectorTransform
    {
        [Key(0)]
        public Vector3 Position { get; set; }
    
        [Key(1)]
        public Quaternion Rotation { get; set; }
    
        [Key(2)]
        public Vector3 Scale { get; set; }
    }

    [MessagePackObject]
    public class AbbrStreamingSectorNodeDataEntry
    {
        [Key(0)]
        public required AbbrSectorTransform[] Transforms { get; set; }
    
        [Key(1)]
        public int NodeIndex { get; set; }
        
        [Key(2)]
        public BoundingBox? AABB { get; set; }
        
        [Key(3)]
        public ulong? ProxyRef { get; set; }
    }

    [MessagePackObject]
    public class AbbrStreamingSectorNodesEntry
    {
        [Key(0)]
        public required NodeTypeProcessingOptions.Enum Type { get; set; }
    
        [Key(1)]
        public string? ResourcePath { get; set; }
    
        [Key(2)]
        public AbbrCollisionActors[]? Actors { get; set; }
    
        [Key(3)]
        public ulong? SectorHash { get; set; }
    
        [Key(4)]
        public string? DebugName { get; set; }
        
        [Key(5)]
        public uint? ExpectedNodesUnderProxy { get; set; }
    }

    [MessagePackObject]
    public class AbbrCollisionActors
    {
        [Key(0)]
        public required AbbrSectorTransform Transform { get; set; }
    
        [Key(1)]
        public AbbrActorShapes[]? Shapes { get; set; }
    }

    [MessagePackObject]
    public class AbbrActorShapes
    {
        [Key(0)]
        public required AbbrSectorTransform Transform { get; set; }
    
        [Key(1)]
        public ulong? Hash { get; set; }
    
        [Key(2)]
        public required WolvenKit.RED4.Types.Enums.physicsShapeType ShapeType { get; set; }
    }

}