using Newtonsoft.Json;

namespace VolumetricSelection2077.Models.WorldBuilder.Spawn.Collision;

public class MeshCollision : Spawnable
{
    [JsonProperty("sectorHash")]
    public string? SectorHash { get; set; }
    
    [JsonProperty("shapeHash")]
    public string? ShapeHash { get; set; }
    
    [JsonProperty("meshType")]
    public string? MeshType { get; set; }
    
    [JsonProperty("material")]
    public int Material { get; set; }
    
    [JsonProperty("preset")]
    public int Preset { get; set; }
    
    public MeshCollision()
    {
        DataType = "Collision Mesh";
        ModulePath = "collision/meshCollider";
        NodeType = "worldCollisionNode";

        SectorHash = null;
        ShapeHash = null;
        MeshType = null;
        
        Material = 1;
        Preset = 33;
    }
}