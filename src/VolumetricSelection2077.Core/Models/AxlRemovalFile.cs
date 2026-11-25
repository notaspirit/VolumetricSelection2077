using Newtonsoft.Json;

namespace VolumetricSelection2077.Core.Models;

public class AxlRemovalFile
{
    [JsonProperty("streaming")] 
    public AxlRemovalStreaming Streaming { get; set; } = new();
}

public class AxlRemovalStreaming
{
    [JsonProperty("sectors")]
    public List<AxlRemovalSector> Sectors { get; set; } = [];
}

public class AxlRemovalSector
{
    [JsonProperty("nodeDeletions")] 
    public List<AxlRemovalNodeDeletion> NodeDeletions { get; set; } = [];
    
    [JsonProperty("expectedNodes")]
    public int ExpectedNodes { get; set; }

    [JsonProperty("path")]
    public string Path { get; set; } = "";
}

public class AxlRemovalNodeDeletion
{
    [JsonProperty("type")] 
    public string Type { get; set; } = "";
    
    [JsonProperty("index")]
    public int Index { get; set; }
    
    [JsonProperty("actorDeletions")] 
    public List<int> ActorDeletions { get; set; } = [];
    
    [JsonProperty("expectedActors")]
    public int ExpectedActors { get; set; }
}