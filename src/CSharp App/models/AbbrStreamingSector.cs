using System.Collections.Generic;
using SharpDX;

namespace VolumetricSelection2077.Models
{

    public class AbbrSector
    {
        public required List<AbbrStreamingSectorNodesEntry> Nodes { get; set; }
        public required List<AbbrStreamingSectorNodeDataEntry> NodeData { get; set; }
    }

    public class AbbrSectorTransform
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }
    }
    
    public class AbbrStreamingSectorNodeDataEntry
    {
        public required List<AbbrSectorTransform> Transforms { get; set; }
        public int NodeIndex { get; set; }
    }
    public class AbbrStreamingSectorNodesEntry
    {
        public required string Type { get; set; }
        public string? MeshDepotPath { get; set; }
        public List<AbbrCollisionActors>? Actors { get; set; }
        public string? SectorHash { get; set; }
        public string? DebugName { get; set; }
    }

    public class AbbrCollisionActors
    {
        public required AbbrSectorTransform Transform { get; set; }
        public List<AbbrActorShapes>? Shapes { get; set; }
    }
    public class AbbrActorShapes
    {
        public required AbbrSectorTransform Transform { get; set; }
        
        public string? Hash { get; set; }
        
        public required string ShapeType { get; set; }
    }
}