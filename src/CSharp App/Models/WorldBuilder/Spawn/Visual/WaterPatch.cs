using Newtonsoft.Json;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Mesh;

namespace VolumetricSelection2077.models.WorldBuilder.Spawn.Visual;

public class WaterPatch : Mesh
{
    [JsonProperty("depth")]
    public float Depth { get; set; }
    
    public WaterPatch()
    {
        DataType = "Water Patch";
        ModulePath = "visual/waterPatch";
        NodeType = "worldWaterPatchNode";
        Depth = 2;
    }
}