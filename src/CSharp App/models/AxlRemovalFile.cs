using Newtonsoft.Json;
using System.Collections.Generic;
using VolumetricSelection2077.Models;

namespace VolumetricSelection2077.Models;

public class AxlModificationFile
{
    [JsonProperty("streaming")]
    public required AxlStreaming Streaming { get; set; }
}

public class AxlStreaming
{
    [JsonProperty("sectors")]
    public required List<AxlSector> Sectors { get; set; }
}

public class AxlSector
{
    [JsonProperty("nodeDeletions")]
    public List<AxlNodeDeletion>? NodeDeletions { get; set; }
    
    [JsonProperty("nodeMutations")]
    public List<AxlNodeMutation>? NodeMutations { get; set; }
    
    [JsonProperty("expectedNodes")]
    public required int ExpectedNodes { get; set; }
    
    [JsonProperty("path")]
    public required string Path { get; set; }
}

public class AxlNodeBase
{
    [JsonProperty("type")]
    public required string Type { get; set; }
    
    [JsonProperty("index")]
    public required int Index { get; set; }
    
    [JsonProperty("debugName")]
    public string? DebugName { get; set; }
    
    [JsonProperty("proxyRef")]
    public string? ProxyRef { get; set; }
}

public class AxlNodeDeletion : AxlNodeBase { }

public class AxlCollisionNodeDeletion : AxlNodeDeletion
{
    [JsonProperty("actorDeletions")]
    public required List<int> ActorDeletions { get; set; }
    
    [JsonProperty("expectedActors")]
    public required int ExpectedActors { get; set; }
}

public class AxlInstancedNodeDeletion : AxlNodeDeletion
{
    [JsonProperty("instanceDeletions")]
    public required List<int> InstanceDeletions { get; set; }
    
    [JsonProperty("expectedInstances")]
    public required int ExpectedInstances { get; set; }
}

public class AxlNodeMutation : AxlNodeBase { }

public class AxlProxyNodeMutationMutation : AxlNodeMutation
{
    [JsonProperty("nbNodesUnderProxyDiff")]
    public required int NbNodesUnderProxyDiff { get; set; }
}