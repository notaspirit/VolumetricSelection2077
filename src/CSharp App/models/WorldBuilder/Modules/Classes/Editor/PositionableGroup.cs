using Newtonsoft.Json;

namespace VolumetricSelection2077.Models.WorldBuilder.Modules.Classes.Editor
{
    // Organizes multiple objects and/or groups, with position and rotation
    public class PositionableGroup : Positionable
    {
        [JsonProperty("yaw")]
        public float Yaw { get; set; }
        
        [JsonProperty("supportsSaving")]
        public bool SupportsSaving { get; set; }
        
        public PositionableGroup() : base()
        {
            Name = "New Group";
            ModulePath = "modules/classes/editor/positionableGroup";
            Yaw = 0;
            SupportsSaving = true;
            Class.Add("positionableGroup");
        }
    }
}
