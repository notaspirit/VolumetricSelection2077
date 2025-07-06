using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Models.WorldBuilder.Spawn;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Entity;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Light;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Mesh;
using VolumetricSelection2077.models.WorldBuilder.Spawn.Visual;


namespace VolumetricSelection2077.Converters;

public class WorldBuilderSpawnableJsonConverter : JsonConverter<Spawnable>
{
    public override bool CanWrite => true;

    public override Spawnable? ReadJson(
        JsonReader reader,
        Type objectType,
        Spawnable? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);
        
        var newSerializer = new JsonSerializer();
        
        switch (obj["node"]?.Value<string>())
        {
            case "worldMeshNode":
                return obj.ToObject<Mesh>(newSerializer);
            case "worldRotatingMeshNode":
                return obj.ToObject<RotatingMesh>(newSerializer);
            case "worldClothMeshNode":
                return obj.ToObject<ClothMesh>(newSerializer);
            case "worldWaterPatchNode":
                return obj.ToObject<WaterPatch>(newSerializer);
            case "worldStaticDecalNode":
                return obj.ToObject<Decal>(newSerializer);
            case "worldStaticParticleNode":
                return obj.ToObject<Particle>(newSerializer);
            case "worldEffectNode":
                return obj.ToObject<Effect>(newSerializer);
            case "worldDynamicMeshNode":
                return obj.ToObject<DynamicMesh>(newSerializer);
            case "worldEntityNode":
                return obj.ToObject<Entity>(newSerializer);
            case "worldStaticLightNode":
                return obj.ToObject<Light>(newSerializer);
            default:
                return obj.ToObject<Spawnable>(newSerializer);
        }
    }

    public override void WriteJson(JsonWriter writer, Spawnable? value, JsonSerializer serializer)
    {
        var newSerializer = new JsonSerializer();
        
        newSerializer.Serialize(writer, value);
    }
}