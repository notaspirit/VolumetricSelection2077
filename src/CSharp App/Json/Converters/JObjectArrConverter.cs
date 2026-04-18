using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Json.Helpers;

namespace VolumetricSelection2077.Json.Converters;

public class JObjectArrConverter : JsonConverter<JObject>
{
    public override JObject? ReadJson(JsonReader reader, Type objectType, JObject? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var cleanSerializer = JsonSerializerUtils.CreateChildSerializer(serializer, typeof(JObjectArrConverter));
        switch (reader.TokenType)
        {
            case JsonToken.StartArray:
                var arr = JArray.Load(reader);
                if (arr.Count == 0)
                    return new JObject();
                throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing JObject. Expected {JsonToken.StartObject} (or {JsonToken.StartArray} if empty), got {JsonToken.StartArray} with values.");
            case JsonToken.StartObject:
                var obj = JObject.Load(reader);
                return obj.ToObject<JObject>(cleanSerializer);
            case JsonToken.Null:
                return null;
            default:
                throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing JObject.");
        }
    }
    
    public override void WriteJson(JsonWriter writer, JObject? value, JsonSerializer serializer)
    {
        var cleanSerializer = JsonSerializerUtils.CreateChildSerializer(serializer, typeof(JObjectArrConverter));
        cleanSerializer.Serialize(writer, value);
    }
}