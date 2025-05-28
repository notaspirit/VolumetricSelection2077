using System.Collections.Generic;
using Newtonsoft.Json;
using SharpDX;
using VolumetricSelection2077.Models.WorldBuilder.Modules.Classes.Generated;

namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn
{
    // Base class for any object/node that can be spawned
    public class Spawnable
    {
        [JsonProperty("dataType")]
        public string DataType;
        
        [JsonProperty("spawnListType")]
        public string SpawnListType;
        
        [JsonProperty("spawnListPath")]
        public string SpawnListPath;
        
        [JsonProperty("modulePath")]
        public string ModulePath;
        
        [JsonProperty("spawnData")]
        public string SpawnData;
        
        [JsonProperty("app")]
        public string App;
        
        [JsonProperty("position")]
        public Vector4 Position;
        
        [JsonProperty("rotation")]
        public EulerAngles Rotation; // Use a generated struct/class for EulerAngles
        
        [JsonProperty("entityID")]
        public object EntityID;
        
        [JsonProperty("entity")]
        public object Entity;
        
        [JsonProperty("spawned")]
        public bool Spawned;
        
        [JsonProperty("spawning")]
        public bool Spawning;
        
        [JsonProperty("despawning")]
        public bool Despawning;
        
        [JsonProperty("queueRespawn")]
        public bool QueueRespawn;
        
        [JsonProperty("primaryRange")]
        public float PrimaryRange;
        
        [JsonProperty("secondaryRange")]
        public float SecondaryRange;
        
        [JsonProperty("uk10")]
        public int Uk10;
        
        [JsonProperty("uk11")]
        public int Uk11;
        
        [JsonProperty("streamingMultiplier")]
        public float StreamingMultiplier;
        
        [JsonProperty("isHovered")]
        public bool IsHovered;
        
        [JsonProperty("arrowDirection")]
        public string ArrowDirection;
        
        [JsonProperty("object")]
        public object Object; // Should be an Element
        
        [JsonProperty("node")]
        public string Node;
        
        [JsonProperty("description")]
        public string Description;
        
        [JsonProperty("previewNote")]
        public string PreviewNote;
        
        [JsonProperty("icon")]
        public string Icon;
        
        [JsonProperty("rotationRelative")]
        public bool RotationRelative;
        
        [JsonProperty("outline")]
        public int Outline;
        
        [JsonProperty("spawnedAndCachedCallback")]
        public List<object> SpawnedAndCachedCallback;
        
        [JsonProperty("nodeRef")]
        public string NodeRef;
        
        [JsonProperty("noExport")]
        public bool NoExport;
        
        [JsonProperty("worldNodePropertyWidth")]
        public float WorldNodePropertyWidth;
        
        [JsonProperty("assetPreviewType")]
        public string AssetPreviewType;
        
        [JsonProperty("assetPreviewDelay")]
        public float AssetPreviewDelay;
        
        [JsonProperty("isAssetPreview")]
        public bool IsAssetPreview;
        
        [JsonProperty("assetPreviewLensDistortion")]
        public bool AssetPreviewLensDistortion;
        
        // added manually, might not be the correct location for it
        [JsonProperty("spawnDataPath")]
        public string SpawnDataPath;
        
        public Spawnable()
        {
            DataType = "Spawnable";
            SpawnListType = "list";
            SpawnListPath = "data/spawnables/entity/templates/";
            ModulePath = "spawnable";
            Node = "worldEntityNode";
            Description = string.Empty;
            PreviewNote = "---";
            Icon = string.Empty;
            SpawnData = "base\\spawner\\empty_entity.ent";
            App = "default";
            Position = new Vector4(0, 0, 0, 0);
            Rotation = new EulerAngles(0, 0, 0);
            EntityID = null;
            Entity = null;
            Spawned = false;
            Spawning = false;
            Despawning = false;
            QueueRespawn = false;
            SpawnedAndCachedCallback = new List<object>();
            WorldNodePropertyWidth = 0;
            NoExport = false;
            PrimaryRange = 120;
            SecondaryRange = 100;
            Uk10 = 1024;
            Uk11 = 512;
            StreamingMultiplier = 1;
            NodeRef = string.Empty;
            IsHovered = false;
            ArrowDirection = "none";
            RotationRelative = false;
            Outline = 0;
            AssetPreviewType = "none";
            AssetPreviewDelay = 0.2f;
            IsAssetPreview = false;
            AssetPreviewLensDistortion = false;
            Object = null;
        }
    }
}
