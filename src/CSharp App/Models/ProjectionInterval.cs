namespace VolumetricSelection2077.Models;

public struct ProjectionInterval
{
    public float Min { get; }
    public float Max { get; }
        
    public ProjectionInterval(float min, float max)
    {
        Min = min;
        Max = max;
    }
        
    public bool Overlaps(ProjectionInterval other)
    {
        return Max >= other.Min && Min <= other.Max;
    }
}