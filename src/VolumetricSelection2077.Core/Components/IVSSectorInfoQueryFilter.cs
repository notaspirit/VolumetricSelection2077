using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Core.Components;

public interface IVSSectorInfoQueryFilter
{
    bool Required { get; set; }
    bool Filter(VSSectorInfo sectorInfo, string name);
}