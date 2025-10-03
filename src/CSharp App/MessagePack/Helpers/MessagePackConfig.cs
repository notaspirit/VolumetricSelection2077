using System.Collections.Generic;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using VolumetricSelection2077.MessagePack.Formatters;
using Vector3Formatter = VolumetricSelection2077.MessagePack.Formatters.Vector3Formatter;
using QuaternionFormatter = VolumetricSelection2077.MessagePack.Formatters.QuaternionFormatter;

namespace VolumetricSelection2077.MessagePack.Helpers;

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
        resolvers.Add(StandardResolver.Instance);
        var customResolver = CompositeResolver.Create(
            formatters.ToArray(),
            resolvers.ToArray()
        );
        
        MessagePackSerializer.DefaultOptions = 
            MessagePackSerializerOptions.Standard.WithResolver(customResolver);
    }
}