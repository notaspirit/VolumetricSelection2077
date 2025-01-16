using Newtonsoft.Json;
using System.Collections.Generic;

namespace VolumetricSelection2077.Models;

public class AxlRemovalFile
{
    [JsonProperty("streaming")]
    public required AxlRemovalStreaming Streaming { get; set; }
}

public class AxlRemovalStreaming
{
    [JsonProperty("sectors")]
    public required List<AxlRemovalSector> Sectors { get; set; }
}

public class AxlRemovalSector
{
    [JsonProperty("nodeDeletions")]
    public required List<AxlRemovalNodeDeletion> NodeDeletions { get; set; }
    
    [JsonProperty("expectedNodes")]
    public required int ExpectedNodes { get; set; }
    
    [JsonProperty("path")]
    public required string Path { get; set; }
}

public class AxlRemovalNodeDeletion
{
    [JsonProperty("type")]
    public required string Type { get; set; }
    
    [JsonProperty("actorDeletions")]
    public List<int>? ActorDeletions { get; set; }
    
    [JsonProperty("expectedActors")]
    public int? ExpectedActors { get; set; }
    
    [JsonProperty("index")]
    public required int Index { get; set; }
}