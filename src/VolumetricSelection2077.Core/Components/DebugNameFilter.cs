using System.Text.RegularExpressions;
using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Core.Components;

public class DebugNameFilter : IVSNodeQueryFilter
{
    public List<string> DebugNameRegexPatterns = [];

    public bool Required { get; set; } = false;

    public bool Filter(VSNodeData nodeData, VSNode node, int index) => DebugNameRegexPatterns.Count == 0 ||
                                                                       DebugNameRegexPatterns.Any(rg => Regex.IsMatch(node.DebugName ?? "", rg));
}