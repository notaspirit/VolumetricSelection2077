using Newtonsoft.Json;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VolumetricSelection2077.App.Services;

public abstract class AppUtils : Utils
{
    public static AxlRemovalFile? TryParseAxlRemovalFile(string input)
    {
        try
        {
            return JsonConvert.DeserializeObject<AxlRemovalFile>(input);
        }
        catch (JsonException) { }
            
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            return deserializer.Deserialize<AxlRemovalFile>(input);
        }
        catch (YamlDotNet.Core.YamlException) { }
            
        return null;
    }
}