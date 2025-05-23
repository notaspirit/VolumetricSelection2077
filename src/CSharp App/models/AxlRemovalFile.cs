using Newtonsoft.Json;
using System.Collections.Generic;
using VolumetricSelection2077.Models;
using YamlDotNet.Serialization;

namespace VolumetricSelection2077.Models;

public class AxlModificationFile
{
    [JsonProperty("streaming")]
    [YamlMember(Alias = "streaming")]
    public required AxlStreaming Streaming { get; set; }
}

public class AxlStreaming
{
    [JsonProperty("sectors")]
    [YamlMember(Alias = "sectors")]
    public required List<AxlSector> Sectors { get; set; }
}

public class AxlSector
{
    [JsonProperty("nodeDeletions")]
    [YamlMember(Alias = "nodeDeletions")]
    public List<AxlNodeDeletion>? NodeDeletions { get; set; }
    
    [JsonProperty("nodeMutations")]
    [YamlMember(Alias = "nodeMutations")]
    public List<AxlNodeMutation>? NodeMutations { get; set; }
    
    [JsonProperty("expectedNodes")]
    [YamlMember(Alias = "expectedNodes")]
    public required int ExpectedNodes { get; set; }
    
    [JsonProperty("path")]
    [YamlMember(Alias = "path")]
    public required string Path { get; set; }
}

public class AxlNodeBase
{
    [JsonProperty("type")]
    [YamlMember(Alias = "type")]
    public required string Type { get; set; }
    
    [JsonProperty("index")]
    [YamlMember(Alias = "index")]
    public required int Index { get; set; }
    
    [JsonProperty("debugName")]
    [YamlMember(Alias = "debugName")]
    public string? DebugName { get; set; }

    [JsonProperty("proxyRef")]
    [YamlMember(Alias = "proxyRef")]
    public ulong? ProxyRef { get; set; }
}

public class AxlNodeDeletion : AxlNodeBase { }

public class AxlCollisionNodeDeletion : AxlNodeDeletion
{
    [JsonProperty("actorDeletions")]
    [YamlMember(Alias = "actorDeletions")]
    public required List<int> ActorDeletions { get; set; }
    
    [JsonProperty("expectedActors")]
    [YamlMember(Alias = "expectedActors")]
    public required int ExpectedActors { get; set; }
}

public class AxlInstancedNodeDeletion : AxlNodeDeletion
{
    [JsonProperty("instanceDeletions")]
    [YamlMember(Alias = "instanceDeletions")]
    public required List<int> InstanceDeletions { get; set; }
    
    [JsonProperty("expectedInstances")]
    [YamlMember(Alias = "expectedInstances")]
    public required int ExpectedInstances { get; set; }
}

public class AxlNodeMutation : AxlNodeBase { }

public class AxlProxyNodeMutationMutation : AxlNodeMutation
{
    [JsonProperty("nbNodesUnderProxyDiff")]
    [YamlMember(Alias = "nbNodesUnderProxyDiff")]
    public required int NbNodesUnderProxyDiff { get; set; }
}