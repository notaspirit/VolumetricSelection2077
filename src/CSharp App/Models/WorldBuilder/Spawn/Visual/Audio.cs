using Newtonsoft.Json;
using VolumetricSelection2077.App.Models.WorldBuilder.Spawn;

namespace VolumetricSelection2077.App.models.WorldBuilder.Spawn.Visual;

public class Audio : Visualized
{
    [JsonProperty("radius")]
    public float Radius { get; set; }
    
    [JsonProperty("emitterMetadataName")]
    public string EmitterMetadataName { get; set; }
    
    [JsonProperty("useDoppler")]
    public bool UseDoppler { get; set; }
    
    [JsonProperty("usePhysicsObstruction")]
    public bool UsePhysicsObstruction { get; set; }

    public Audio()
    {
        DataType = "Sounds";
        ModulePath = "visual/audio";
        NodeType = "worldStaticSoundEmitterNode";
        
        Radius = 5;
        EmitterMetadataName = "";
        UseDoppler = true;
        UsePhysicsObstruction = true;
    }
    
}