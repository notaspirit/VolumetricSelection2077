using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Parsers;
using Newtonsoft.Json;
using WolvenKit.RED4.Types;

namespace VolumetricSelection2077.Services;

public class ProcessService
{
    private readonly SettingsService _settings;
    private readonly GameFileService _gameFileService;
    public ProcessService()
    {
        _settings = SettingsService.Instance;
        _gameFileService = GameFileService.Instance;
    }

    private string FormatElapsedTime(TimeSpan elapsed)
    {
        var parts = new List<string>();
        
        if (elapsed.Hours > 0)
        {
            parts.Add($"{elapsed.Hours} hour{(elapsed.Hours == 1 ? "" : "s")}");
        }
        if (elapsed.Minutes > 0)
        {
            parts.Add($"{elapsed.Minutes} minute{(elapsed.Minutes == 1 ? "" : "s")}");
        }
        if (elapsed.Seconds > 0 || parts.Count == 0)
        {
            parts.Add($"{elapsed.Seconds}.{elapsed.Milliseconds:D3} seconds");
        }
        
        return string.Join(", ", parts);
    }
    // also returns null if none of the nodes in the sector are inside the box
    private async Task<(bool success, string error, AxlRemovalSector? result)> ProcessStreamingsector(AbbrSector sector, string sectorPath, SelectionBox selectionBox)
    {
        List<AxlRemovalNodeDeletion> nodeDeletions = new List<AxlRemovalNodeDeletion>();
        int nodeDataIndex = 0;
        foreach (var nodeDataEntry in sector.NodeData)
        {
            var nodeEntry = sector.Nodes[nodeDataEntry.NodeIndex];
            if (nodeEntry.MeshDepotPath != null)
            {
                string meshDepotPath = nodeEntry.MeshDepotPath;
                // get mesh, pass local transform and mesh to mesh check method, if mesh is inside the box add index and type to list
                var (successGet, errorGet, model) = _gameFileService.GetGameFileAsGlb(meshDepotPath);
                if (!successGet || model == null)
                {
                    Logger.Warning($"Failed to get {meshDepotPath} with error: {errorGet}");
                    continue;
                }
                
                AbbrMesh? mesh = AbbrMeshParser.ParseFromGlb(model);
                if (mesh == null)
                {
                    Logger.Warning($"Failed to parse {meshDepotPath}.");
                    continue;
                }
                
                bool isInside = CollisionCheckService.checkMesh(mesh,
                    selectionBox,
                    nodeDataEntry.Rotation,
                    nodeDataEntry.Position,
                    nodeDataEntry.Scale);
                
                if (isInside)
                {
                    nodeDeletions.Add(new AxlRemovalNodeDeletion()
                    {
                        Index = nodeDataIndex,
                        Type = nodeEntry.Type
                    });
                }
            }

            if (nodeEntry.SectorHash != null && (nodeEntry.Actors != null || nodeEntry.Actors?.Count > 0))
            {
                foreach (var actor in nodeEntry.Actors)
                {
                    // switch here to process all the possible actor types (e.g. mesh, box, capsule etc)
                }
            }
            nodeDataIndex++;
        }

        if (nodeDeletions.Count == 0)
        {
            return (true, "No Nodes Intersect with Box.", null);
        }

        var result = new AxlRemovalSector()
        {
            NodeDeletions = nodeDeletions,
            ExpectedNodes = sector.NodeData.Count,
            Path = sectorPath
        };
        return (true, "", result);
    }

