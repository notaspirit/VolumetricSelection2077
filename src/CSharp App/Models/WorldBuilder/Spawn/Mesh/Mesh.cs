using Newtonsoft.Json;
using WEnums = WolvenKit.RED4.Types.Enums;
using Vector3 = VolumetricSelection2077.models.WorldBuilder.Structs.Vector3;

namespace VolumetricSelection2077.Models.WorldBuilder.Spawn.Mesh;

public class Mesh : Spawnable
{
    [JsonProperty("scale")]
    public Vector3 Scale { get; set; }
    
    [JsonProperty("windImpulseEnabled")]
    public bool WindImpulseEnabled { get; set; }
    
    [JsonProperty("castLocalShadows")]
    public WEnums.shadowsShadowCastingMode CastLocalShadows { get; set; }
    
    [JsonProperty("castRayTracedGlobalShadows")]
    public WEnums.shadowsShadowCastingMode CastRayTracedGlobalShadows { get; set; }
    
    [JsonProperty("castRayTracedLocalShadows")]
    public WEnums.shadowsShadowCastingMode CastRayTracedLocalShadows { get; set; }
    
    [JsonProperty("castShadows")]
    public WEnums.shadowsShadowCastingMode CastShadows { get; set; }
    
    public Mesh()
    {
        ModulePath = "mesh/mesh";
        NodeType = "worldMeshNode";
        ResourcePath = @"base\fx\meshes\cube_debug.mesh";
        Scale = new Vector3(1, 1, 1);
        WindImpulseEnabled = true;

        CastLocalShadows = 0;
        CastRayTracedGlobalShadows = 0;
        CastRayTracedLocalShadows = 0;
        CastShadows = 0;
        
        Uk10 = 1040;
    }
}