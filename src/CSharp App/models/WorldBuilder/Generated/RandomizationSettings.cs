using Newtonsoft.Json;

namespace VolumetricSelection2077.models.WorldBuilder.Generated;

public class RandomizationSettings
{
    [JsonProperty("randomizeRotation")]
    public bool RandomizeRotation { get; set; }

    [JsonProperty("randomizeRotationAxis")]
    public int RandomizeRotationAxis { get; set; }

    [JsonProperty("randomizeAppearance")]
    public bool RandomizeAppearance { get; set; }

    [JsonProperty("probability")]
    public float Probability { get; set; }
        
    public RandomizationSettings()
    {
        RandomizeRotation = false;
        RandomizeRotationAxis = 2;
        RandomizeAppearance = false;
        Probability = 0.5f;
    }
}
