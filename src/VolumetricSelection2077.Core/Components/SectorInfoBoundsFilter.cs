using SharpDX;
using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Core.Components;

public class SectorInfoBoundsFilter : IVSSectorInfoQueryFilter
{
    public bool Required { get; set; } = true;
    public BoundingBox BoundingBox { get; set; }

    public bool Filter(VSSectorInfo sectorInfo, string name) =>
        BoundingBox.Contains(sectorInfo.BoundingBox) != ContainmentType.Disjoint;
}