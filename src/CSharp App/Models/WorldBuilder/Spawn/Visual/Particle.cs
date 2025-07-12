using Newtonsoft.Json;
using VolumetricSelection2077.Models.WorldBuilder.Spawn;

namespace VolumetricSelection2077.models.WorldBuilder.Spawn.Visual;

public class Particle : Visualized
{
    [JsonProperty("emissionRate")]
    public float EmissionRate { get; set; }
    
    public Particle()
    {
        DataType = "Particles";
        ModulePath = "visual/particle";
        NodeType = "worldStaticParticleNode";
        
        EmissionRate = 1;
    }
}