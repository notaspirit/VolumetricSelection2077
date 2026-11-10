using Newtonsoft.Json;
using VolumetricSelection2077.App.Models.WorldBuilder.Spawn;

namespace VolumetricSelection2077.App.Models.WorldBuilder.Editor;

public class SpawnableElement : Positionable
{
    [JsonProperty("spawnable")]
    public Spawnable Spawnable { get; set; }
    
    public SpawnableElement()
    {
        ModulePath = "modules/classes/editor/spawnableElement";
    }
}