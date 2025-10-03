using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Json.Helpers;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.Json.Converters;

public class DictStringJObjectConverter : JsonConverter<Dictionary<string, JObject>>
{
    public override Dictionary<string, JObject>? ReadJson(JsonReader reader, Type objectType, Dictionary<string, JObject>? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var cleanSerializer = JsonSerializerUtils.CreateChildSerializer(serializer, typeof(DictStringJObjectConverter));
        switch (reader.TokenType)
        {
            case JsonToken.StartArray:
                var arr = JArray.Load(reader);
                if (arr.Count == 0)
                    return new Dictionary<string, JObject>();
                throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing dictionary. Expected {JsonToken.StartObject} (or {JsonToken.StartArray} if empty), got {JsonToken.StartArray} with values.");
            case JsonToken.StartObject:
                var obj = JObject.Load(reader);
                return obj.ToObject<Dictionary<string, JObject>>(cleanSerializer);
            case JsonToken.Null:
                return null;
            default:
                throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing dictionary.");
        }
    }
    
    public override void WriteJson(JsonWriter writer, Dictionary<string, JObject>? value, JsonSerializer serializer)
    {
        var cleanSerializer = JsonSerializerUtils.CreateChildSerializer(serializer, typeof(DictStringJObjectConverter));
        cleanSerializer.Serialize(writer, value);
    }
}