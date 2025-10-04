using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Json.Helpers;
using VolumetricSelection2077.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.Json.Converters;

public class WorldBuilderElementListConverter : JsonConverter<List<Element>>
{
    public override List<Element>? ReadJson(JsonReader reader, Type objectType, List<Element>? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var cleanSerializer = JsonSerializerUtils.CreateChildSerializer(serializer, typeof(WorldBuilderElementListConverter));
        if (cleanSerializer.Converters.All(c => c.GetType() != typeof(WorldBuilderElementJsonConverter)))
            cleanSerializer.Converters.Add(new WorldBuilderElementJsonConverter());
        var arr = JArray.Load(reader);
        return arr.ToObject<List<Element>>(cleanSerializer);
    }
    
    public override void WriteJson(JsonWriter writer, List<Element>? value, JsonSerializer serializer)
    {
        var cleanSerializer = JsonSerializerUtils.CreateChildSerializer(serializer, typeof(WorldBuilderElementListConverter));
        if (cleanSerializer.Converters.All(c => c.GetType() != typeof(WorldBuilderElementJsonConverter)))
            cleanSerializer.Converters.Add(new WorldBuilderElementJsonConverter());
        cleanSerializer.Serialize(writer, value);
    }
}