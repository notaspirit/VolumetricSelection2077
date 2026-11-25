using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VolumetricSelection2077.Core.Extensions;

public static class JsonExtensions
{
    public static void Populate<T>(this JToken value, T target, JsonSerializer? serializer = null) where T : class
    {
        serializer ??= JsonSerializer.CreateDefault();
        using var sr = value.CreateReader();
        serializer.Populate(sr, target);
    }
}
