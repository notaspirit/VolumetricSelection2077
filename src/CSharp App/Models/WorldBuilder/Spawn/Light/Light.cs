using Newtonsoft.Json;
using VolumetricSelection2077.models.WorldBuilder.Structs;
using WolvenKit.RED4.Types;

namespace VolumetricSelection2077.Models.WorldBuilder.Spawn.Light;

public class Light : Visualized
{
    [JsonProperty("color")]
    public Color Color { get; set; }
    
    [JsonProperty("intensity")]
    public float Intensity { get; set; }
    
    [JsonProperty("innerAngle")]
    public float InnerAngle { get; set; }
    
    [JsonProperty("outerAngle")]
    public float OuterAngle { get; set; }
    
    [JsonProperty("radius")]
    public float Radius { get; set; }
    
    [JsonProperty("capsuleLength")]
    public float CapsuleLength { get; set; }
    
    [JsonProperty("autoHideDistance")]
    public float AutoHideDistance { get; set; }
    
    [JsonProperty("flickerStrength")]
    public float FlickerStrength { get; set; }
    
    [JsonProperty("flickerPeriod")]
    public float FlickerPeriod { get; set; }
    
    [JsonProperty("flickerOffset")]
    public float FlickerOffset { get; set; }
    
    [JsonProperty("lightType")]
    public Enums.ELightType LightType { get; set; }
    
    [JsonProperty("localShadows")]
    public bool LocalShadows { get; set; }

    [JsonProperty("temperature")]
    public float Temperature { get; set; }
    
    [JsonProperty("scaleVolFog")]
    public float ScaleVolFog { get; set; }
    
    [JsonProperty("useInParticles")]
    public bool UseInParticles { get; set; }
    
    [JsonProperty("useInTransparents")]
    public bool UseInTransparents { get; set; }
    
    [JsonProperty("ev")]
    public float EV { get; set; }
    
    [JsonProperty("shadowFadeDistance")]
    public float ShadowFadeDistance { get; set; }
    
    [JsonProperty("shadowFadeRange")]
    public float ShadowFadeRange { get; set; }
    
    [JsonProperty("contactShadows")]
    public Enums.rendContactShadowReciever ContactShadows { get; set; }
    
    [JsonProperty("spotCapsule")]
    public bool SpotCapsule { get; set; }
    
    [JsonProperty("softness")]
    public float Softness { get; set; }
    
    [JsonProperty("attenuation")]
    public Enums.rendLightAttenuation Attenuation { get; set; }
    
    [JsonProperty("clampAttenuation")]
    public bool ClampAttenuation { get; set; }
    
    [JsonProperty("sceneSpecularScale")]
    public float SceneSpecularScale { get; set; }
    
    [JsonProperty("sceneDiffuse")]
    public bool SceneDiffuse { get; set; }
    
    [JsonProperty("roughnessBias")]
    public float RoughnessBias { get; set; }
    
    [JsonProperty("sourceRadius")]
    public float SourceRadius { get; set; }
    
    [JsonProperty("directional")]
    public bool Directional { get; set; }

    public Light()
    {
        DataType = "Static Light";
        ModulePath = "light/light";
        NodeType = "worldStaticLightNode";
        
        Color = new Color(1, 1, 1);
        Intensity = 100;
        InnerAngle = 20;
        OuterAngle = 60;
        Radius = 15;
        CapsuleLength = 1;
        AutoHideDistance = 45;
        FlickerStrength = 0;
        FlickerPeriod = 0.2f;
        FlickerOffset = 0;
        LightType = Enums.ELightType.LT_Spot;
        LocalShadows = true;
        Temperature = -1;
        ScaleVolFog = 0;
        UseInParticles = true;
        UseInTransparents = true;
        EV = 0;
        ShadowFadeDistance = 10;
        ShadowFadeRange = 5;
        ContactShadows = Enums.rendContactShadowReciever.CSR_None;
        SpotCapsule = false;
        Softness = 2;
        Attenuation = Enums.rendLightAttenuation.LA_InverseSquare;
        ClampAttenuation = false;
        SceneSpecularScale = 100;
        SceneDiffuse = true;
        RoughnessBias = 0;
        SourceRadius = 0.05f;
        Directional = false;
    }
}
