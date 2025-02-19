using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using VolumetricSelection2077.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VolumetricSelection2077.Services
{
    public class UtilService
    {
        public static string BuildORRegex(List<string> patterns)
        {
            return string.Join("|", patterns);
        }
        public static string EscapeSlashes(string input)
        {
            return input.Replace("\\", "\\\\");
        }
        
        public static string FormatElapsedTime(TimeSpan elapsed)
        {
            var parts = new List<string>();
        
            if (elapsed.Hours > 0)
            {
                parts.Add($"{elapsed.Hours} hour{(elapsed.Hours == 1 ? "" : "s")}");
            }
            if (elapsed.Minutes > 0)
            {
                parts.Add($"{elapsed.Minutes} minute{(elapsed.Minutes == 1 ? "" : "s")}");
            }
            if (elapsed.Seconds > 0 || parts.Count == 0)
            {
                parts.Add($"{elapsed.Seconds}.{elapsed.Milliseconds:D3} seconds");
            }
        
            return string.Join(", ", parts);
        }

        public static AxlRemovalFile? TryParseAxlRemovalFile(String input)
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
}