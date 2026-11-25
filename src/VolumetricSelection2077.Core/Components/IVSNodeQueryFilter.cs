using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Core.Components;

public interface IVSNodeQueryFilter
{
    bool Required { get; set; }
    bool Filter(VSNodeData nodeData, VSNode node, int index);
}