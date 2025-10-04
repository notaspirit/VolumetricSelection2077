using System;
using Newtonsoft.Json;

namespace VolumetricSelection2077.Json.Converters;

public class NormalizeZeroConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(double) || objectType == typeof(float);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer)
        {
            double value = Convert.ToDouble(reader.Value);
            return NormalizeZero(value, objectType);
        }

        if (reader.TokenType == JsonToken.Null && Nullable.GetUnderlyingType(objectType) != null)
            return null;

        throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing {objectType}");
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is double d)
        {
            writer.WriteValue(NormalizeZero(d, typeof(double)));
        }
        else if (value is float f)
        {
            writer.WriteValue(NormalizeZero(f, typeof(float)));
        }
        else
        {
            throw new JsonSerializationException($"Unexpected value type {value.GetType()}");
        }
    }

    private static object NormalizeZero(double value, Type targetType)
    {
        double normalized = value == 0.0 ? 0.0 : value; 

        if (targetType == typeof(float))
            return (float)normalized;

        return normalized;
    }
}