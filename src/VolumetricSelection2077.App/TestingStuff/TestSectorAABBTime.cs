using System.Diagnostics;
using System.Threading.Tasks;
using VolumetricSelection2077.App.Models;
using VolumetricSelection2077.App.Services;
using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.App.TestingStuff;

public class TestSectorAABBTime : IDebugTool
{
    private readonly ISectorInfoBuildingService _boundingBoxBuildingService;
    public TestSectorAABBTime(ISectorInfoBuildingService boundingBoxBuildingService)
    {
        _boundingBoxBuildingService = boundingBoxBuildingService;
    }
    
    public async Task RunAsync()
    {
        var testSector = @"base\worlds\03_night_city\_compiled\default\exterior_-4_8_-1_2.streamingsector";
        var sw = Stopwatch.StartNew();
        await _boundingBoxBuildingService.BuildSectorInfo(testSector, null);
        sw.Stop();
        Logger.Info($"Time: {sw.ElapsedMilliseconds}");
    }
    
    public void Run()
    {
        RunAsync().Wait();
    }
}