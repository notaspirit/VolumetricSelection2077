using Newtonsoft.Json;

namespace VolumetricSelection2077.Models.WorldBuilder.Spawn.Collision;

public class ColliderBase : Spawnable
{
    [JsonProperty("material")]
    public int Material { get; set; }
    
    [JsonProperty("preset")]
    public int Preset { get; set; }
    
    public ColliderBase()
    {
        ModulePath = "entity/colliderBase";
        
        Material = 1;
        Preset = 33;
    }
}