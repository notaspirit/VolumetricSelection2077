using System;
using MessagePack;
using MessagePack.Formatters;
using SharpDX;

namespace VolumetricSelection2077.MessagePack.Formatters;

public class PlaneFormatter : IMessagePackFormatter<Plane>
{
    public static readonly PlaneFormatter Instance = new PlaneFormatter();
    
    public void Serialize(ref MessagePackWriter writer, Plane value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(4);
        writer.Write(value.Normal.X);
        writer.Write(value.Normal.Y);
        writer.Write(value.Normal.Z);
        writer.Write(value.D);
    }

    public Plane Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var length = reader.ReadArrayHeader();
        if (length != 4)
            throw new InvalidOperationException("Invalid Plane array length");

        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        float z = reader.ReadSingle();
        float d = reader.ReadSingle();

        return new Plane(x, y, z, d);
    }
}