using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia.Controls.Shapes;
using DynamicData;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Parsers;
using Newtonsoft.Json;
using SharpDX;
using VolumetricSelection2077.Resources;
using VolumetricSelection2077.TestingStuff;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Path = System.IO.Path;

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
        async Task<AxlRemovalNodeDeletion?> ProcessNodeAsync(AbbrStreamingSectorNodeDataEntry nodeDataEntry, int index)
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
                    return null;
                }
            }
            
            CollisionCheck.Types entryType = CollisionCheck.Types.Default;
            if (nodeEntry.MeshDepotPath != null)
            {
                entryType = CollisionCheck.Types.Mesh;
            } 
            else if (nodeEntry.SectorHash != null && (nodeEntry.Actors != null || nodeEntry.Actors?.Length > 0))
            {
                entryType = CollisionCheck.Types.Collider;
            }

            switch (entryType)
            {
                case CollisionCheck.Types.Mesh:
                    var mesh = _gameFileService.GetCMesh(nodeEntry.MeshDepotPath);
                    if (mesh == null)
                    {
                        Logger.Warning($"Failed to get CMesh from {nodeEntry.MeshDepotPath}");
                        return null;
                    }

                    bool isInside = CollisionCheckService.IsMeshInsideBox(mesh,
                        selectionBox.Obb,
                        selectionBox.Aabb,
                        nodeDataEntry.Transforms);
                    
                    if (isInside)
                    {
                        return new AxlRemovalNodeDeletion()
                        {
                            Index = index,
                            Type = nodeEntry.Type,
                            DebugName = nodeEntry.DebugName
                        };
                    }
                    return null;
                case CollisionCheck.Types.Collider:
                    List<int> actorRemoval = new List<int>();
                    int actorIndex = 0;
                    foreach (var actor in nodeEntry.Actors)
                    {
                        var shapeIntersects = false;
                        var sectorHash = nodeEntry.SectorHash;
                        var transformActor = actor.Transform;
                        foreach (var shape in actor.Shapes)
                        {
                            
                            if (shape.ShapeType.Contains("Mesh"))
                            {
                                var collisionMesh = await _gameFileService.GetPhysXMesh((ulong)sectorHash, (ulong)shape.Hash);
                                if (collisionMesh == null)
                                {
                                    Logger.Warning($"Failed to get PhysX Mesh from {sectorHash} : {shape.Hash}");
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
                                string collectionName = sectorPath.Split(@"\")[^1] + " " + index + " " + actorIndex; // just for testing so it's easy to identify the source of the shapes
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
                        return new AxlRemovalNodeDeletion()
                            {
                                Index = index,
                                Type = nodeEntry.Type,
                                ActorDeletions = actorRemoval,
                                ExpectedActors = nodeEntry.Actors.Length,
                                DebugName = nodeEntry.DebugName
                            };
                    }
                    return null;
                case CollisionCheck.Types.Default:
                    foreach (var transform in nodeDataEntry.Transforms)
                    {
                        var intersection = selectionBox.Obb.Contains(transform.Position);
                        if (intersection != ContainmentType.Disjoint)
                        {
                            return new AxlRemovalNodeDeletion()
                            {
                                Index = index,
                                Type = nodeEntry.Type,
                                DebugName = nodeEntry.DebugName
                            };
                        }
                    }
                    return null;
            }

            return null;
        }
        
        var tasks = sector.NodeData.Select((input, index) => Task.Run(() => ProcessNodeAsync(input, index))).ToArray();

        var nodeDeletionsRaw = await Task.WhenAll(tasks);

        List<AxlRemovalNodeDeletion> nodeDeletions = new();
        foreach (var nodeDeletion in nodeDeletionsRaw)
        {
            if (nodeDeletion != null)
            {
                nodeDeletions.Add(nodeDeletion);
            }
        }
        
        if (nodeDeletions.Count == 0)
        {
            return (true, "No Nodes Intersect with Box.", null);
        }

        var result = new AxlRemovalSector()
        {
            NodeDeletions = nodeDeletions,
            ExpectedNodes = sector.NodeData.Length,
            Path = sectorPath
        };
        return (true, "", result);
    }

    public async Task<(bool success, string error)> MainProcessTask(string? customRemovalFile = null, string? customRemovalDirectory = null)
    {
        Logger.Info($"Version: {_settings.ProgramVersion}");
        
        CompareGenOutput.Run();
        return (true, string.Empty);
        
        var options = new JsonSerializerOptions
        {
            Converters = { new Vector3Converter(), new QuaternionConverter() },
            WriteIndented = true
        };
        
        Logger.Info("Processing Sector");
        var path = @"C:\Users\zweit\AppData\Roaming\VolumetricSelection2077\sectorComparison\DirectParsing.json";
        var sectorPath = @"base\worlds\03_night_city\_compiled\default\exterior_7_-2_0_3.streamingsector";
        
        var sectorTest = _gameFileService.GetSector(sectorPath);
        
        var sectorJson = JsonSerializer.Serialize(sectorTest, options);
        
        File.WriteAllText(path, sectorJson);
        
        Logger.Info("Processing CMesh");
        var pathCMesh = @"C:\Users\zweit\AppData\Roaming\VolumetricSelection2077\meshComparison\DirectCMeshParsing.json";
        var cmeshPath = @"base\environment\architecture\common\int\int_ent_industrial_a\int_ent_industrial_a_pillar_beam_h100_l600_a.mesh";
        var meshTestCMesh = _gameFileService.GetCMesh(cmeshPath);
        Logger.Info($"CMesh is null: {meshTestCMesh is null}");
        Logger.Info($"Submeshes: {meshTestCMesh.SubMeshes.Length}");
        var AbbrCMeshJson = JsonSerializer.Serialize(meshTestCMesh, options);
        File.WriteAllText(pathCMesh, AbbrCMeshJson);
        
        Logger.Info("Processing PhysX Mesh 1");
        var pathPhysX1 = @"C:\Users\zweit\AppData\Roaming\VolumetricSelection2077\meshComparison\DirectPhysX1Parsing.json";
        ulong sectorHash = 12717457377011094652;
        ulong physXMesh1Hash = 9246134327794375400;
        var meshTestPhysX1Mesh = await _gameFileService.GetPhysXMesh(sectorHash, physXMesh1Hash);
        Logger.Info($"CMesh is null: {meshTestPhysX1Mesh is null}");
        Logger.Info($"Submeshes: {meshTestPhysX1Mesh.SubMeshes.Length}");
        var AbbrPhysX1Json = JsonSerializer.Serialize(meshTestPhysX1Mesh, options);
        File.WriteAllText(pathPhysX1, AbbrPhysX1Json);
        
        Logger.Info("Processing PhysX Mesh 2");
        var pathPhysX2 = @"C:\Users\zweit\AppData\Roaming\VolumetricSelection2077\meshComparison\DirectPhysX2Parsing.json";
        ulong physXMesh2Hash = 9386483786976406912;
        var meshTestPhysX2Mesh = await _gameFileService.GetPhysXMesh(sectorHash, physXMesh2Hash);
        Logger.Info($"CMesh is null: {meshTestPhysX2Mesh is null}");
        Logger.Info($"Submeshes: {meshTestPhysX2Mesh.SubMeshes.Length}");
        var AbbrPhysX2Json = JsonSerializer.Serialize(meshTestPhysX2Mesh, options);
        File.WriteAllText(pathPhysX2, AbbrPhysX2Json);
        
        // CompareGenOutput.Run();
        return (true, string.Empty);
        Logger.Info("Validating inputs...");
        
        if (!ValidationService.ValidateInput(_settings.GameDirectory, _settings.OutputFilename))
        {
            return (false, "Validation failed");
        }
        
        Logger.Info("Starting Process...");
        
        bool customRemovalFileProvided = customRemovalFile != null;
        bool customRemovalDirectoryProvided = customRemovalDirectory != null;
        if (customRemovalFileProvided != customRemovalDirectoryProvided)
        {
            throw new Exception("Both file path and output directory must be provided for a custom process!");
        }

        if (!File.Exists(customRemovalFile) && (customRemovalDirectoryProvided || customRemovalDirectory != null))
        {
            throw new Exception($"Provided file ({customRemovalFile}) doesn't exist!");
        }

        SelectionInput? CETOutputFile;
        
        if (customRemovalDirectory == null)
        {
            string CETOuputFilepath = Path.Combine(_settings.GameDirectory, "bin", "x64", "plugins", "cyber_engine_tweaks",
                "mods", "VolumetricSelection2077", "data", "selection.json");
            string CETOutputFileString = File.ReadAllText(CETOuputFilepath);
            ( var successSP, var errorSP, CETOutputFile) = SelectionParser.ParseSelection(CETOutputFileString);

            if (CETOutputFile == null || successSP == false)
            {
                return (false, $"Failed to parse CET output file with error: {errorSP}");
            }
        }
        else
        {
            string customOutputFileString = File.ReadAllText(customRemovalFile);
            (var successSP, var errorSP, CETOutputFile) = SelectionParser.ParseSelection(customOutputFileString);
            if (CETOutputFile == null || successSP == false)
            {
                return (false, $"Failed to parse CET output file with error: {errorSP}");
            }
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
        /*
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
        */
        // List<AxlRemovalSector> sectors = new List<AxlRemovalSector>();

        // List<string> testSectors = new();
        // testSectors.Add(@"base\worlds\03_night_city\_compiled\default\exterior_-10_-4_0_1.streamingsector");
        
        async Task<AxlRemovalSector?> SectorProcessThread(string streamingSectorName)
        {
            Logger.Info($"Starting sector process thread for {streamingSectorName}...");

            try
            {
                string streamingSectorNameFix = Regex.Replace(streamingSectorName, @"\\{2}", @"\");
                var sector = _gameFileService.GetSector(streamingSectorNameFix);
                if (sector == null)
                {
                    Logger.Warning($"Failed to find sector {streamingSectorNameFix}");
                    return null;
                }
                
                var (successPSS, errorPSS, resultPss) = await ProcessStreamingsector(sector, streamingSectorName, CETOutputFile);
                if (successPSS)
                { 
                    Logger.Info($"Successfully processed streamingsector {streamingSectorName} which found {resultPss?.NodeDeletions.Count ?? 0} nodes out of {sector.NodeData.Length} nodes.");
                    return resultPss;
                }
            
                Logger.Error($"Failed to processes streamingsector {streamingSectorName} with error: {errorPSS}");
                return null;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to Processes {streamingSectorName}: {e}");
                return null;
            }
        }
        
        var tasks = CETOutputFile.Sectors.Select(input => Task.Run(() => SectorProcessThread(input))).ToArray();

        var sectorsOutputRaw = await Task.WhenAll(tasks);

        List<AxlRemovalSector> sectors = new();
        foreach (var sector in sectorsOutputRaw)
        {
            if (sector != null)
            {
                sectors.Add(sector);
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
            
            int nodeCount = 0;
            foreach (var sector in sectors)
            {
                nodeCount += sector.NodeDeletions.Count;
            }
            Logger.Success($"Found {nodeCount} nodes across {sectors.Count} sectors.");
            
            string axlFilePath;
            if (customRemovalDirectory == null)
            {
                axlFilePath = _settings.GameDirectory + @"\archive\pc\mod\" + _settings.OutputFilename + ".xl";   
            }
            else
            {
                string fileName = Path.GetFileNameWithoutExtension(customRemovalFile);
                axlFilePath = customRemovalDirectory + $"\\{fileName}.xl";
            }
            
            if (_settings.ExtendExistingFile && File.Exists(axlFilePath))
            {
                string fileContent = File.ReadAllText(axlFilePath);
                var exisitngRemovalFile = UtilService.TryParseAxlRemovalFile(fileContent);
                if (exisitngRemovalFile != null)
                {
                    var newSectors = removalFile.Streaming.Sectors;
                    var oldSectors = exisitngRemovalFile.Streaming.Sectors;
                        
                    Dictionary<string, AxlRemovalSector> mergedDict = oldSectors.ToDictionary(x => x.Path);

                    foreach (var newSector in newSectors)
                    {
                        if (mergedDict.TryGetValue(newSector.Path, out AxlRemovalSector existingSector))
                        {
                            existingSector.NodeDeletions.AddRange(newSector.NodeDeletions);
                            existingSector.NodeDeletions = existingSector.NodeDeletions.Distinct().ToList();
                        }
                        else
                        {
                            mergedDict[newSector.Path] = newSector;
                        }
                    }
                    var mergedSectors = mergedDict.Values.ToList();
                    removalFile.Streaming.Sectors = mergedSectors;
                }
                else
                {
                    Logger.Error($"Failed to parse existing removal file {axlFilePath}");
                }
            }
            
            string outputContent;
            if (_settings.SaveAsYaml)
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                    .Build();

                outputContent = serializer.Serialize(removalFile);
            }
            else
            {
                outputContent = JsonConvert.SerializeObject(removalFile,
                    new JsonSerializerSettings()
                        { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented });
            }
            
            void SaveFile(string outputFilePath)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                
                if (!File.Exists(outputFilePath))
                {
                    File.WriteAllText(outputFilePath, outputContent);
                    Logger.Info($"Created file {outputFilePath}");
                    return;
                }

                if (_settings.ExtendExistingFile)
                {
                    File.WriteAllText(outputFilePath, outputContent);
                    Logger.Info($"Extended file {outputFilePath}");
                    return;
                }
                
                if (_settings.AllowOverwrite)
                {
                    File.WriteAllText(outputFilePath, outputContent);
                    Logger.Info($"Overwrote file {outputFilePath}");
                    return;
                }
                
                int totalCount = 1;
                string outputFilePathWithoutExtension = outputFilePath.Split('.').First();
                foreach (var file in Directory.GetFiles(Path.GetDirectoryName(outputFilePath), "*.*",
                             SearchOption.AllDirectories))
                {
                    if (!file.StartsWith(outputFilePathWithoutExtension)) continue;
                    if (Int32.TryParse(file.Split("+").Last(), out int count))
                    {
                        if (count > totalCount) totalCount = count;
                    }
                }
                
                string newOutputFilePath = $"{outputFilePathWithoutExtension.Split("+").First()}+{totalCount}.xl";
                File.WriteAllText(newOutputFilePath, outputContent);
                Logger.Info($"Created file {newOutputFilePath}");
            }
            
            SaveFile(axlFilePath);
        }
        // TestingService.TestVertexTransform();
        return (true, string.Empty);
    }
}