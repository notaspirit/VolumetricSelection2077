using System.IO;
using Newtonsoft.Json;
using VolumetricSelection2077.App.Json.Helpers;
using VolumetricSelection2077.App.Models;
using VolumetricSelection2077.App.Models.WorldBuilder.Editor;

using VolumetricSelection2077.App.Services;

namespace VolumetricSelection2077.App.TestingStuff;

public class TestWheezeKitSerialization : IDebugTool
{
    private static void LogResourcePathsRecursively(Element e)
    {
        if (e is SpawnableElement se)
        {
            // Logger.Debug($"Quat rotation: {se.Spawnable.QuatRotation}, euler rotation : {se.Spawnable.EulerRotation.Pitch}, {se.Spawnable.EulerRotation.Yaw}, {se.Spawnable.EulerRotation.Roll}");
        }

        if (e.Children.Count <= 0)
            return;
        foreach (var child in e.Children)
            LogResourcePathsRecursively(child);
    }
    
    public void Run()
    {
        Logger.Debug("Starting Wheeze Kit Deserialization Test...");
        var testFilePath = @"E:\Games\Cyberpunk 2077\bin\x64\plugins\cyber_engine_tweaks\mods\entSpawner\data\objects\Huge Penthouse Mockup.json";
        var testFileContent = File.ReadAllText(testFilePath);
        
        var deserializedElement = JsonConvert.DeserializeObject<Element>(testFileContent, JsonSerializerPresets.WorldBuilder);
        
        
        LogResourcePathsRecursively(deserializedElement);
        
        Logger.Debug($"Deserialized element:\n{JsonConvert.SerializeObject(deserializedElement, JsonSerializerPresets.WorldBuilder)}");
    }
}