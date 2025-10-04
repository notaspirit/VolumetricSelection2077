using Newtonsoft.Json;
using VolumetricSelection2077.Json.Converters;

namespace VolumetricSelection2077.Json.Helpers;

public class JsonSerializerPresets
{
    public static JsonSerializerSettings WorldBuilder { get; } = new JsonSerializerSettings
    {
        Converters =
        { new WorldBuilderElementJsonConverter(),
            new WorldBuilderSpawnableJsonConverter(),
            new WorldBuilderElementListConverter(),
            new ColorToColorArrayConverter(),
            new NormalizeZeroConverter()
        },
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented
    };

    public static JsonSerializerSettings Default { get; } = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented
    };
}