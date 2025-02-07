using VolumetricSelection2077.Parsers;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class testCollBoxByExportToBlender
{
    public static void Run(GameFileService gfs)
    {
        string sectorPath = @"base\worlds\03_night_city\_compiled\default\exterior_-10_-4_0_1.streamingsector";
        var (secSucc, secError, secString) = gfs.GetGameFileAsJsonString(sectorPath);
        if (!secSucc || secError != "" || secString != "")
        {
            Logger.Warning("Failed to get test sector!");
            return;
        }
        
        var parsedSector = AbbrSectorParser.Deserialize(secString);
    }
}