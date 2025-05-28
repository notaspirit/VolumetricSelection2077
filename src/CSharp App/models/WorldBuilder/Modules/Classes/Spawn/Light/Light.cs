using Newtonsoft.Json;

namespace VolumetricSelection2077.models.WorldBuilder.Modules.Classes.Spawn.Light
{
    // Class for worldStaticLightNode
    public class Light : Visualized
    {
        [JsonProperty("color")]
        public object Color; // Use SharpDX.Color or a struct if needed

        [JsonProperty("intensity")]
        public float Intensity;

        [JsonProperty("innerAngle")]
        public float InnerAngle;

        [JsonProperty("outerAngle")]
        public float OuterAngle;

        [JsonProperty("radius")]
        public float Radius;

        [JsonProperty("capsuleLength")]
        public float CapsuleLength;

        [JsonProperty("autoHideDistance")]
        public float AutoHideDistance;

        [JsonProperty("flickerStrength")]
        public float FlickerStrength;

        [JsonProperty("flickerPeriod")]
        public float FlickerPeriod;

        [JsonProperty("flickerOffset")]
        public float FlickerOffset;

        [JsonProperty("lightType")]
        public int LightType;

        [JsonProperty("localShadows")]
        public bool LocalShadows;

        public Light() : base()
        {
            DataType = "Light";
            ModulePath = "light/light";
            Node = "worldStaticLightNode";
        }
    }
}
