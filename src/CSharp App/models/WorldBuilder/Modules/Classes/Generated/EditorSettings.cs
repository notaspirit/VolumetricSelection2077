using Newtonsoft.Json;

namespace VolumetricSelection2077.Models.WorldBuilder.Modules.Classes.Generated
{
    public class EditorSettings
    {
        [JsonProperty("color")]
        public int Color { get; set; }

        public EditorSettings()
        {
            Color = 1;
        }
    }
}
