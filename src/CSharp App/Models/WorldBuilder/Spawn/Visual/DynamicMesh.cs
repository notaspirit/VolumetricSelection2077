using Newtonsoft.Json;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Mesh;

namespace VolumetricSelection2077.models.WorldBuilder.Spawn.Visual;

public class DynamicMesh : Mesh
{
    [JsonProperty("startAsleep")]
    public bool StartAsleep { get; set; }

    public DynamicMesh()
    {
        DataType = "Dynamic Mesh";
        ModulePath = "physics/dynamicMesh";
        NodeType = "worldDynamicMeshNode";
        
        StartAsleep = true;
    }
}