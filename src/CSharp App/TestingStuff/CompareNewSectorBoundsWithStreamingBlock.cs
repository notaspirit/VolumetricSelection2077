using System.Linq;
using MessagePack;
using SharpDX;
using VolumetricSelection2077.Converters.Simple;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;
using WolvenKit.RED4.Types;
using WEnums = WolvenKit.RED4.Types.Enums;
using Vector3 = SharpDX.Vector3;

namespace VolumetricSelection2077.TestingStuff;

public class CompareNewSectorBoundsWithStreamingBlock : IDebugTool
{
    public void Run()
    {
        var gfs = GameFileService.Instance;
        var cs = CacheService.Instance;
        Logger.Info("Getting basegame streamingblock...");
        if (cs.IsInitialized == false)
        {
            Logger.Error("Cache service is not initialized!");
            return;
        }
        string baseGameStreamingBlock = @"base\worlds\03_night_city\_compiled\default\blocks\all.streamingblock";
        var streamingblock = gfs?.ArchiveManager?.GetCR2WFile(baseGameStreamingBlock);
        if (streamingblock == null)
        {
            Logger.Error("Failed to load streaming block");
            return;
        }

        Logger.Info("Reading streaming block...");
        var streamingBlockRoot = streamingblock.RootChunk as worldStreamingBlock;
        if (streamingBlockRoot == null)
        {
            Logger.Error("Failed to read streaming block");
            return;
        }
        
        var exteriorSectors = streamingBlockRoot.Descriptors.Where(x => x.Category == WEnums.worldStreamingSectorCategory.Exterior).ToList();
        var interiorSectors = streamingBlockRoot.Descriptors.Where(x => x.Category == WEnums.worldStreamingSectorCategory.Interior).ToList();
        
        Logger.Info($"Found {exteriorSectors.Count} exterior sectors and {interiorSectors.Count} interior sectors. in base streaming block.");
        
        var exteriorSizeDifference = Vector3.Zero;
        var interiorSizeDifference = Vector3.Zero;
        
        var minExternalSizeDifference = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var minInteriorSizeDifference = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        
        
        var exteriorSectorsVanilla = cs.GetAllEntries(CacheDatabases.VanillaBounds);
        
        foreach (var sector in exteriorSectors)
        {
            var customBoundsRaw = exteriorSectorsVanilla.FirstOrDefault(x => x.Key == sector.Data.DepotPath).Value;
            if (customBoundsRaw == null)
            {
                Logger.Error($"Failed to get vanilla bounds for {sector.Data.DepotPath}");
                continue;
            }
            var customBoundsBB = MessagePackSerializer.Deserialize<BoundingBox>(customBoundsRaw);
            if (customBoundsBB == null)
            {
                Logger.Error($"Failed to deserialize vanilla bounds for {sector.Data.DepotPath}");
                continue;
            }
            
            var customSize = customBoundsBB.Size;
            var blockSize = new BoundingBox(WolvenkitToSharpDXConverter.Vector3(sector.StreamingBox.Min), WolvenkitToSharpDXConverter.Vector3(sector.StreamingBox.Max)).Size;
            var difference = blockSize - customSize;
            var differencePercent = difference / customSize;
            Logger.Info($"Custom bounds for {sector.Data.DepotPath} differ by {difference} with {differencePercent * 100}% of the block size. ({customSize} vs {blockSize})");
            exteriorSizeDifference += differencePercent;
            minExternalSizeDifference = Vector3.Min(minExternalSizeDifference, differencePercent);
        }
        
        var interiorSectorsVanilla = cs.GetAllEntries(CacheDatabases.VanillaBounds);
        
        foreach (var sector in interiorSectors)
        {
            var customBoundsRaw = interiorSectorsVanilla.FirstOrDefault(x => x.Key == sector.Data.DepotPath).Value;
            if (customBoundsRaw == null)
            {
                Logger.Error($"Failed to get vanilla bounds for {sector.Data.DepotPath}");
                continue;
            }
            var customBoundsBB = MessagePackSerializer.Deserialize<BoundingBox>(customBoundsRaw);
            if (customBoundsBB == null)
            {
                Logger.Error($"Failed to deserialize vanilla bounds for {sector.Data.DepotPath}");
                continue;
            }
            
            var customSize = customBoundsBB.Size;
            var blockSize = new BoundingBox(WolvenkitToSharpDXConverter.Vector3(sector.StreamingBox.Min), WolvenkitToSharpDXConverter.Vector3(sector.StreamingBox.Max)).Size;
            var difference =  blockSize - customSize;
            var differencePercent =   difference / customSize;
            Logger.Info($"Custom bounds for {sector.Data.DepotPath} differ by {difference} with {differencePercent * 100}% of the block size. ({customSize} vs {blockSize})");
            interiorSizeDifference += differencePercent;
            minInteriorSizeDifference = Vector3.Min(minInteriorSizeDifference, differencePercent);
        }
        
        Logger.Info($"Average exterior size difference: {exteriorSizeDifference / exteriorSectors.Count}");
        Logger.Info($"Average interior size difference: {interiorSizeDifference / interiorSectors.Count}");
        
        Logger.Info($"Minimum exterior size difference: {minExternalSizeDifference}");
        Logger.Info($"Minimum interior size difference: {minInteriorSizeDifference}");
    }
}