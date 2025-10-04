using Newtonsoft.Json;
using WEnums = WolvenKit.RED4.Types.Enums;

namespace VolumetricSelection2077.Models.WorldBuilder.Spawn.Mesh;

public class RotatingMesh : Mesh
{
    [JsonProperty("duration")]
    public float Duration { get; set; }
    
    [JsonProperty("axis")]
    public WEnums.worldRotatingMeshNodeAxis  Axis { get; set; }
    
    [JsonProperty("reverse")]
    public bool Reverse { get; set; }
    
    public RotatingMesh()
    {
        DataType = "Rotating Mesh";
        ModulePath = "mesh/rotatingMesh";
        NodeType = "worldRotatingMeshNode";

        Duration = 5;
        Axis = WEnums.worldRotatingMeshNodeAxis.X;
        Reverse = false;
    }
}