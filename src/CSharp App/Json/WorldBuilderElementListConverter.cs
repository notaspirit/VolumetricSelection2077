using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData.Kernel;
using Newtonsoft.Json;
using VolumetricSelection2077.Models.WorldBuilder.Editor;

namespace VolumetricSelection2077.Json;

public class WorldBuilderElementListConverter : JsonConverter<List<Element>>
{
    public override List<Element>? ReadJson(JsonReader reader, Type objectType, List<Element>? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var currentConverter = serializer.Converters.First(x => x == this);
        serializer.Converters.Remove(currentConverter);
        var elementConverter = serializer.Converters.FirstOrOptional(x => x is WorldBuilderElementJsonConverter);
        if (elementConverter == null)
        {
            serializer.Converters.Add(new WorldBuilderElementJsonConverter());
        }
        var deserialized = serializer.Deserialize<List<Element>>(reader);
        serializer.Converters.Add(currentConverter);
        if (elementConverter == null)
        {
            serializer.Converters.Remove(serializer.Converters.First(x => x is WorldBuilderElementJsonConverter));
        }
        return deserialized;
    }
    
    public override void WriteJson(JsonWriter writer, List<Element>? value, JsonSerializer serializer)
    {
        var currentConverter = serializer.Converters.First(x => x == this);
        serializer.Converters.Remove(currentConverter);
        serializer.Serialize(writer, value);
        serializer.Converters.Add(currentConverter);
    }
}