using System.Collections.Generic;
using Newtonsoft.Json;
using SharpDX;

namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Area
{
    // Base type for all area type nodes
    public class Area : Visualized
    {
        [JsonProperty("outlinePath")]
        public string OutlinePath;
        
        [JsonProperty("height")]
        public float Height;
        
        [JsonProperty("markers")]
        public List<Vector4> Markers;
        
        [JsonProperty("maxPropertyWidth")]
        public float MaxPropertyWidth;
        
        public Area() : base()
        {
            DataType = "Area";
            ModulePath = "area/area";
            Node = "worldAreaShapeNode";
            Description = "Base type for all area type nodes. Position is irrelevant, as the actual position is determined by the outline markers.";
            Icon = "Select";
            Previewed = true;
            PreviewColor = "cyan";
            OutlinePath = string.Empty;
            Height = 0;
            Markers = new List<Vector4>();
            MaxPropertyWidth = 0;
        }
    }
}
