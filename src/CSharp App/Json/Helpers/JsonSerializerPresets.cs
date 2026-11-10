using Newtonsoft.Json;
using VolumetricSelection2077.App.Json.Converters;

namespace VolumetricSelection2077.App.Json.Helpers;

public static class JsonSerializerPresets
{
    public static JsonSerializerSettings WorldBuilder { get; } = new JsonSerializerSettings
    {
        Converters =
        { new WorldBuilderElementJsonConverter(),
            new WorldBuilderSpawnableJsonConverter(),
            new WorldBuilderElementListConverter(),
            new ColorToColorArrayConverter(),
            new NormalizeZeroConverter(),
            new DictStringJObjectConverter(),
            new JObjectArrConverter()
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