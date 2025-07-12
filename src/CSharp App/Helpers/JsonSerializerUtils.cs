using System;
using Newtonsoft.Json;

namespace VolumetricSelection2077.Helpers;

public class JsonSerializerUtils
{
    public static JsonSerializer CloneWithoutConverters(JsonSerializer original)
    {
        ArgumentNullException.ThrowIfNull(original);

        return new JsonSerializer
        {
            Formatting = original.Formatting,
            Culture = original.Culture,
            CheckAdditionalContent = original.CheckAdditionalContent,
            ConstructorHandling = original.ConstructorHandling,
            Context = original.Context,
            ContractResolver = original.ContractResolver,
            DateFormatHandling = original.DateFormatHandling,
            DateFormatString = original.DateFormatString,
            DateParseHandling = original.DateParseHandling,
            DateTimeZoneHandling = original.DateTimeZoneHandling,
            DefaultValueHandling = original.DefaultValueHandling,
            EqualityComparer = original.EqualityComparer,
            FloatFormatHandling = original.FloatFormatHandling,
            FloatParseHandling = original.FloatParseHandling,
            MaxDepth = original.MaxDepth,
            MetadataPropertyHandling = original.MetadataPropertyHandling,
            MissingMemberHandling = original.MissingMemberHandling,
            NullValueHandling = original.NullValueHandling,
            ObjectCreationHandling = original.ObjectCreationHandling,
            PreserveReferencesHandling = original.PreserveReferencesHandling,
            ReferenceLoopHandling = original.ReferenceLoopHandling,
            StringEscapeHandling = original.StringEscapeHandling,
            TraceWriter = original.TraceWriter,
            TypeNameAssemblyFormatHandling = original.TypeNameAssemblyFormatHandling,
            TypeNameHandling = original.TypeNameHandling
        };
    }
}