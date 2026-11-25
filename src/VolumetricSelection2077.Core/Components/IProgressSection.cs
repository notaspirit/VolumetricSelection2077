namespace VolumetricSelection2077.Core.Components;

public interface IProgressSection
{
    public string Id { get; set; }
    public uint Weight { get; set; }
    public bool BlockingDownstreamUpdates { get; set; }
    public uint Target { get; set; }
    public uint Current { get; set; }
    public float Progress { get; }
    public bool IsCompleted { get;  }
}