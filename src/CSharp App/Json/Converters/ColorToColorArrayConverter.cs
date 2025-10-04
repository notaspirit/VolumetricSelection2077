using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Json.Helpers;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.Json.Converters;

public class ColorToColorArrayConverter : JsonConverter<float[]>
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
                var cleanSerializer = JsonSerializerUtils.CreateChildSerializer(serializer, typeof(ColorToColorArrayConverter));
                var arr = JArray.Load(reader);
                return arr.ToObject<float[]>(cleanSerializer);
            default:
                throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing color.");
        }
    }

    public override void WriteJson(JsonWriter writer, float[]? value, JsonSerializer serializer)
    {
        var cleanSerializer = JsonSerializerUtils.CreateChildSerializer(serializer, typeof(ColorToColorArrayConverter));
        cleanSerializer.Serialize(writer, value);
    }
}