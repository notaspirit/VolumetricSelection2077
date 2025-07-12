using Newtonsoft.Json;
using WolvenKit.RED4.Types;

namespace VolumetricSelection2077.Models.WorldBuilder.Spawn.Mesh;

public class ClothMesh : Mesh
{
    [JsonProperty("affectedByWind")]
    public bool AffectedByWind { get; set; }
    
    public ClothMesh()
    {
        DataType = "Cloth Mesh";
        ModulePath = "mesh/clothMesh";
        NodeType = "worldClothMeshNode";
    }
}