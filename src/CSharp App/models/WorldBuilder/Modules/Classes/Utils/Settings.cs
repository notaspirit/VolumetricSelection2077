using System.Collections.Generic;
using Newtonsoft.Json;
using VolumetricSelection2077.Models.WorldBuilder.Modules.Classes.Generated;

namespace VolumetricSelection2077.Models.WorldBuilder.Modules.Classes.Utils
{
    public class SettingsData
    {
        [JsonProperty("spawnPos")]
        public int SpawnPos { get; set; }
        
        [JsonProperty("spawnDist")]
        public float SpawnDist { get; set; }

        [JsonProperty("posSteps")]
        public float PosSteps { get; set; }

        [JsonProperty("precisionMultiplier")]
        public float PrecisionMultiplier { get; set; }

        [JsonProperty("rotSteps")]
        public float RotSteps { get; set; }

        [JsonProperty("despawnOnReload")]
        public bool DespawnOnReload { get; set; }

        [JsonProperty("headerState")]
        public bool HeaderState { get; set; }

        [JsonProperty("deleteConfirm")]
        public bool DeleteConfirm { get; set; }

        [JsonProperty("moveCloneToParent")]
        public int MoveCloneToParent { get; set; }

        [JsonProperty("spawnUIOnlyNames")]
        public bool SpawnUIOnlyNames { get; set; }

        [JsonProperty("editor")]
        public EditorSettings Editor { get; set; }

        [JsonProperty("colliderColor")]
        public int ColliderColor { get; set; }

        [JsonProperty("selectedType")]
        public string SelectedType { get; set; }

        [JsonProperty("lastVariants")]
        public Dictionary<string, string> LastVariants { get; set; }

        [JsonProperty("spawnUIFilter")]
        public string SpawnUIFilter { get; set; }

        [JsonProperty("savedUIFilter")]
        public string SavedUIFilter { get; set; }

        [JsonProperty("windowStates")]
        public Dictionary<string, object> WindowStates { get; set; }

        [JsonProperty("editorBottomSize")]
        public int EditorBottomSize { get; set; }

        [JsonProperty("gizmoActive")]
        public bool GizmoActive { get; set; }

        [JsonProperty("gizmoOnSelected")]
        public bool GizmoOnSelected { get; set; }

        [JsonProperty("outlineSelected")]
        public bool OutlineSelected { get; set; }

        [JsonProperty("outlineColor")]
        public int OutlineColor { get; set; }

        [JsonProperty("editorWidth")]
        public int EditorWidth { get; set; }

        [JsonProperty("resetSpawnPopupSearch")]
        public bool ResetSpawnPopupSearch { get; set; }

        [JsonProperty("spawnAtCursor")]
        public bool SpawnAtCursor { get; set; }

        [JsonProperty("defaultAISpotNPC")]
        public string DefaultAISpotNPC { get; set; }

        [JsonProperty("defaultAISpotSpeed")]
        public float DefaultAISpotSpeed { get; set; }

        [JsonProperty("nodeRefPrefix")]
        public string NodeRefPrefix { get; set; }

        [JsonProperty("cacheExlusions")]
        public Dictionary<string, object> CacheExlusions { get; set; }

        [JsonProperty("assetPreviewEnabled")]
        public Dictionary<string, object> AssetPreviewEnabled { get; set; }

        [JsonProperty("mainWindowName")]
        public string MainWindowName { get; set; }

        [JsonProperty("draggingThreshold")]
        public float DraggingThreshold { get; set; }

        [JsonProperty("ignoreHiddenDuringExport")]
        public bool IgnoreHiddenDuringExport { get; set; }

        [JsonProperty("filterTags")]
        public Dictionary<string, object> FilterTags { get; set; }

        [JsonProperty("favoritesFilter")]
        public string FavoritesFilter { get; set; }

        [JsonProperty("favoritesTagsAND")]
        public bool FavoritesTagsAND { get; set; }

        [JsonProperty("tabSizes")]
        public Dictionary<string, object> TabSizes { get; set; }

        public SettingsData()
        {
            SpawnPos = 1;
            SpawnDist = 4f;
            PosSteps = 0.002f;
            PrecisionMultiplier = 0.2f;
            RotSteps = 0.050f;
            DespawnOnReload = true;
            HeaderState = true;
            DeleteConfirm = true;
            MoveCloneToParent = 1;
            SpawnUIOnlyNames = false;
            Editor = new EditorSettings { Color = 1 };
            ColliderColor = 0;
            SelectedType = "Entity";
            LastVariants = new Dictionary<string, string> {
                { "Entity", "Template" },
                { "Lights", "Light" },
                { "Mesh", "Mesh" },
                { "Collision", "Collision Shape" },
                { "Deco", "Particles" },
                { "Meta", "Occluder" },
                { "Area", "Outline Marker" },
                { "AI", "AI Spot" }
            };
            SpawnUIFilter = "";
            SavedUIFilter = "";
            WindowStates = new Dictionary<string, object>();
            EditorBottomSize = 200;
            GizmoActive = true;
            GizmoOnSelected = true;
            OutlineSelected = true;
            OutlineColor = 0;
            EditorWidth = 0;
            ResetSpawnPopupSearch = true;
            SpawnAtCursor = true;
            DefaultAISpotNPC = "Character.Judy";
            DefaultAISpotSpeed = 3f;
            NodeRefPrefix = "mod";
            CacheExlusions = new Dictionary<string, object>();
            AssetPreviewEnabled = new Dictionary<string, object>();
            MainWindowName = "World Builder";
            DraggingThreshold = 5f;
            IgnoreHiddenDuringExport = false;
            FilterTags = new Dictionary<string, object>();
            FavoritesFilter = "";
            FavoritesTagsAND = false;
            TabSizes = new Dictionary<string, object>();
        }
    }
}
