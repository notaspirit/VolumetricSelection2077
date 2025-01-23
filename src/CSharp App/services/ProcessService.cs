using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Parsers;
using Newtonsoft.Json;

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
    
    // also returns null if none of the nodes in the sector are inside the box
    private async Task<(bool success, string error, AxlRemovalSector? result)> ProcessStreamingsector(AbbrSector sector, string sectorPath, SelectionBox selectionBox)
    {
        Logger.Info($"Processing sector {sectorPath}");
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
                    nodeDataIndex++;
                    continue;
                }
                
                AbbrMesh? mesh = AbbrMeshParser.ParseFromGlb(model);
                if (mesh == null)
                {
                    Logger.Warning($"Failed to parse {meshDepotPath}.");
                    nodeDataIndex++;
                    continue;
                }
                
                bool isInside = CollisionCheckService.isMeshInsideBox(mesh,
                    selectionBox,
                    nodeDataEntry.Position,
                    nodeDataEntry.Scale,
                    nodeDataEntry.Rotation);
                
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
        Logger.Info($"Found {nodeDeletions.Count} node deletions out of {nodeDataIndex + 1} nodes.");
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
        Logger.Info("Validating inputs...");

        if (!ValidationService.ValidateInput(_settings.GameDirectory, _settings.OutputFilename))
        {
            return (false, "Validation failed");
        }
        Logger.Info("Starting Process...");

        string CETOuputFilepath = Path.Combine(_settings.GameDirectory, "bin", "x64", "plugins", "cyber_engine_tweaks",
            "mods", "VolumetricSelection2077", "data", "selection.json");
        string CETOutputFileString = File.ReadAllText(CETOuputFilepath);
        SelectionInput? CETOutputFile = JsonConvert.DeserializeObject<SelectionInput>(CETOutputFileString);

        if (CETOutputFile == null)
        {
            return (false, "Failed to parse CET output file");
        }
        // Logger.Info(JsonConvert.SerializeObject(CETOutputFile, Formatting.Indented));
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
            Logger.Warning("No sectors Intersect!");
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
        return (true, string.Empty);
    }
}