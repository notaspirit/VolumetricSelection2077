using Newtonsoft.Json;
using WolvenKit.RED4.Types;

namespace VolumetricSelection2077.Models.WorldBuilder.Spawn.Mesh;

public class RotatingMesh : Mesh
{
    [JsonProperty("duration")]
    public float Duration { get; set; }
    
    [JsonProperty("axis")]
    public Enums.worldRotatingMeshNodeAxis  Axis { get; set; }
    
    [JsonProperty("reverse")]
    public bool Reverse { get; set; }
    
    public RotatingMesh()
    {
        DataType = "Rotating Mesh";
        ModulePath = "mesh/rotatingMesh";
        NodeType = "worldRotatingMeshNode";

        Duration = 5;
        Axis = Enums.worldRotatingMeshNodeAxis.X;
        Reverse = false;
    }
}