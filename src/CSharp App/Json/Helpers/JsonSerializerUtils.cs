using System;
using System.Linq;
using Newtonsoft.Json;

namespace VolumetricSelection2077.Json.Helpers;

public class JsonSerializerUtils
{
    /// <summary>
    /// Creates a child serializer that excludes the given converters
    /// </summary>
    /// <param name="parent">serializer to copy settings from</param>
    /// <param name="exclude">converters to exclude</param>
    /// <returns></returns>
    public static JsonSerializer CreateChildSerializer(JsonSerializer parent, params Type[] exclude)
    {
        var child = new JsonSerializer
        {
            Culture = parent.Culture,
            ContractResolver = parent.ContractResolver,
            CheckAdditionalContent = parent.CheckAdditionalContent,
            ConstructorHandling = parent.ConstructorHandling,
            Context = parent.Context,
            DateFormatHandling = parent.DateFormatHandling,
            DateFormatString = parent.DateFormatString,
            DateParseHandling = parent.DateParseHandling,
            DateTimeZoneHandling = parent.DateTimeZoneHandling,
            DefaultValueHandling = parent.DefaultValueHandling,
            EqualityComparer = parent.EqualityComparer,
            FloatFormatHandling = parent.FloatFormatHandling,
            FloatParseHandling = parent.FloatParseHandling,
            Formatting = parent.Formatting,
            MaxDepth = parent.MaxDepth,
            MetadataPropertyHandling = parent.MetadataPropertyHandling,
            MissingMemberHandling = parent.MissingMemberHandling,
            NullValueHandling = parent.NullValueHandling,
            ObjectCreationHandling = parent.ObjectCreationHandling,
            PreserveReferencesHandling = parent.PreserveReferencesHandling,
            ReferenceLoopHandling = parent.ReferenceLoopHandling,
            StringEscapeHandling = parent.StringEscapeHandling,
            TypeNameAssemblyFormatHandling = parent.TypeNameAssemblyFormatHandling,
            TypeNameHandling = parent.TypeNameHandling
        };

        foreach (var c in parent.Converters)
            if (!exclude.Contains(c.GetType()))
                child.Converters.Add(c);

        return child;
    }
}