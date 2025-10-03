using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Json.Helpers;
using VolumetricSelection2077.Models.WorldBuilder.Spawn;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Entity;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Light;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Mesh;
using VolumetricSelection2077.models.WorldBuilder.Spawn.Visual;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.Json.Converters;

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
        var cleanSerializer = JsonSerializerUtils.CreateChildSerializer(serializer, typeof(WorldBuilderSpawnableJsonConverter));
        
        var obj = JObject.Load(reader);
        switch (obj["node"]?.Value<string>())
        {
            case "worldMeshNode":
                return obj.ToObject<Mesh>(cleanSerializer);
            case "worldRotatingMeshNode":
                return obj.ToObject<RotatingMesh>(cleanSerializer);
            case "worldClothMeshNode":
                return obj.ToObject<ClothMesh>(cleanSerializer);
            case "worldWaterPatchNode":
                return obj.ToObject<WaterPatch>(cleanSerializer);
            case "worldStaticDecalNode":
                return obj.ToObject<Decal>(cleanSerializer);
            case "worldStaticParticleNode":
                return obj.ToObject<Particle>(cleanSerializer);
            case "worldEffectNode":
                return obj.ToObject<Effect>(cleanSerializer);
            case "worldDynamicMeshNode":
                return obj.ToObject<DynamicMesh>(cleanSerializer);
            case "worldEntityNode":
                if (cleanSerializer.Converters.All(c => c.GetType() != typeof(DictStringJObjectConverter)))
                    cleanSerializer.Converters.Add(new DictStringJObjectConverter());
                return obj.ToObject<Entity>(cleanSerializer);
            case "worldStaticLightNode":
                return obj.ToObject<Light>(cleanSerializer);
            default:
                return obj.ToObject<Spawnable>(cleanSerializer);
        }

    }

    public override void WriteJson(JsonWriter writer, Spawnable? value, JsonSerializer serializer)
    {
        var cleanSerializer = JsonSerializerUtils.CreateChildSerializer(serializer, typeof(WorldBuilderSpawnableJsonConverter));
        cleanSerializer.Serialize(writer, value);
    }
}