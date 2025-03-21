using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VolumetricSelection2077.Services;
using WolvenKit.RED4.Archive;
using WolvenKit.RED4.Types;

namespace VolumetricSelection2077.TestingStuff;

public class TestMaxCacheSize
{
    private static async void ProcessMesh(string? path, GameFileService gfs)
    {
        if (path != null && path.ToLower().EndsWith("mesh"))
        {
            var aMesh = gfs.GetCMesh(path);
            //if (aMesh == null)
                // Logger.Warning($"Failed to get CMesh {path}!");
        }
    }
    
    private static async void ProcessSector(string path, GameFileService gfs)
    {
        // Logger.Info($"Processing sector: {path}");
        var aSec = gfs.GetSector(path);
        if (aSec == null)
        {
            Logger.Error($"Failed to get sector {path}!");
            return;
        }
        List<Task> nodeTasks = new List<Task>();
        foreach (var node in aSec.Nodes)
        {
            nodeTasks.Add(Task.Run(() => ProcessMesh(node.ResourcePath, gfs)));
        }
        await Task.WhenAll(nodeTasks);
    }
    public static async Task Run()
    {
        Logger.Info("Getting streamingblocks...");
        var gfs = GameFileService.Instance;
        var am = gfs._archiveManager;
        string baseBlockPath = @"base\worlds\03_night_city\_compiled\default\blocks\all.streamingblock";
        string ep1BlockPath = @"base\worlds\03_night_city\_compiled\default\ep1\blocks\all.streamingblock";

        var baseBlockFile = am.GetCR2WFile(baseBlockPath);
        if (baseBlockFile == null)
        {  
            Logger.Error("Failed to get baseBlockFile");
            return;
        }
        var baseBlock = baseBlockFile.RootChunk as worldStreamingBlock;
        if (baseBlock == null)
        {  
            Logger.Error("Failed to get ep1Block");
            return;
        }
        
        var ep1BlockFile = am.GetCR2WFile(ep1BlockPath);
        if (ep1BlockFile == null)
        {  
            Logger.Error("Failed to get ep1BlockFile");
            return;
        }
        var ep1Block = ep1BlockFile.RootChunk as worldStreamingBlock;
        if (ep1Block == null)
        {  
            Logger.Error("Failed to get ep1Block");
            return;
        }
        CacheService.Instance.StartListening();
        
        Logger.Info("Starting sector processing...");
        List<Task> sectorTasks = new List<Task>();
        foreach (var sectorDescriptor in baseBlock.Descriptors)
        {
            string? path = sectorDescriptor.Data.DepotPath;
            if (path == null)
                continue;
            
            sectorTasks.Add(Task.Run(() => ProcessSector(path, gfs)));
        }
        
        foreach (var sectorDescriptor in ep1Block.Descriptors)
        {
            string? path = sectorDescriptor.Data.DepotPath;
            if (path == null)
                continue;
            
            sectorTasks.Add(Task.Run(() => ProcessSector(path, gfs)));
        }
        
        await Task.WhenAll(sectorTasks);
        CacheService.Instance.StopListening();
        Logger.Success("Finished sector processing");
    }
}