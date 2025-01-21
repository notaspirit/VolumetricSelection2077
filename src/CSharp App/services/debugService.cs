using System;
using Newtonsoft.Json.Linq;

namespace VolumetricSelection2077.Services;

public class DebugService
{
    public static void ChildValueMeshPathDebug(string jsonString)
    {
        var jObject = JObject.Parse(jsonString);
        
        var rootChunk = jObject["Data"]?["RootChunk"] as JObject;
        var nodes = rootChunk?["nodes"] as JArray;
        if (nodes == null)
        {
            Logger.Error("No nodes found");
            return;
        }
        Logger.Info($"Sector contains {nodes.Count} nodes");
        int index = 0;
        foreach (var node in nodes)
        {
            index++;
            try
            {
                string? meshPath = node["Data"]?["mesh"]?["DepotPath"]?["$value"]?.Value<string>() ?? null;
                if (meshPath != null)
                {
                    Logger.Info($"Found mesh path {meshPath} in node {index}");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to parse node {index} with exception {ex}");
            }
        }
        
    }
}