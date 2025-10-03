using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Json.Helpers;
using VolumetricSelection2077.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.Json.Converters;

public class WorldBuilderElementJsonConverter : JsonConverter<Element>
{
    public override bool CanWrite => true;

    public override Element? ReadJson(
        JsonReader reader,
        Type objectType,
        Element? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var cleanSerializer = JsonSerializerUtils.CreateChildSerializer(serializer, typeof(WorldBuilderElementJsonConverter));
        if (cleanSerializer.Converters.All(c => c.GetType() != typeof(WorldBuilderElementListConverter)))
            cleanSerializer.Converters.Add(new WorldBuilderElementListConverter());
        
        var obj = JObject.Load(reader);
        switch (obj["modulePath"]?.Value<string>())
        {
            case "modules/classes/editor/spawnableElement":
                return obj.ToObject<SpawnableElement>(cleanSerializer);
            case "modules/classes/editor/positionable":
                return obj.ToObject<Positionable>(cleanSerializer);
            case "modules/classes/editor/positionableGroup":
                return obj.ToObject<PositionableGroup>(cleanSerializer);
            default:
                return obj.ToObject<Element>(cleanSerializer);
        }
    }

    public override void WriteJson(JsonWriter writer, Element? value, JsonSerializer serializer)
    {
        var cleanSerializer = JsonSerializerUtils.CreateChildSerializer(serializer, typeof(WorldBuilderElementJsonConverter));
        cleanSerializer.Serialize(writer, value);
    }
}