    public async Task<(bool success, string error)> Process()
    {
        var stopwatch = Stopwatch.StartNew();
        Logger.Info("Validating inputs...");

        if (!ValidationService.ValidateInput(_settings.GameDirectory, _settings.OutputFilename))
        {
            stopwatch.Stop();
            Logger.Error($"Process failed after {FormatElapsedTime(stopwatch.Elapsed)}");
            return (false, "Validation failed");
        }
        Logger.Info("Starting Process...");

        string CETOuputFilepath = Path.Combine(_settings.GameDirectory, "bin", "x64", "plugins", "cyber_engine_tweaks",
            "mods", "VolumetricSelection2077", "data", "selection.json");
        string CETOutputFileString = File.ReadAllText(CETOuputFilepath);
        SelectionInput? CETOutputFile = JsonConvert.DeserializeObject<SelectionInput>(CETOutputFileString);

        if (CETOutputFile == null)
        {
            stopwatch.Stop();
            Logger.Error("Failed to parse CET output file");
            Logger.Error($"Process failed after {FormatElapsedTime(stopwatch.Elapsed)}");
            return (false, "Failed to parse CET output file");
        }
        /*
        _gameFileService.Testing();
        
        string actorHash = "1367596920923086056";
        string sectorHash = "10565727457643407283";
        var (success, error, Output) = _gameFileService.GetGeometryFromCache(sectorHash, actorHash);
        Logger.Info($"Getting GeometryCache Entry resulted in: {success}, {error}, {Output}");
        
        string testGLBPath =
            Regex.Replace(
                "ep1\\worlds\\03_night_city\\sectors\\_external\\proxy\\2939601539\\mon_ave_scaffolding_f.mesh",
                @"\\{2}", @"\");
        ResourcePath testResourcePath = testGLBPath;
        Logger.Info(testResourcePath.GetResolvedText() ?? "No resolved text found!");
        var (successGLB, errorGLB, testGLB) = _gameFileService.GetGameFileAsGlb(testGLBPath);
        if (testGLB == null || successGLB == false)
        {
            stopwatch.Stop();
            Logger.Error("Failed to get test GLB file");
            Logger.Error(errorGLB);
            Logger.Error($"Process failed after {FormatElapsedTime(stopwatch.Elapsed)}");
            return (false, "Failed to get test GLB file");
        }

        var parsedGlb = AbbrMeshParser.ParseFromGlb(testGLB);
        
        List<string> testSectors = new List<string>();
        testSectors.Add("base\\worlds\\03_night_city\\_compiled\\default\\exterior_-6_-4_0_2.streamingsector");
        */
        List<AxlRemovalSector> sectors = new List<AxlRemovalSector>();
        
        foreach (string streamingSectorName in CETOutputFile.Sectors)
        {
            string streamingSectorNameFix = Regex.Replace(streamingSectorName, @"\\{2}", @"\");
            var (successGET, errorGET, stringGET) = _gameFileService.GetGameFileAsJsonString(streamingSectorNameFix);
            if (!successGET || !string.IsNullOrEmpty(errorGET) || string.IsNullOrEmpty(stringGET))
            {
                Logger.Error($"Failed to get streamingsector {streamingSectorName}, error: {errorGET}");
                continue;
            }
            // Logger.Info(stringGET);

            AbbrSector? sectorDeserialized = AbbrSectorParser.Deserialize(stringGET);
            if (sectorDeserialized == null)
            {
                Logger.Error($"Failed to deserialize streamingsector {streamingSectorName}");
                continue;
            }

            var (successPSS, errorPSS, resultPss) =
                await ProcessStreamingsector(sectorDeserialized, streamingSectorName, CETOutputFile.Box);
            if (successPSS && resultPss != null)
            {
                sectors.Add(resultPss);
            }
        }

        if (sectors.Count == 0)
        {
            Logger.Error("No sectors Intersect!");
        }
        else
        {
            var removalFile = new AxlRemovalFile()
            {
                Streaming = new AxlRemovalStreaming()
                {
                    Sectors = sectors
                }
            };
            Logger.Info(JsonConvert.SerializeObject(removalFile, new JsonSerializerSettings(){NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented}));
        }
        
        
        stopwatch.Stop();
        var elapsed = stopwatch.Elapsed;
        Logger.Info($"Process completed in {FormatElapsedTime(elapsed)}.");
        return (true, string.Empty);
    }
}