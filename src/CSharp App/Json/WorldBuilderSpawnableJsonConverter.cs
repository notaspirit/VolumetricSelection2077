using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using VolumetricSelection2077.Helpers;
using VolumetricSelection2077.Models.WorldBuilder.Spawn;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Entity;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Light;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Mesh;
using VolumetricSelection2077.models.WorldBuilder.Spawn.Visual;
using VolumetricSelection2077.Services;


namespace VolumetricSelection2077.Json;

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
        
        var CC = serializer.Converters.First(x => x == this);
        serializer.Converters.Remove(CC);

        try
        {
            switch (obj["node"]?.Value<string>())
            {
                case "worldMeshNode":
                    return obj.ToObject<Mesh>(serializer);
                case "worldRotatingMeshNode":
                    return obj.ToObject<RotatingMesh>(serializer);
                case "worldClothMeshNode":
                    return obj.ToObject<ClothMesh>(serializer);
                case "worldWaterPatchNode":
                    return obj.ToObject<WaterPatch>(serializer);
                case "worldStaticDecalNode":
                    return obj.ToObject<Decal>(serializer);
                case "worldStaticParticleNode":
                    return obj.ToObject<Particle>(serializer);
                case "worldEffectNode":
                    return obj.ToObject<Effect>(serializer);
                case "worldDynamicMeshNode":
                    return obj.ToObject<DynamicMesh>(serializer);
                case "worldEntityNode":
                    return obj.ToObject<Entity>(serializer);
                case "worldStaticLightNode":
                    return obj.ToObject<Light>(serializer);
                default:
                    return obj.ToObject<Spawnable>(serializer);
            }
        }
        finally
        {
            serializer.Converters.Add(CC);
        }
    }

    public override void WriteJson(JsonWriter writer, Spawnable? value, JsonSerializer serializer)
    {
        var CC = serializer.Converters.First(x => x == this);
        serializer.Converters.Remove(CC);
        serializer.Serialize(writer, value);
        serializer.Converters.Add(CC);
    }
}