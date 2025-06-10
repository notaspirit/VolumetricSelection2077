using Newtonsoft.Json;
using WolvenKit.RED4.Types;
using Vector3 = VolumetricSelection2077.models.WorldBuilder.Structs.Vector3;

namespace VolumetricSelection2077.Models.WorldBuilder.Spawn.Mesh;

public class Mesh : Spawnable
{
    [JsonProperty("scale")]
    public Vector3 Scale { get; set; }
    
    [JsonProperty("windImpulseEnabled")]
    public bool WindImpulseEnabled { get; set; }
    
    [JsonProperty("castLocalShadows")]
    public Enums.shadowsShadowCastingMode CastLocalShadows { get; set; }
    
    [JsonProperty("castRayTracedGlobalShadows")]
    public Enums.shadowsShadowCastingMode CastRayTracedGlobalShadows { get; set; }
    
    [JsonProperty("castRayTracedLocalShadows")]
    public Enums.shadowsShadowCastingMode CastRayTracedLocalShadows { get; set; }
    
    [JsonProperty("castShadows")]
    public Enums.shadowsShadowCastingMode CastShadows { get; set; }
    
    public Mesh()
    {
        ModulePath = "mesh/mesh";
        NodeType = "worldMeshNode";
        Scale = new Vector3(1, 1, 1);
        WindImpulseEnabled = true;

        CastLocalShadows = 0;
        CastRayTracedGlobalShadows = 0;
        CastRayTracedLocalShadows = 0;
        CastShadows = 0;
        
        Uk10 = 1040;
    }
}