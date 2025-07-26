using System.Diagnostics;
using System.Threading.Tasks;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class TestSectorAABBTime : IDebugTool
{
    public async Task RunAsync()
    {
        var builder = new BoundingBoxBuilderService();
        var testSector = @"base\worlds\03_night_city\_compiled\default\exterior_-4_8_-1_2.streamingsector";
        var sw = Stopwatch.StartNew();
        await builder.ProcessStreamingsector(testSector, null);
        sw.Stop();
        Logger.Info($"Time: {sw.ElapsedMilliseconds}");
    }
    
    public void Run()
    {
        RunAsync().Wait();
    }
}