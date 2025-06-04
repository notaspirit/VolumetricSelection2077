using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class TestRecalulateNbNodesUnderProxy
{
    public static void Run()
    {
        var existingFilePath = @"E:\Games\Cyberpunk 2077\archive\pc\mod\VS2077\TestingProxyResolution+4.xl";
        var file = File.ReadAllText(existingFilePath);
        var axlFile = UtilService.TryParseAxlRemovalFile(file);
        
        var sectors = new Dictionary<string, AxlSector>();
        foreach (var sector in axlFile.Streaming.Sectors)
        {
            sectors.Add(sector.Path, sector);
        }
        
        new MergingService().RecalculateNbNodesUnderProxy(sectors);
        
        var testFilePath = @"E:\Games\Cyberpunk 2077\archive\pc\mod\VS2077\TestingProxyResolution+4-recalced.xl";
        File.WriteAllText(testFilePath,JsonConvert.SerializeObject(axlFile,
            new JsonSerializerSettings
                { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented }));
    }
}