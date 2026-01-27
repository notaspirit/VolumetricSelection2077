using System;
using System.Collections.Generic;
using System.Linq;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public struct TrafficTuple
{
    public int Index;
    public string Type;
}

public class FindTrafficNodes : IDebugTool
{
    public void Run()
    {
        Logger.Info("Finding traffic nodes...");
        var gfs = GameFileService.Instance;
        var ps = Progress.Instance;
        
        var sectors = gfs.ArchiveManager.GetGameArchives()
            .SelectMany(x => x.Files.Values
            .Where(y => y.Extension == ".streamingsector")
            .Select(y => y.FileName))
            .ToList().Distinct();
        
        ps.Reset();
        ps.SetWeight(1f, 0f, 0f);
        ps.AddTarget(sectors.Count(), Progress.ProgressSections.Startup);
        
        var outPaths = new Dictionary<string, List<TrafficTuple>>();
        
        foreach (var sPath in sectors)
        {
            ps.AddCurrent(1, Progress.ProgressSections.Startup);
            var sector = gfs.GetSector(sPath);
            if (sector == null)
                continue;
            
            var i = -1;
            foreach (var node in sector.Nodes)
            {
                i++;
                if (!node.Type.ToString().Contains("Traffic", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (outPaths.TryGetValue(sPath, out var list))
                {
                    list.Add(new TrafficTuple() { Index = i, Type = node.Type.ToString()});
                }
                else
                {
                    outPaths[sPath] = new List<TrafficTuple>() { new TrafficTuple() { Index = i, Type = node.Type.ToString()} };
                }
            }
        }
        
        Logger.Info($"Found {outPaths.SelectMany(x => x.Value).Count()} traffic nodes across {outPaths.Count} sectors.");
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(outPaths, Newtonsoft.Json.Formatting.Indented);
        Logger.Info($"\n{json}");
    }
}