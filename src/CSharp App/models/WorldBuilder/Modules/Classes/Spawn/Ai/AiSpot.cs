using System.Collections.Generic;
using Newtonsoft.Json;
using VolumetricSelection2077.Models.WorldBuilder.Modules.Classes.Generated;
using VolumetricSelection2077.Models.WorldBuilder.Modules.Classes.Utils;


namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Ai
{
    public class AiSpot : Visualized
    {
        [JsonProperty("previewNPC")]
        public string PreviewNPC { get; set; }

        [JsonProperty("spawnNPC")]
        public bool SpawnNPC { get; set; }

        [JsonProperty("isWorkspotInfinite")]
        public bool IsWorkspotInfinite { get; set; }

        [JsonProperty("isWorkspotStatic")]
        public bool IsWorkspotStatic { get; set; }

        [JsonProperty("markings")]
        public List<string> Markings { get; set; }

        [JsonProperty("maxPropertyWidth")]
        public double? MaxPropertyWidth { get; set; }

        [JsonProperty("npcID")]
        public string NpcID { get; set; }

        [JsonProperty("npcSpawning")]
        public bool NpcSpawning { get; set; }

        [JsonProperty("cronID")]
        public double? CronID { get; set; }

        [JsonProperty("workspotSpeed")]
        public double WorkspotSpeed { get; set; }

        [JsonProperty("rigs")]
        public List<string> Rigs { get; set; }

        [JsonProperty("apps")]
        public List<string> Apps { get; set; }

        [JsonProperty("workspotDefInfinite")]
        public bool WorkspotDefInfinite { get; set; }

        public AiSpot() : base()
        {
            var settings = new SettingsData();
            
            SpawnListType = "list";
            DataType = "AI Spot";
            SpawnDataPath = "data/spawnables/ai/aiSpot/";
            ModulePath = "ai/aiSpot";
            Node = "worldAISpotNode";
            Description = "Defines a spot at which NPCs use a workspot. Must be used together with a community node.";
            Icon = "MapMarkerStar";
            Previewed = true;
            PreviewColor = "fuchsia";
            PreviewNPC = settings.DefaultAISpotNPC;
            SpawnNPC = true;
            WorkspotSpeed = settings.DefaultAISpotSpeed;
            IsWorkspotInfinite = true;
            IsWorkspotStatic = false;
            Markings = new List<string>();
            MaxPropertyWidth = null;
            NpcID = null;
            NpcSpawning = false;
            CronID = null;
            Rigs = new List<string>();
            Apps = new List<string>();
            WorkspotDefInfinite = false;
            AssetPreviewType = "position";
            StreamingMultiplier = 5;
        }
    }
}
