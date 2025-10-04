using System;
using MessagePack;
using MessagePack.Formatters;
using SharpDX;

namespace VolumetricSelection2077.MessagePack.Formatters;

public class BoundingBoxFormatter : IMessagePackFormatter<BoundingBox>
{
    public void Serialize(ref MessagePackWriter writer, BoundingBox value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(6);
        writer.Write(value.Minimum.X);
        writer.Write(value.Minimum.Y);
        writer.Write(value.Minimum.Z);
        writer.Write(value.Maximum.X);
        writer.Write(value.Maximum.Y);
        writer.Write(value.Maximum.Z);
    }

    public BoundingBox Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var length = reader.ReadArrayHeader();
        if (length != 6)
            throw new InvalidOperationException("Invalid BoundingBox array length");

        float minX = reader.ReadSingle();
        float minY = reader.ReadSingle();
        float minZ = reader.ReadSingle();
        float maxX = reader.ReadSingle();
        float maxY = reader.ReadSingle();
        float maxZ = reader.ReadSingle();

        return new BoundingBox(
            new Vector3(minX, minY, minZ), 
            new Vector3(maxX, maxY, maxZ)
        );
    }
}