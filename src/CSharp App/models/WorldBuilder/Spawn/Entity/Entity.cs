using Newtonsoft.Json;

namespace VolumetricSelection2077.Models.WorldBuilder.Spawn.Entity;

public class Entity : Spawnable
{
    [JsonProperty("apps")]
    public string[] Appearances { get; set; }
    
    [JsonProperty("appIndex")]
    public int AppearanceIndex { get; set; }
    
    public Entity()
    {
        DataType = "Entity";
        ModulePath = "entity/entity";
        NodeType = "worldEntityNode";
        
        Appearances = new string[0];
        AppearanceIndex = 0;
    }
}