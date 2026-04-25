using Newtonsoft.Json;
using VolumetricSelection2077.models.WorldBuilder.Structs;

namespace VolumetricSelection2077.Models.WorldBuilder.Spawn.Collision;

public class Collision : ColliderBase
{
    [JsonProperty("shape")]
    public int Shape { get; set; }
    
    [JsonProperty("material")]
    public int Material { get; set; }
    
    [JsonProperty("preset")]
    public int Preset { get; set; }
    
    [JsonProperty("scale")]
    public Vector3 Scale { get; set; }
    
    public Collision()
    {
        ModulePath = "collision/collider";
        NodeType = "worldCollisionNode";
        Scale = new Vector3(1, 1, 1);
        
        Shape = 0;
        
        Uk10 = 1040;
    }
}