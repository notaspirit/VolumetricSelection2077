using System;
using MessagePack;
using MessagePack.Formatters;
using SharpDX;

namespace VolumetricSelection2077.MessagePack.Formatters;

public class QuaternionFormatter : IMessagePackFormatter<Quaternion>
{
    public static readonly QuaternionFormatter Instance = new QuaternionFormatter();

    public void Serialize(ref MessagePackWriter writer, Quaternion value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(4);
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
        writer.Write(value.W);
    }

    public Quaternion Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var length = reader.ReadArrayHeader();
        if (length != 4)
            throw new InvalidOperationException("Invalid Quaternion array length");

        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        float z = reader.ReadSingle();
        float w = reader.ReadSingle();

        return new Quaternion(x, y, z, w);
    }
}