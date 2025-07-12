using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VolumetricSelection2077.Models.WorldBuilder.Spawn.Entity;

public class Entity : Spawnable
{
    [JsonProperty("apps")]
    public string[] Appearances { get; set; }
    
    [JsonProperty("appIndex")]
    public int AppearanceIndex { get; set; }
    
    // use JObject instead of IRedType to avoid deserialization issues due to the abstract class
    [JsonProperty("instanceDataChanges")]
    public Dictionary<string, JObject> InstanceDataChanges { get; set; }
    
    public Entity()
    {
        DataType = "Entity";
        ModulePath = "entity/entity";
        NodeType = "worldEntityNode";
        
        Appearances = new string[0];
        AppearanceIndex = 0;
        InstanceDataChanges = new Dictionary<string, JObject>();
    }
}