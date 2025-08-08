using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Helpers;
using VolumetricSelection2077.models.WorldBuilder.Structs;

namespace VolumetricSelection2077.Json;

public class ColorToColorArray : JsonConverter<float[]>
{
    public override bool CanWrite => true;

    public override float[]? ReadJson(
        JsonReader reader,
        Type objectType,
        float[]? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.StartObject:
                var obj = JObject.Load(reader);
                if (!obj.ContainsKey("r") || !obj.ContainsKey("g") || !obj.ContainsKey("b"))
                    throw new JsonSerializationException("Color object must contain r, g, and b properties.");
                return new float[] { obj["r"].Value<float>() / 255f, obj["g"].Value<float>() / 255f, obj["b"].Value<float>() / 255f };
            case JsonToken.StartArray:
                var CC = serializer.Converters.First(x => x == this);
                serializer.Converters.Remove(CC);
                var deserialized = serializer.Deserialize<float[]>(reader);
                serializer.Converters.Add(CC);
                return deserialized;
            default:
                throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing color.");
        }
    }

    public override void WriteJson(JsonWriter writer, float[]? value, JsonSerializer serializer)
    {
        var CC = serializer.Converters.First(x => x == this);
        serializer.Converters.Remove(CC);
        serializer.Serialize(writer, value);
        serializer.Converters.Add(CC);
    }
}