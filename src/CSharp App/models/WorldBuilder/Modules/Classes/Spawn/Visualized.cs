using Newtonsoft.Json;

namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn
{
    // Any spawnable that has a basic visualizer
    public class Visualized : Spawnable
    {
        [JsonProperty("previewed")]
        public bool Previewed;
        
        [JsonProperty("previewShape")]
        public string PreviewShape;
        
        [JsonProperty("previewColor")]
        public string PreviewColor;
        
        [JsonProperty("previewMesh")]
        public string PreviewMesh;
        
        [JsonProperty("intersectionMultiplier")]
        public float IntersectionMultiplier;
        
        public Visualized() : base()
        {
            DataType = "Visualized";
            ModulePath = "visualized";
            Previewed = false;
            PreviewShape = "sphere";
            PreviewMesh = string.Empty;
            IntersectionMultiplier = 1;
            PreviewColor = "blue";
        }
    }
}
