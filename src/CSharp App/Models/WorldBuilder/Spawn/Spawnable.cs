using Newtonsoft.Json;
using VolumetricSelection2077.models.WorldBuilder.Structs;

namespace VolumetricSelection2077.Models.WorldBuilder.Spawn;

public class Spawnable
{
    [JsonProperty("modulePath")]
    public string ModulePath { get; set; }
    
    [JsonProperty("dataType")]
    public string DataType { get; set; }

    [JsonProperty("node")]
    public string NodeType { get; set; }
    
    [JsonProperty("spawnData")]
    public string ResourcePath { get; set; }
    
    [JsonProperty("app")]
    public string Appearance { get; set; }
    
    [JsonProperty("position")]
    public Vector4 Position { get; set; }
    
    [JsonProperty("rotation")]
    public EulerAngles EulerRotation { get; set; }
    
    [JsonProperty("primaryRange")]
    public float PrimaryRange { get; set; }
    
    [JsonProperty("secondaryRange")]
    public float SecondaryRange { get; set; }
    
    [JsonProperty("uk10")]
    public int Uk10 { get; set; }
    
    [JsonProperty("uk11")]
    public int Uk11 { get; set; }
    
    public Spawnable()
    {
        DataType = "Spawnable";
        ModulePath = "spawnable";
        NodeType = "worldEntityNode";
        ResourcePath = "base\\spawner\\empty_entity.ent";
        Appearance = "default";
        Position = new Vector4();
        PrimaryRange = 120;
        SecondaryRange = 100;
        Uk10 = 1024;
        Uk11 = 512;
    }
}