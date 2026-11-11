using SharpDX;
using VolumetricSelection2077.Core.Components;
using VolumetricSelection2077.Core.Enums;

namespace VolumetricSelection2077.Core.Models;

public class VSQuery
{
    public WorldDataSource DataSource { get; set; }
    public BoundingBox AABB { get; set; }
    public OrientedBoundingBox OBB { get; set; }
    public List<IVSNodeQueryFilter> Filters { get; set; }
    public bool MatchAllFilters { get; set; }
}