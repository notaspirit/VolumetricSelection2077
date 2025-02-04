using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpDX;

public class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var json = JsonDocument.ParseValue(ref reader).RootElement;
        return new Vector3(
            json.GetProperty("X").GetSingle(),
            json.GetProperty("Y").GetSingle(),
            json.GetProperty("Z").GetSingle()
        );
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteNumber("Z", value.Z);
        writer.WriteEndObject();
    }
}

public class QuaternionConverter : JsonConverter<Quaternion>
{
    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var json = JsonDocument.ParseValue(ref reader).RootElement;
        return new Quaternion(
            json.GetProperty("X").GetSingle(),
            json.GetProperty("Y").GetSingle(),
            json.GetProperty("Z").GetSingle(),
            json.GetProperty("W").GetSingle()
        );
    }

    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteNumber("Z", value.Z);
        writer.WriteNumber("W", value.W);
        writer.WriteEndObject();
    }
}