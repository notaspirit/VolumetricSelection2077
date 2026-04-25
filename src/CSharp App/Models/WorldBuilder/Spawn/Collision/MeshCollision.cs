using Newtonsoft.Json;
using VolumetricSelection2077.models.WorldBuilder.Structs;

namespace VolumetricSelection2077.Models.WorldBuilder.Spawn.Collision;

public class MeshCollision : ColliderBase
{
    [JsonProperty("sectorHash")]
    public string? SectorHash { get; set; }
    
    [JsonProperty("shapeHash")]
    public string? ShapeHash { get; set; }
    
    [JsonProperty("meshType")]
    public string? MeshType { get; set; }
    
    [JsonProperty("scale")]
    public Vector3 Scale { get; set; }   
    
    public MeshCollision()
    {
        DataType = "Collision Mesh";
        ModulePath = "collision/meshCollider";
        NodeType = "worldCollisionNode";

        SectorHash = null;
        ShapeHash = null;
        MeshType = null;
        
        Scale = new Vector3(1,1,1);
    }
}