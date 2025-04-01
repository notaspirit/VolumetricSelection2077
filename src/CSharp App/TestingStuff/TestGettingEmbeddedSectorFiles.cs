using VolumetricSelection2077.Services;
using WolvenKit.RED4.Archive.Buffer;
using WolvenKit.RED4.Types;

namespace VolumetricSelection2077.TestingStuff;

public class TestGettingEmbeddedSectorFiles
{
    public static void Run()
    {
        var gfs = GameFileService.Instance;
        string testSector = @"base\worlds\03_night_city\_compiled\default\exterior_4_-7_0_0.streamingsector";

        var sectorCr2w = gfs.ArchiveManager.GetCR2WFile(testSector);
        if (sectorCr2w == null)
        {
            Logger.Error("Failed to load test sector " + testSector); 
            return;
        }
        

        if (sectorCr2w.RootChunk is worldStreamingSector sector)
        {
            Logger.Info("Inplace local resources:");
            foreach (var inplaceInternalResource in sector.LocalInplaceResource)
            {
                Logger.Info(inplaceInternalResource.DepotPath);

            }
            Logger.Info("Inplace external resources:");
            Logger.Info(sector.ExternInplaceResource.DepotPath);
        }

        foreach (var efile in sectorCr2w.EmbeddedFiles)
        {
            if (efile.Content is worldFoliageCompiledResource wfcr)
            {
                Logger.Success("found foliage resource");
                if (wfcr.DataBuffer.Data is FoliageBuffer fb)
                {
                    foreach (var pos in fb.Populations)
                    {
                        Logger.Info(pos.Position.ToString());
                    }
                }
            }
        }
    }
}