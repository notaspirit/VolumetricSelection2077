namespace VolumetricSelection2077.Models;

public class ProxyNodeInfo : AxlNodeMutation
{
    public required string SectorPath { get; set; }
}

public class ProxyNodeTracker
{
    public required ulong ProxyRef { get; set; }
    public required bool IsResolved { get; set; }
    public required int NodesUnderProxyDiff { get; set; }
    public ProxyNodeInfo? NodeInfo { get; set; }
}