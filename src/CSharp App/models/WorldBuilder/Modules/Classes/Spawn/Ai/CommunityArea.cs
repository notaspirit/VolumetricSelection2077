using System.Collections.Generic;
using Newtonsoft.Json;

namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Ai
{
    // A collection of NPCs, with their phases, time periods and assigned spots
    public class CommunityArea : Visualized
    {
        [JsonProperty("entries")]
        public List<object> Entries; // Should be a list of a generated CommunityEntry class

        [JsonProperty("periodEnums")]
        public List<string> PeriodEnums;

        [JsonProperty("primaryRange")]
        public int PrimaryRange;

        [JsonProperty("secondaryRange")]
        public int SecondaryRange;

        [JsonProperty("previewed")]
        public bool Previewed;

        [JsonProperty("previewColor")]
        public string PreviewColor;

        [JsonProperty("nodeRef")]
        public string NodeRef;

        public CommunityArea() : base()
        {
            DataType = "Community";
            ModulePath = "ai/communityArea";
            Node = "worldCompiledCommunityAreaNode_Streamable";
            Description = "A collection of NPCs, with their phases, time periods and assigned spots.";
            Icon = "AccountGroup";
            Previewed = true;
            PreviewColor = "palegreen";
            PrimaryRange = 250;
            SecondaryRange = 200;
            Entries = new List<object>();
            PeriodEnums = new List<string>();
            NodeRef = string.Empty;
        }
    }
}
