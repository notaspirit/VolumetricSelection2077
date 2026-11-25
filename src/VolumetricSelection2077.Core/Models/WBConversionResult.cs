using VolumetricSelection2077.Core.Models.WorldBuilder.Editor;

namespace VolumetricSelection2077.Core.Models;

public class WBConversionResult
{
    public Element RootElement { get; set; }
    /// <summary>
    /// List of file paths used that are embedded in game sectors where key is the parent sector path and value is the list of embedded file paths
    /// </summary>
    public Dictionary<string, List<string>> EmbeddedFiles { get; set; }
}