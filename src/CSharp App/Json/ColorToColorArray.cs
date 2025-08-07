using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Helpers;
using VolumetricSelection2077.models.WorldBuilder.Structs;

namespace VolumetricSelection2077.Json;

public class ColorToColorArray : JsonConverter<Color>
{
    public override bool CanWrite => true;

    public override Color? ReadJson(
        JsonReader reader,
        Type objectType,
        Color? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.StartObject:
                var obj = JObject.Load(reader);
                var colorConverter = serializer.Converters.First(x => x == this);
                serializer.Converters.Remove(colorConverter);
                var serialized =  obj.ToObject<Color>(serializer);
                serializer.Converters.Add(colorConverter);
                return serialized;
            case JsonToken.StartArray:
                var array = JArray.Load(reader);
                var r = (ushort)((float)array[0] * 255);
                var g = (ushort)((float)array[1] * 255);
                var b = (ushort)((float)array[2] * 255);
                return new Color(r, g, b);
            default:
                throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing color.");
        }
    }

    public override void WriteJson(JsonWriter writer, Color? value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value?.r / 255f);
        writer.WriteValue(value?.g / 255f);
        writer.WriteValue(value?.b / 255f);
        writer.WriteEndArray();
    }
}