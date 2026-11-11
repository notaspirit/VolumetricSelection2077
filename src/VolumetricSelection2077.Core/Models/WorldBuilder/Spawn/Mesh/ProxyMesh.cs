using Newtonsoft.Json;

namespace VolumetricSelection2077.Core.Models.WorldBuilder.Spawn.Mesh;

public class ProxyMesh : Mesh
{
    [JsonProperty("nearAutoHideDistance")]
    public float NearAutoHideDistance { get; set; }
    
    public ProxyMesh()
    {
        ModulePath = "mesh/proxyMesh";
        NodeType = "worldGenericProxyMeshNode";
        
        NearAutoHideDistance = 15f;
    }
}