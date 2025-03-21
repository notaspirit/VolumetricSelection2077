using System;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Formatters;
using SharpDX;

namespace VolumetricSelection2077.Converters;

public class Vector3Formatter : IMessagePackFormatter<Vector3>
{
    public void Serialize(ref MessagePackWriter writer, Vector3 value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(3);
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }

    public Vector3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var length = reader.ReadArrayHeader();
        if (length != 3)
            throw new InvalidOperationException("Invalid Vector3 array length");

        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        float z = reader.ReadSingle();

        return new Vector3(x, y, z);
    }
}

// Custom MessagePack formatter for BoundingBox
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

// Configure MessagePack to use custom formatters
public static class MessagePackConfig
{
    public static void ConfigureFormatters()
    {
        List<IMessagePackFormatter> formatters = new List<IMessagePackFormatter>();
        formatters.Add(new Vector3Formatter());
        formatters.Add(new BoundingBoxFormatter());
        formatters.Add(new QuaternionFormatter());
        formatters.Add(new PlaneFormatter());
        List<IFormatterResolver> resolvers = new List<IFormatterResolver>();
        resolvers.Add(MessagePack.Resolvers.StandardResolver.Instance);
        var customResolver = MessagePack.Resolvers.CompositeResolver.Create(
            formatters,
            resolvers
        );
        
        MessagePackSerializer.DefaultOptions = 
            MessagePackSerializerOptions.Standard.WithResolver(customResolver);
    }
}