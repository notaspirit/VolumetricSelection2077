using Newtonsoft.Json;
using VolumetricSelection2077.models.WorldBuilder.Structs;

namespace VolumetricSelection2077.Models.WorldBuilder.Editor;

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