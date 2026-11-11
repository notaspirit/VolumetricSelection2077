using Newtonsoft.Json;

namespace VolumetricSelection2077.Core.Models.WorldBuilder.Spawn.Visual;

public class WaterPatch : VolumetricSelection2077.Core.Models.WorldBuilder.Spawn.Mesh.Mesh
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