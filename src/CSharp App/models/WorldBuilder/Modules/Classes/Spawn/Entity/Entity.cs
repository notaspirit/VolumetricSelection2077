using System.Collections.Generic;
using Newtonsoft.Json;
using SharpDX;
using VolumetricSelection2077.Models.WorldBuilder.Modules.Classes.Generated;

namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Entity
{
    // Base entity handling
    public class Entity : Spawnable
    {
        [JsonProperty("apps")]
        public List<string> Apps;
        
        [JsonProperty("appsLoaded")]
        public bool AppsLoaded;
        
        [JsonProperty("appIndex")]
        public int AppIndex;
        
        [JsonProperty("bBoxCallback")]
        public object BBoxCallback;
        
        [JsonProperty("bBox")]
        public BBoxStruct BBox;
        
        [JsonProperty("bBoxLoaded")]
        public bool BBoxLoaded;
        
        [JsonProperty("meshes")]
        public List<object> Meshes;
        
        [JsonProperty("instanceDataChanges")]
        public List<object> InstanceDataChanges;
        
        [JsonProperty("defaultComponentData")]
        public List<object> DefaultComponentData;
        
        [JsonProperty("typeInfo")]
        public List<object> TypeInfo;
        
        [JsonProperty("enumInfo")]
        public List<object> EnumInfo;
        
        [JsonProperty("deviceClassName")]
        public string DeviceClassName;
        
        [JsonProperty("propertiesMaxWidth")]
        public float? PropertiesMaxWidth;
        
        [JsonProperty("instanceDataSearch")]
        public string InstanceDataSearch;
        
        [JsonProperty("psControllerID")]
        public string PsControllerID;
        
        [JsonProperty("assetPreviewType")]
        public string AssetPreviewType;
        
        [JsonProperty("assetPreviewDelay")]
        public float AssetPreviewDelay;
        
        [JsonProperty("assetPreviewBackplane")]
        public object AssetPreviewBackplane;
        
        [JsonProperty("assetPreviewIsCharacter")]
        public bool AssetPreviewIsCharacter;
        
        [JsonProperty("uk10")]
        public int Uk10;
        
        public Entity() : base()
        {
            Apps = new List<string>();
            AppsLoaded = false;
            AppIndex = 0;
            BBox = new BBoxStruct { Min = new Vector4(-0.5f, -0.5f, -0.5f, 0), Max = new Vector4(0.5f, 0.5f, 0.5f, 0) };
            BBoxLoaded = false;
            Meshes = new List<object>();
            InstanceDataChanges = new List<object>();
            DefaultComponentData = new List<object>();
            TypeInfo = new List<object>();
            EnumInfo = new List<object>();
            DeviceClassName = string.Empty;
            PropertiesMaxWidth = null;
            InstanceDataSearch = string.Empty;
            PsControllerID = string.Empty;
            AssetPreviewType = "backdrop";
            AssetPreviewDelay = 0.15f;
            AssetPreviewBackplane = null;
            AssetPreviewIsCharacter = false;
            Uk10 = 1056;
        }
    }
}
