using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Resources;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class GetSectorStats
{
    private static Dictionary<NodeTypeProcessingOptions.Enum, ulong> ProcessSector(string sectorPath, GameFileService gfs, Progress progress)
    {
        var result = new Dictionary<NodeTypeProcessingOptions.Enum, ulong>();
        var sector = gfs.GetSector(sectorPath);
        if (sector == null)
        {
            Logger.Error($"Failed to load sector {sectorPath}");
            progress.AddCurrent(1, Progress.ProgressSections.Processing);
            return result;
        }

        // foreach (var node in sector.Nodes)
        foreach(var nodeDataInstance in sector.NodeData)
        {
            if (nodeDataInstance.Transforms.Length <= 1)
                continue;
            var node = sector.Nodes[nodeDataInstance.NodeIndex];
            if (result.TryGetValue(node.Type, out var count))
            {
                count += (ulong)nodeDataInstance.Transforms.Length;
                result[node.Type] = count;
            }
            else
            {
                result.Add(node.Type, (ulong)nodeDataInstance.Transforms.Length);
            }
        }
        progress.AddCurrent(1, Progress.ProgressSections.Processing);
        return result;
    }

    private static Dictionary<NodeTypeProcessingOptions.Enum, ulong> MergeDictionaries(
        IEnumerable<Dictionary<NodeTypeProcessingOptions.Enum, ulong>> dictionaries)
    {
        var result = new Dictionary<NodeTypeProcessingOptions.Enum, ulong>();
        foreach (var dict in dictionaries)
        {
            foreach (var (key, value) in dict)
            {
                if (result.TryGetValue(key, out var count))
                {
                    count += value;
                    result[key] = count;
                }
                else
                {
                    result.Add(key, value);
                }
            }
        }
        return result;
    }
    
    public static async Task Run()
    {
        Logger.Info("Building node type stats...");
        var gfs = GameFileService.Instance;
        var progress = Progress.Instance;

        progress.Reset();
        progress.SetWeight(0f, 1f, 0f);
        
        var sectors = gfs?.ArchiveManager?.GetGameArchives().SelectMany(x => x.Files)
            .Where(x => x.Value.Extension == ".streamingsector");
        progress.AddTarget(1, Progress.ProgressSections.Startup);
        progress.AddCurrent(1, Progress.ProgressSections.Startup);
        
        progress.AddTarget(sectors?.Count() ?? 0, Progress.ProgressSections.Processing);
        var tasks = sectors?.Select(x => Task.Run(() => ProcessSector(x.Value.FileName, gfs, progress)));
        var results = await Task.WhenAll(tasks);

        var cleanResult = results.OfType<Dictionary<NodeTypeProcessingOptions.Enum, ulong>>();
        
        var mergedResult = MergeDictionaries(cleanResult);

        var sb = new StringBuilder();
        foreach (var (key, value) in mergedResult)
        {
            sb.AppendLine($"{key},{value}");
        }
        var outputpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "debug", "nodeTypesFromInstances.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(outputpath) ?? "");
        await File.WriteAllTextAsync(outputpath, sb.ToString());
        Logger.Success("Finished building node type stats.");
    }
}