using Newtonsoft.Json;

namespace VolumetricSelection2077.models.WorldBuilder.Generated;

public class RelativeOffset
{
    [JsonProperty("x")]
    public float X { get; set; }

    [JsonProperty("y")]
    public float Y { get; set; }

    [JsonProperty("z")]
    public float Z { get; set; }

    public RelativeOffset()
    {
        X = 0;
        Y = 0;
        Z = 0;
    }
}