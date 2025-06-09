using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Models.WorldBuilder.Spawn;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Mesh;


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
        
        if (obj["node"] != null && obj["node"]?.Value<string>() == "worldMeshNode")
            return obj.ToObject<Mesh>(newSerializer);

        return obj.ToObject<Spawnable>(newSerializer);
    }

    public override void WriteJson(JsonWriter writer, Spawnable? value, JsonSerializer serializer)
    {
        var newSerializer = new JsonSerializer();
        
        newSerializer.Serialize(writer, value);
    }
}