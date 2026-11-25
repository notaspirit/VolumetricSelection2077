using System.Text.RegularExpressions;
using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Core.Components;

public class ResourcePathFilter : IVSNodeQueryFilter
{
    public List<string> ResourcePathRegexPatterns = [];
    public bool Required { get; set; } = false;
    
    public bool Filter(VSNodeData nodeData, VSNode node, int index) => ResourcePathRegexPatterns.Count == 0 ||
                                                                             ResourcePathRegexPatterns.Any(rg => Regex.IsMatch(node.ResourcePath ?? "", rg));
}