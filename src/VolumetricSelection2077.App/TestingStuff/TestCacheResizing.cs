using VolumetricSelection2077.App.Models;
using VolumetricSelection2077.App.Services;
using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.App.TestingStuff;

public class TestCacheResizing : IDebugTool
{
    private readonly CacheService _cacheService;
    public TestCacheResizing(CacheService cacheService)
    {
        _cacheService = cacheService;
    }
    public void Run()
    {
        Logger.Info("Starting Cache Resize Test...");
        int sampleSize = 100;
        var originalSample = _cacheService.GetSample(sampleSize);

        _cacheService.ResizeEnvironment();
        
        var newSample = _cacheService.GetSample(sampleSize);
        
        Logger.Info("Checking Sample...");
        Logger.Info($"Original Count: {originalSample.Data.vanillaEntriesCount}, new Count: {newSample.Data.vanillaEntriesCount}");
        var vanillaCountStatus = (originalSample.Data.vanillaEntriesCount == newSample.Data.vanillaEntriesCount) ? "OK" : "BAD";
        Logger.Info($"Vanilla Count: {vanillaCountStatus}");
        Logger.Info($"Original Count: {originalSample.Data.moddedEntriesCount}, new Count: {newSample.Data.moddedEntriesCount}");
        var moddedCountStatus = (originalSample.Data.moddedEntriesCount == newSample.Data.moddedEntriesCount) ? "OK" : "BAD";
        Logger.Info($"Modded Count: {moddedCountStatus}");
        Logger.Info($"Original Count: {originalSample.Data.vanillaEntriesSample.Length}, new Count: {newSample.Data.vanillaEntriesSample.Length}");
        var vanillaEntryStatus = (originalSample.Data.vanillaEntriesSample == newSample.Data.vanillaEntriesSample) ? "OK" : "BAD";
        Logger.Info($"Vanilla Entries: {vanillaEntryStatus}");
        Logger.Info($"Original Count: {originalSample.Data.moddedEntriesSample.Length}, new Count: {newSample.Data.moddedEntriesSample.Length}");
        var moddedEntryStatus = (originalSample.Data.moddedEntriesSample == newSample.Data.moddedEntriesSample) ? "OK" : "BAD";
        Logger.Info($"Modded Entries: {moddedEntryStatus}");

        Logger.Info("Vanilla Entries");
        foreach (var entry in newSample.Data.vanillaEntriesSample) Logger.Info($"{entry}");
    }
}