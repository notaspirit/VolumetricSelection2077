using Newtonsoft.Json;

namespace VolumetricSelection2077.Core.Models.WorldBuilder.Spawn.Visual;

public class DynamicMesh : VolumetricSelection2077.Core.Models.WorldBuilder.Spawn.Mesh.Mesh
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