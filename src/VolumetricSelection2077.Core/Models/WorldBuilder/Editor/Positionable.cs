using Newtonsoft.Json;
using VolumetricSelection2077.Core.Models.WorldBuilder.Structs;

namespace VolumetricSelection2077.Core.Models.WorldBuilder.Editor;

public class Positionable : Element
{
    [JsonProperty("pos")]
    public Vector4 Position { get; set; }
    
    public Positionable()
    {
        ModulePath = "modules/classes/editor/positionable";
        Position = new Vector4();
    }
}