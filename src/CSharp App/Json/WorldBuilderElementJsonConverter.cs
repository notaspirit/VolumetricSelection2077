using System;
using System.Linq;
using DynamicData.Kernel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using VolumetricSelection2077.Helpers;
using VolumetricSelection2077.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Services;


namespace VolumetricSelection2077.Json;

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
        var elementListConverter = serializer.Converters.FirstOrOptional(x => x is WorldBuilderElementListConverter);
        if (elementListConverter == null)
        {
            serializer.Converters.Add(new WorldBuilderElementListConverter());
        }
        
        var CC = serializer.Converters.First(x => x == this);
        serializer.Converters.Remove(CC);
        try
        {
            var obj = JObject.Load(reader);
            switch (obj["modulePath"]?.Value<string>())
            {
                case "modules/classes/editor/spawnableElement":
                    return obj.ToObject<SpawnableElement>(serializer);
                case "modules/classes/editor/positionable":
                    return obj.ToObject<Positionable>(serializer);
                case "modules/classes/editor/positionableGroup":
                    return obj.ToObject<PositionableGroup>(serializer);
                default:
                    return obj.ToObject<Element>(serializer);
            }
        }
        finally
        {
            serializer.Converters.Add(CC);
            if (elementListConverter == null)
            {
                serializer.Converters.Remove(serializer.Converters.First(x => x is WorldBuilderElementListConverter));
            }
        }

    }

    public override void WriteJson(JsonWriter writer, Element? value, JsonSerializer serializer)
    {
        var CC = serializer.Converters.First(x => x == this);
        serializer.Converters.Remove(CC);
        serializer.Serialize(writer, value);
        serializer.Converters.Add(CC);
    }
}