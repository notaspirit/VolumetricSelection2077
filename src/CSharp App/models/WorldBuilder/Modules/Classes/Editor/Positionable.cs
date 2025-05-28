using System.Collections.Generic;
using Newtonsoft.Json;
using VolumetricSelection2077.Models.WorldBuilder.Modules.Classes.Generated;

namespace VolumetricSelection2077.Models.WorldBuilder.Modules.Classes.Editor
{
    // Element with position, rotation and optionally scale
    public class Positionable : Element
    {
        [JsonProperty("transformExpanded")]
        public bool TransformExpanded { get; set; }

        [JsonProperty("rotationRelative")]
        public bool RotationRelative { get; set; }

        [JsonProperty("hasScale")]
        public bool HasScale { get; set; }

        [JsonProperty("scaleLocked")]
        public bool ScaleLocked { get; set; }

        [JsonProperty("rotationLocked")]
        public bool RotationLocked { get; set; }

        [JsonProperty("relativeOffset")]
        public RelativeOffsetStruct RelativeOffset { get; set; }

        [JsonProperty("visualizerState")]
        public bool VisualizerState { get; set; }

        [JsonProperty("visualizerDirection")]
        public string VisualizerDirection { get; set; }

        [JsonProperty("controlsHovered")]
        public bool ControlsHovered { get; set; }

        [JsonProperty("randomizationSettings")]
        public Dictionary<string, object> RandomizationSettings { get; set; }

        public Positionable() : base()
        {
            TransformExpanded = true;
            RotationRelative = false;
            HasScale = false;
            ScaleLocked = true;
            RotationLocked = false;
            RelativeOffset = new RelativeOffsetStruct { X = 0, Y = 0, Z = 0 };
            VisualizerState = false;
            VisualizerDirection = "none";
            ControlsHovered = false;
            RandomizationSettings = new Dictionary<string, object> { { "probability", 0.5 } };
            Class.Add("positionable");
        }
    }
}
