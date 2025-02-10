using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using DynamicData;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Parsers;
using Newtonsoft.Json;
using SharpDX;
using VolumetricSelection2077.Resources;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
    private async Task<(bool success, string error, AxlRemovalSector? result)> ProcessStreamingsector(AbbrSector sector, string sectorPath, SelectionInput selectionBox)
    {
        Logger.Info($"Processing sector {sectorPath}");
        
        List<AxlRemovalNodeDeletion> nodeDeletions = new List<AxlRemovalNodeDeletion>();
        
        int nodeDataIndex = 0;
        foreach (var nodeDataEntry in sector.NodeData)
        {
            // Logger.Debug($"Processing node {nodeDataIndex}:");
            var nodeEntry = sector.Nodes[nodeDataEntry.NodeIndex];
            
            int nodeTypeTableIndex = NodeTypeProcessingOptions.NodeTypeOptions.IndexOf(nodeEntry.Type);
            if (nodeTypeTableIndex == -1)
            {
                Logger.Warning($"Node {nodeEntry.Type} is not part of the assumed node type set! Please report this issue. Processing node regardless.");
            }
            else
            {
                if (_settings.NodeTypeFilter[nodeTypeTableIndex] != true)
                {
                    continue;
                }
            }
            
            CollisionCheck.Types entryType = CollisionCheck.Types.Default;
            if (nodeEntry.MeshDepotPath != null)
            {
                entryType = CollisionCheck.Types.Mesh;
            } 
            else if (nodeEntry.SectorHash != null && (nodeEntry.Actors != null || nodeEntry.Actors?.Count > 0))
            {
                entryType = CollisionCheck.Types.Collider;
            }

            switch (entryType)
            {
                case CollisionCheck.Types.Mesh:
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

                    bool isInside = CollisionCheckService.IsMeshInsideBox(mesh,
                        selectionBox.Obb,
                        selectionBox.Aabb,
                        nodeDataEntry.Transforms);

                    if (isInside)
                    {
                        nodeDeletions.Add(new AxlRemovalNodeDeletion()
                        {
                            Index = nodeDataIndex,
                            Type = nodeEntry.Type
                        });
                    }
                    break;
                case CollisionCheck.Types.Collider:
                    
                    List<int> actorRemoval = new List<int>();
                    int actorIndex = 0;
                    foreach (var actor in nodeEntry.Actors)
                    {
                        bool shapeIntersects = false;
                        string sectorHash = nodeEntry.SectorHash;
                        AbbrSectorTransform transformActor = actor.Transform;
                        foreach (var shape in actor.Shapes)
                        {
                            
                            if (shape.ShapeType.Contains("Mesh"))
                            {
                                var (successGetShape, errorGetShape, collisionMeshString) = _gameFileService.GetGeometryFromCache(sectorHash, shape.Hash);
                                if (!successGetShape || collisionMeshString == null)
                                {
                                    Logger.Warning($"Failed to get shape {sectorHash}, {shape.Hash} with error: {errorGetShape}");
                                    continue;
                                }
                                
                                AbbrMesh collisionMesh = AbbrMeshParser.ParseFromJson(collisionMeshString);
                                if (collisionMesh == null)
                                {
                                    Logger.Warning($"Failed to parse {collisionMeshString}.");
                                    continue;
                                }
                                
                                bool isCollisionMeshInsideBox = CollisionCheckService.IsCollisonMeshInsideSelectionBox(collisionMesh, selectionBox.Obb, selectionBox.Aabb, transformActor, shape.Transform);
                                if (isCollisionMeshInsideBox)
                                {
                                    shapeIntersects = true;
                                    break;
                                }
                            }
                            
                            if (shape.ShapeType == "Box")
                            {
                                string collectionName = sectorPath.Split(@"\")[^1] + " " + nodeDataIndex + " " + actorIndex; // just for testing so it's easy to identify the source of the shapes
                                bool isCollisionBoxInsideBox = CollisionCheckService.IsCollisionBoxInsideSelectionBox(shape, transformActor, selectionBox.Aabb,  selectionBox.Obb, collectionName);
                                if (isCollisionBoxInsideBox)
                                {
                                    shapeIntersects = true;
                                    break;
                                }
                            }
                            
                            if (shape.ShapeType == "Capsule")
                            {
                                bool isCollisionCapsuleInsideBox = CollisionCheckService.IsCollisionCapsuleInsideSelectionBox(shape, transformActor, selectionBox.Aabb,  selectionBox.Obb);
                                if (isCollisionCapsuleInsideBox)
                                {
                                    shapeIntersects = true;
                                    break;
                                }
                            }
                            
                        }

                        if (shapeIntersects)
                        {
                            actorRemoval.Add(actorIndex);
                        }
                        actorIndex++;
                    }

                    // Logger.Debug($"Found {actorRemoval.Count} actors marked for removal in {nodeDataIndex}");
                    if (actorRemoval.Count > 0)
                    {
                        nodeDeletions.Add(new AxlRemovalNodeDeletion()
                            {
                                Index = nodeDataIndex,
                                Type = nodeEntry.Type,
                                ActorDeletions = actorRemoval,
                                ExpectedActors = nodeEntry.Actors.Count
                            });
                    }
                    
                    break;
                case CollisionCheck.Types.Default:
                    foreach (var transform in nodeDataEntry.Transforms)
                    {
                        var intersection = selectionBox.Obb.Contains(transform.Position);
                        if (intersection != ContainmentType.Disjoint)
                        {
                            nodeDeletions.Add(new AxlRemovalNodeDeletion()
                            {
                                Index = nodeDataIndex,
                                Type = nodeEntry.Type
                            });
                        }
                    }
                    break;
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
        var (successSP, errorSP, CETOutputFile) = SelectionParser.ParseSelection(CETOutputFileString);

        if (CETOutputFile == null || successSP == false)
        {
            return (false, $"Failed to parse CET output file with error: {errorSP}");
        }
        /*
        Logger.Debug("Selection Box AABB Details:");
        Logger.Debug($"Center point: {CETOutputFile.Aabb.Center.ToString()}");
        Logger.Debug($"Box Vertices: {string.Join(", ", CETOutputFile.Aabb.GetCorners())}");
        Logger.Debug($"Box scale: {CETOutputFile.Aabb.Size}");
        
        Logger.Debug("Selection Box OBB Details:");
        Logger.Debug($"Center point: {CETOutputFile.Obb.Center.ToString()}");
        Logger.Debug($"Box Vertices: {string.Join(", ", CETOutputFile.Obb.GetCorners())}");
        Logger.Debug($"Box scale: {CETOutputFile.Obb.Size}");
        */
        string uniqueId = "initial";
        
        string selectionBoxString = $"selectionBoxVerts{uniqueId} = [ ";
        var vertsSelectionBox = CETOutputFile.Obb.GetCorners();
        foreach (var v in vertsSelectionBox)
        {
            selectionBoxString +=
                $"({v.X.ToString(CultureInfo.InvariantCulture)}, {v.Y.ToString(CultureInfo.InvariantCulture)}, {v.Z.ToString(CultureInfo.InvariantCulture)}),";
        }

        selectionBoxString +=
            $"]\n" +
            $"selectionBox{uniqueId} = create_box(\"selectionBox{uniqueId}\", selectionBoxVerts{uniqueId}, \"initial\")\n";
        
        Logger.Debug(selectionBoxString);
        List<AxlRemovalSector> sectors = new List<AxlRemovalSector>();

        // List<string> testSectors = new();
        // testSectors.Add(@"base\worlds\03_night_city\_compiled\default\exterior_-10_-4_0_1.streamingsector");
        
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
                await ProcessStreamingsector(sectorDeserialized, streamingSectorName, CETOutputFile);
            if (successPSS && resultPss != null)
            {
                sectors.Add(resultPss);
            }
        }
        
        if (sectors.Count == 0)
        {
            Logger.Warning("No sectors intersect, no output file generated!");
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
            string axlFilePath = _settings.GameDirectory + @"\archive\pc\mod\" + _settings.OutputFilename + ".xl";
            File.WriteAllText(axlFilePath, JsonConvert.SerializeObject(removalFile, new JsonSerializerSettings(){NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented}));
            int nodeCount = 0;
            foreach (var sector in sectors)
            {
                nodeCount += sector.NodeDeletions.Count;
            }
            Logger.Success($"Found {nodeCount} nodes in {sectors.Count} sectors and saved output to the archive mod folder.");
        }
        // TestingService.TestVertexTransform();
        return (true, string.Empty);
    }
}