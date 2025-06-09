using Newtonsoft.Json;
using SharpDX;

namespace VolumetricSelection2077.Models.WorldBuilder.Spawn.Mesh;

public class Mesh : Spawnable
{
    [JsonProperty("scale")]
    public Vector3 Scale { get; set; }
    
    [JsonProperty("windImpulseEnabled")]
    public bool WindImpulseEnabled { get; set; }
    
    [JsonProperty("castLocalShadows")]
    public int CastLocalShadows { get; set; }
    
    [JsonProperty("castRayTracedGlobalShadows")]
    public int CastRayTracedGlobalShadows { get; set; }
    
    [JsonProperty("castRayTracedLocalShadows")]
    public int CastRayTracedLocalShadows { get; set; }
    
    [JsonProperty("castShadows")]
    public int CastShadows { get; set; }
    
    [JsonProperty("bBox")]
    public BoundingBox BoundingBox { get; set; }
    
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
        
        BoundingBox = new BoundingBox();
        Uk10 = 1040;
    }
}