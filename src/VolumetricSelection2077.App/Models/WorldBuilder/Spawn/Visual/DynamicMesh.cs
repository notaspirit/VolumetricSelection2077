using Newtonsoft.Json;
using VolumetricSelection2077.App.Models.WorldBuilder.Spawn.Mesh;

namespace VolumetricSelection2077.App.models.WorldBuilder.Spawn.Visual;

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