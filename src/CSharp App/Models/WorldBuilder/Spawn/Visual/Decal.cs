using Newtonsoft.Json;
using VolumetricSelection2077.Models.WorldBuilder.Spawn;
using VolumetricSelection2077.models.WorldBuilder.Structs;

namespace VolumetricSelection2077.models.WorldBuilder.Spawn.Visual;

public class Decal : Spawnable
{
    [JsonProperty("alpha")]
    public float Alpha { get; set; }
    
    [JsonProperty("horizontalFlip")]
    public bool HorizontalFlip { get; set; }
    
    [JsonProperty("verticalFlip")]
    public bool  VerticalFlip { get; set; }
    
    [JsonProperty("autoHideDistance")]
    public float AutoHideDistance { get; set; }
    
    [JsonProperty("scale")]
    public Vector3 Scale { get; set; }
    
    public Decal()
    {
        DataType = "Decals";
        ModulePath = "visual/decal";
        NodeType = "worldStaticDecalNode";

        Alpha = 1;
        HorizontalFlip = false;
        VerticalFlip = false;
        AutoHideDistance = 150;
        
        Scale = new Vector3(1,1,1);
    }
}