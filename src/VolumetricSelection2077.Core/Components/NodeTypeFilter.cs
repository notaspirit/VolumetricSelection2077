using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Core.Components;

public class NodeTypeFilter : IVSNodeQueryFilter
{
    public HashSet<string> NodeTypes = [];

    public bool Required { get; set; } = true;

    public bool Filter(VSNodeData nodeData, VSNode node, int index) => NodeTypes.Contains(node.Type);
}
