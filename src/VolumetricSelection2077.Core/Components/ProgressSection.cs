namespace VolumetricSelection2077.Core.Components;

public class ProgressSection : IProgressSection
{
    public string Id { get; set; }
    public uint Weight { get; set; }
    public bool BlockingDownstreamUpdates { get; set; }
    public uint Target { get; set; }
    public uint Current { get; set; }
    
    public float Progress => Current / (float)Target * 100;
    public bool IsCompleted => Current >= Target;
    
    public ProgressSection(string id, uint weight, bool blockingDownstreamUpdates = false)
    {
        Id = id;
        Weight = weight;
        BlockingDownstreamUpdates = blockingDownstreamUpdates;
    }
}