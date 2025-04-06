using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class TestCacheResizing
{
    public static void Run()
    {
        Logger.Info("Starting Cache Resize Test...");
        int sampleSize = 100;
        var cs = CacheService.Instance;
        var originalSample = cs.GetSample(sampleSize);

        cs.ResizeEnvironment();
        
        var newSample = cs.GetSample(sampleSize);
        
        Logger.Info("Checking Sample...");
        Logger.Info($"Original Count: {originalSample.vanillaEntriesCount}, new Count: {newSample.vanillaEntriesCount}");
        var vanillaCountStatus = (originalSample.vanillaEntriesCount == newSample.vanillaEntriesCount) ? "OK" : "BAD";
        Logger.Info($"Vanilla Count: {vanillaCountStatus}");
        Logger.Info($"Original Count: {originalSample.moddedEntriesCount}, new Count: {newSample.moddedEntriesCount}");
        var moddedCountStatus = (originalSample.moddedEntriesCount == newSample.moddedEntriesCount) ? "OK" : "BAD";
        Logger.Info($"Modded Count: {moddedCountStatus}");
        Logger.Info($"Original Count: {originalSample.vanillaEntriesSample.Length}, new Count: {newSample.vanillaEntriesSample.Length}");
        var vanillaEntryStatus = (originalSample.vanillaEntriesSample == newSample.vanillaEntriesSample) ? "OK" : "BAD";
        Logger.Info($"Vanilla Entries: {vanillaEntryStatus}");
        Logger.Info($"Original Count: {originalSample.moddedEntriesSample.Length}, new Count: {newSample.moddedEntriesSample.Length}");
        var moddedEntryStatus = (originalSample.moddedEntriesSample == newSample.moddedEntriesSample) ? "OK" : "BAD";
        Logger.Info($"Modded Entries: {moddedEntryStatus}");

        Logger.Info("Vanilla Entries");
        foreach (var entry in newSample.vanillaEntriesSample) Logger.Info($"{entry}");
    }
}