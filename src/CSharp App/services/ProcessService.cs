using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using VolumetricSelection2077.Models;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using DynamicData;
using MessagePack;
using VolumetricSelection2077.Parsers;
using Newtonsoft.Json;
using SharpDX;
using VolumetricSelection2077.Resources;
using VolumetricSelection2077.TestingStuff;
using WolvenKit.RED4.Types;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Path = System.IO.Path;

namespace VolumetricSelection2077.Services;

public class ProcessService
{
    private readonly SettingsService _settings;
    private readonly GameFileService _gameFileService;
    private readonly DialogService _dialogService;
    private Progress _progress;
    public ProcessService(DialogService dialogService)
    {
        _settings = SettingsService.Instance;
        _gameFileService = GameFileService.Instance;
        _progress = Progress.Instance;
        _dialogService = dialogService;
    }

    class MergeChanges
    {
        public int newSectors { get; set; } = 0;
        public int newNodes { get; set; } = 0;
        public int newActors { get; set; } = 0;
    }
    
    private (List<AxlRemovalSector>?,MergeChanges?)  MergeSectors(string filepath, AxlRemovalFile newRemovals)
    {
        var changeCount = new MergeChanges();
        
        string fileContent = File.ReadAllText(filepath);
        var exisitngRemovalFile = UtilService.TryParseAxlRemovalFile(fileContent);
        if (exisitngRemovalFile != null)
        {
            var newSectors = newRemovals.Streaming.Sectors;
            var oldSectors = exisitngRemovalFile.Streaming.Sectors;
            
            Dictionary<string, AxlRemovalSector> mergedDict = oldSectors.ToDictionary(x => x.Path);

            foreach (var newSector in newSectors)
            {
                if (mergedDict.TryGetValue(newSector.Path, out AxlRemovalSector existingSector))
                {
                    Dictionary<int, AxlRemovalNodeDeletion> mergedNodes =
                        existingSector.NodeDeletions.ToDictionary(x => x.Index);
                    foreach (var newNode in newSector.NodeDeletions)
                    {
                        if (mergedNodes.TryGetValue(newNode.Index, out AxlRemovalNodeDeletion existingNode))
                        {
                            if (newNode.ActorDeletions != null || 
                                newNode.ActorDeletions?.Count > 0 ||
                                existingNode.ActorDeletions?.Count != null ||
                                existingNode.ActorDeletions?.Count > 0)
                            {
                                existingNode.ExpectedActors =  newNode.ExpectedActors ?? existingNode.ExpectedActors;
                                HashSet<int> actorSet = new HashSet<int>(newNode.ActorDeletions ?? new List<int>());
                                actorSet.UnionWith(existingNode.ActorDeletions ?? new List<int>());
                                existingNode.ActorDeletions = actorSet.ToList();
                                changeCount.newActors += actorSet.Count - existingNode.ActorDeletions.Count;
                            }
                        }
                        else
                        {
                            mergedNodes[newNode.Index] = newNode;
                            changeCount.newNodes++;
                            changeCount.newActors += newNode.ActorDeletions?.Count ?? 0;
                        }
                    }
                    existingSector.NodeDeletions = mergedNodes.Values.ToList();
                }
                else
                {
                    mergedDict[newSector.Path] = newSector;
                    changeCount.newSectors++;
                    changeCount.newNodes += newSector.NodeDeletions?.Count ?? 0;
                    foreach (var newNode in newSector.NodeDeletions)
                        changeCount.newActors += newNode.ActorDeletions?.Count ?? 0;
                }
            }
            var mergedSectors = mergedDict.Values.ToList();
            return (mergedSectors, changeCount);
        }
        Logger.Error($"Failed to parse existing removal file {filepath}");
        return (null, null);
    }
    
    private void SaveFile(AxlRemovalFile removalFile, string? customRemovalDirectory = null, string? customRemovalFilename = null)
    {
        string outputFilePath;
        if (customRemovalDirectory == null || customRemovalFilename == null)
        {
            if (_settings.SaveToArchiveMods)
                outputFilePath = Path.Combine(_settings.GameDirectory, "archive", "pc", "mod", _settings.OutputFilename) + ".xl";
            else if (!string.IsNullOrEmpty(_settings.OutputDirectory))
                outputFilePath = Path.Combine(_settings.OutputDirectory, _settings.OutputFilename) + ".xl";
            else
                throw new Exception(
                    $"Failed to save output file! Saving to output directory is enabled but no output directory is set!");
        }
        else
        {
            string fileName = Path.GetFileNameWithoutExtension(customRemovalFilename);
            outputFilePath = Path.Combine(customRemovalDirectory, fileName) + ".xl";
        }

        MergeChanges mergeChanges = new();
        if (_settings.SaveMode == SaveFileMode.Enum.Extend && File.Exists(outputFilePath))
        {
           var mergedSectors = MergeSectors(outputFilePath, removalFile);
           if (mergedSectors.Item1 != null)
           {
               removalFile.Streaming.Sectors = mergedSectors.Item1;
               mergeChanges = mergedSectors.Item2;
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
        
        
        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                
        if (!File.Exists(outputFilePath))
        {
            File.WriteAllText(outputFilePath, outputContent);
            Logger.Info($"Created file {outputFilePath}");
            return;
        }

        if (_settings.SaveMode == SaveFileMode.Enum.Extend)
        {
            File.WriteAllText(outputFilePath, outputContent);
            var newSectorS = mergeChanges?.newSectors != 1 ? "s" : "";
            var newNodesS = mergeChanges?.newNodes != 1 ? "s" : "";
            var newActorsS = mergeChanges?.newActors != 1 ? "s" : "";
            Logger.Info($"Extended file {outputFilePath} with {mergeChanges.newSectors} new sector{newSectorS}, {mergeChanges.newNodes} new node{newNodesS}, {mergeChanges.newActors} new actor{newActorsS}.");
            return;
        }
                
        if (_settings.SaveMode == SaveFileMode.Enum.Overwrite)
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
            if (Int32.TryParse(file.Split("+").Last().Split(".").First(), out int count))
            {
                if (count >= totalCount) 
                    totalCount = count + 1;
            }
        }
                
        string newOutputFilePath = $"{outputFilePathWithoutExtension.Split("+").First()}+{totalCount}.xl";
        File.WriteAllText(newOutputFilePath, outputContent);
        Logger.Info($"Created file {newOutputFilePath}");
    }
    private async Task<AxlRemovalNodeDeletion?> ProcessNodeAsync(AbbrStreamingSectorNodeDataEntry nodeDataEntry, int index, AbbrSector sector, SelectionInput selectionBox)
        {
            var nodeEntry = sector.Nodes[nodeDataEntry.NodeIndex];

            if (_settings.NukeOccluders && nodeEntry.Type.ToString().ToLower().Contains("occluder"))
            {
                return new AxlRemovalNodeDeletion()
                {
                    Type = nodeEntry.Type.ToString(),
                    Index = index,
                    DebugName = nodeEntry.DebugName
                };
            }
            
            bool? matchesDebugFilter = null;
            bool? matchesResourceFilter = null;
            
            if (_settings.DebugNameFilter.Count > 0)
            {
                matchesDebugFilter = false;
                foreach (var filter in _settings.DebugNameFilter)
                {
                    if (Regex.IsMatch(nodeEntry.DebugName?.ToLower() ?? "", filter))
                    {
                        matchesDebugFilter = true;
                        break;
                    }
                }
                
            }
            
            if (_settings.ResourceNameFilter.Count > 0)
            {
                matchesResourceFilter = false;
                foreach (var filter in _settings.ResourceNameFilter)
                {
                    if (Regex.IsMatch(nodeEntry.ResourcePath?.ToLower() ?? "", filter))
                    {
                        matchesResourceFilter = true;
                        break;
                    }
                }
            }

            if (matchesDebugFilter != null && matchesResourceFilter != null)
            {
                if (_settings.FilterModeOr)
                {
                    if (!((bool)matchesDebugFilter || (bool)matchesResourceFilter))
                    {
                        return null;
                    }
                }
                else
                {
                    if (!((bool)matchesDebugFilter && (bool)matchesResourceFilter))
                    {
                        return null;
                    }
                }
            } 
            else if (matchesDebugFilter.HasValue && matchesDebugFilter == false)
            {
                return null;
            }
            else if (matchesResourceFilter.HasValue && matchesResourceFilter == false)
            {
                return null;
            }
            
            int nodeTypeTableIndex = NodeTypeProcessingOptions.NodeTypeOptions.IndexOf(nodeEntry.Type.ToString());
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
            if ((nodeEntry.ResourcePath?.EndsWith(@".mesh") ?? false) || (nodeEntry.ResourcePath?.EndsWith(@".w2mesh") ?? false))
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
                    if (nodeDataEntry.AABB != null)
                    {
                        BoundingBox nodeAABB = (BoundingBox)nodeDataEntry.AABB;
                        if (selectionBox.Obb.Contains(ref nodeAABB) == ContainmentType.Disjoint)
                            return null;
                    }
                    
                    var mesh = _gameFileService.GetCMesh(nodeEntry.ResourcePath);
                    if (mesh == null)
                    {
                        Logger.Warning($"Failed to get CMesh from {nodeEntry.ResourcePath}");
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
                            Type = nodeEntry.Type.ToString(),
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
                            switch (shape.ShapeType)
                            {
                                case Enums.physicsShapeType.TriangleMesh:
                                case Enums.physicsShapeType.ConvexMesh:
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
                                        goto breakShapeLoop;
                                    }
                                    break;
                                case Enums.physicsShapeType.Box:
                                    bool isCollisionBoxInsideBox = CollisionCheckService.IsCollisionBoxInsideSelectionBox(shape, transformActor, selectionBox.Aabb,  selectionBox.Obb);
                                    if (isCollisionBoxInsideBox)
                                    {
                                        shapeIntersects = true;
                                        goto breakShapeLoop;
                                    }
                                    break;
                                case Enums.physicsShapeType.Capsule:
                                    bool isCollisionCapsuleInsideBox = CollisionCheckService.IsCollisionCapsuleInsideSelectionBox(shape, transformActor, selectionBox.Aabb,  selectionBox.Obb);
                                    if (isCollisionCapsuleInsideBox)
                                    {
                                        shapeIntersects = true;
                                        goto breakShapeLoop;
                                    }
                                    break;
                                case Enums.physicsShapeType.Sphere:
                                    bool intersects = CollisionCheckService.IsCollisionSphereInsideSelectionBox(shape, transformActor, selectionBox.Obb);
                                    if (intersects)
                                    {
                                        shapeIntersects = true;
                                        goto breakShapeLoop;
                                    }
                                    break;
                            }
                        }
                        
                        breakShapeLoop:
                        if (shapeIntersects)
                        {
                            actorRemoval.Add(actorIndex);
                        }
                        actorIndex++;
                    }
                    if (actorRemoval.Count > 0)
                    {
                        return new AxlRemovalNodeDeletion()
                            {
                                Index = index,
                                Type = nodeEntry.Type.ToString(),
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
                                Type = nodeEntry.Type.ToString(),
                                DebugName = nodeEntry.DebugName
                            };
                        }
                    }
                    return null;
            }

            return null;
        }
    // also returns null if none of the nodes in the sector are inside the box
    private async Task<(bool success, string error, AxlRemovalSector? result)> ProcessStreamingsector(AbbrSector sector, string sectorPath, SelectionInput selectionBox)
    {
        async Task<AxlRemovalNodeDeletion?> ProcessNodeAsyncWithReport(AbbrStreamingSectorNodeDataEntry nodeDataEntry, int index, AbbrSector sector, SelectionInput selectionBox)
        {
            try
            {
                return await ProcessNodeAsync(nodeDataEntry, index, sector, selectionBox);
            }
            finally
            {
                _progress.AddCurrent(1, Progress.ProgressSections.Processing);
            }
        }
        _progress.AddTarget(sector.NodeData.Length, Progress.ProgressSections.Processing);
        _progress.AddCurrent(1, Progress.ProgressSections.Startup);
        var tasks = sector.NodeData.Select((input, index) => Task.Run(() => ProcessNodeAsyncWithReport(input, index, sector, selectionBox))).ToArray();

        var nodeDeletionsRaw = await Task.WhenAll(tasks);

        bool isOnlyOccluders = true;
        List<AxlRemovalNodeDeletion> nodeDeletions = new();
        foreach (var nodeDeletion in nodeDeletionsRaw)
        {
            if (nodeDeletion != null)
            {
                nodeDeletions.Add(nodeDeletion);
                if (!nodeDeletion.Type.ToLower().Contains("occluder"))
                {
                    isOnlyOccluders = false;
                }
            }
        }
        
        if (nodeDeletions.Count == 0)
        {
            return (true, "No Nodes Intersect with Box.", null);
        }

        if (_settings.NukeOccludersAggressively == false && _settings.NukeOccluders && isOnlyOccluders)
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

    private async Task<bool> FetchRemoteSectorBBs()
    {
        var cacheMetadata = CacheService.Instance.GetMetadata();
        string fileUrl = $"https://raw.githubusercontent.com/notaspirit/VolumetricSelection2077Resources/SectorBounds/{cacheMetadata.GameVersion}-{cacheMetadata.VS2077Version}.bin";
        string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VolumetricSelection2077", "temp", $"{cacheMetadata.GameVersion}-{cacheMetadata.VS2077Version}.bin");
        using (HttpClient client = new HttpClient())
        {
            try
            {
                Logger.Info($"Fetching {fileUrl}...");
                byte[] fileData = await client.GetByteArrayAsync(fileUrl);

                await File.WriteAllBytesAsync(filePath, fileData);
            }
            catch (HttpRequestException e)
            {
                Logger.Exception(e, $"Failed to fetch remote sector bounds! {e.Message}");
                return false;
            }
        }
        
        CacheService.Instance.LoadSectorBBFromFile(filePath);
        File.Delete(filePath);
        return true;
    }
    
    /// <summary>
    /// Logs and Evaluates ValidationServiceResult
    /// </summary>
    /// <param name="vr"></param>
    /// <returns>true if all are valid, false if at least one is invalid</returns>
    private async Task<bool> EvaluateInputValidation(ValidationService.InputValidationResult vr)
    {
        int invalidCount = 0;
        bool invalidRegex = false;
        if (vr.OutputFileName == ValidationService.PathValidationResult.ValidFile)
            Logger.Success("Filename                 : OK");
        else
        {
            Logger.Error($"Filename                 : {vr.OutputFileName}");
            invalidCount++;
        }
        
        if (vr.CacheStatus)
            Logger.Success("Cache status             : OK");
        else
        {
            Logger.Error("Cache status             : Cache state does not match expected");
            invalidCount++;
        }
        
        if (vr.GameFileServiceStatus)
            Logger.Success("Game file service status : OK");
        else
        {
            Logger.Error("Game file service status : Not initialized");
            invalidCount++;
        }

        if (!_settings.SaveToArchiveMods)
        {
            if (vr.ValidOutputDirectory)
                Logger.Success("Output directory         : OK");
            else
            {
                Logger.Error($"Output directory         : {vr.OutputDirectroyPathValidationResult}");
                invalidCount++;
            }
        }

        if (vr.SelectionFileExists)
            Logger.Success("Selection File           : OK");
        else
        {
            string invalidReason = vr.SelectionFilePathValidationResult == ValidationService.PathValidationResult.ValidDirectory ? "Not found" : $"Invalid file path {vr.SelectionFilePathValidationResult}";
            Logger.Error($"Selection File           : {invalidReason}");
            invalidCount++;
        }

        if (vr.ResourceNameFilterValid)
            Logger.Success("Resource Name Filter     : OK");
        else
        {
            Logger.Error("Resource Name Filter     : Invalid Regex");
            invalidCount++;
            invalidRegex = true;
        }
        
        if (vr.DebugNameFilterValid)
            Logger.Success("Debug Name Filter        : OK");
        else
        {
            Logger.Error("Debug Name Filter        : Invalid Regex");
            invalidCount++;
            invalidRegex = true;
        }
        
        if (vr.VanillaSectorBBsBuild)
            Logger.Success("Vanilla Sector BBs       : OK");
        else
        {
            var dialogResult = await _dialogService.ShowDialog("Vanilla Sector Bounds not found!", "Vanilla Sector Bounds are not built, do you want to build them now (this will take a while) or fetch prebuild ones from remote?", ["Fetch Remote", "Build", "Cancel"]);
            switch (dialogResult)
            {
                case 0:
                    RetryFetchingRemote:
                    Logger.Info("Fetching from remote...");
                    var result = await FetchRemoteSectorBBs();
                    if (result)
                        Logger.Success("Vanilla Sector BBs       : OK");
                    else
                    {
                        var failedToFetchRemoteDialogResult = await _dialogService.ShowDialog("Failed to fetch remote sector bounds!", "Failed to fetch remote sector bounds, do you want to retry or build them now (this will take a while)?", ["Retry", "Build", "Cancel"]);
                        switch (failedToFetchRemoteDialogResult)
                        {
                            case 0:
                                goto RetryFetchingRemote;
                            case 1:
                                goto BuildSectorBBs;
                            case 2:
                                Logger.Error("Vanilla Sector BBs       : User Canceled");
                                invalidCount++;
                                break;
                        }
                    }
                    break;
                case 1:
                    BuildSectorBBs:
                    Logger.Info("Building...");
                    try
                    {
                        await new BoundingBoxBuilderService().BuildAllBounds();
                        Logger.Success("Vanilla Sector BBs       : OK");
                    }
                    catch (Exception e)
                    {
                        Logger.Exception(e, $"Failed to build sector bounds with error {e.Message}");
                        var failedToFetchRemoteDialogResult = await _dialogService.ShowDialog("Failed to build sector bounds!", "Failed to build sector bounds, do you want to retry or fetch them from remote?", ["Retry", "Fetch Remote", "Cancel"]);
                        switch (failedToFetchRemoteDialogResult)
                        {
                            case 0:
                                goto BuildSectorBBs;
                            case 1:
                                goto RetryFetchingRemote;
                            case 2:
                                Logger.Error("Vanilla Sector BBs       : User Canceled");
                                invalidCount++;
                                break;
                        }
                    }
                    break;
                case 2:
                    Logger.Error("Vanilla Sector BBs       : User Canceled");
                    invalidCount++;
                    break;
            }
        }
        
        if (invalidCount == 0)
        {
            return true;
        }

        if (invalidRegex)
        {
            Logger.Info(@"If you were not trying to use regex ensure that you have escaped all special characters, most commonly '\' and '.' (should be escaped as '\\' and '\.')");
        }
        
        return false;
    }
    
    private async Task<AxlRemovalSector?> SectorProcessThread(string streamingSectorName, SelectionInput CETOutputFile)
    {
        Logger.Info($"Starting sector process thread for {streamingSectorName}...");
        _progress.AddCurrent(1, Progress.ProgressSections.Startup);
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
    
    /// <summary>
    /// Processes all sectors in the selection file generated by the CET part and saves a removal file to the set location if intersections with the obb were found
    /// </summary>
    /// <param name="customRemovalFile">Absolute path to custom removal file, only used for benchmarking</param>
    /// <param name="customRemovalDirectory">Absolute path to custom output directory, only used for benchmarking</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Provided custom file does not exist, or only one optional param is provided</exception>
    public async Task<(bool success, string error)> MainProcessTask(string? customRemovalFile = null, string? customRemovalDirectory = null)
    {
        Logger.Info("Validating inputs...");

        try
        {
            var validationResult = ValidationService.ValidateInput(_settings.GameDirectory, _settings.OutputFilename);
            if (!await EvaluateInputValidation(validationResult))
                return (false, "Invalid Input");
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, fileOnly: true);
            return (false, ex.Message + " : Failed to validate inputs");
        }

        
        Logger.Info("Starting Process...");
        
        _progress.Reset();
        _progress.SetWeight(0.1f, 0.85f, 0.05f);
        
        bool customRemovalFileProvided = customRemovalFile != null;
        bool customRemovalDirectoryProvided = customRemovalDirectory != null;
        if (customRemovalFileProvided != customRemovalDirectoryProvided)
        {
            throw new ArgumentException("Both file path and output directory must be provided for a custom process!");
        }

        if (!File.Exists(customRemovalFile) && (customRemovalDirectoryProvided || customRemovalDirectory != null))
        {
            throw new ArgumentException($"Provided file ({customRemovalFile}) doesn't exist!");
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
        
        try
        {
            CacheService.Instance.StartListening();
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "Failed to start listening to write requests in cache service!", fileOnly: true);
            return (false, "Failed to start listening to write requests in cache service!");
        }

        AxlRemovalSector?[] sectorsOutputRaw;
        
        CETOutputFile.Sectors.Clear();

        var sectorVanilla = CacheService.Instance.GetAllEntries(CacheDatabases.VanillaBounds);
        Logger.Info($"Found {sectorVanilla?.Length} sectors in vanilla bounds");
        foreach (var sectorAABB in sectorVanilla)
        {
            // Logger.Info($"Checking sector {sectorAABB.Key} in vanilla bounds...");
            var aabb = MessagePackSerializer.Deserialize<BoundingBox>(sectorAABB.Value);
            if (CETOutputFile.Aabb.Contains(aabb) != ContainmentType.Disjoint)
            {
                CETOutputFile.Sectors.Add(sectorAABB.Key);
                Logger.Info($"Found sector {sectorAABB.Key} in vanilla bounds with min max {aabb.Minimum} to {aabb.Maximum}");
            }
        }
        
        Logger.Debug($"Found {CETOutputFile.Sectors.Count} sectors in vanilla bounds");
        try
        {
            _progress.AddTarget(CETOutputFile.Sectors.Count * 2, Progress.ProgressSections.Startup);
            var tasks = CETOutputFile.Sectors.Select(input => Task.Run(() => SectorProcessThread(input, CETOutputFile)))
                .ToArray();

            sectorsOutputRaw = await Task.WhenAll(tasks);
        }
        finally
        {
            CacheService.Instance.StopListening();
        }
        
        _progress.AddTarget(2, Progress.ProgressSections.Finalization);
        List<AxlRemovalSector> sectors = new();
        foreach (var sector in sectorsOutputRaw)
        {
            if (sector != null)
            {
                sectors.Add(sector);
            }
        }
        _progress.AddCurrent(1, Progress.ProgressSections.Finalization);
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
            
            SaveFile(removalFile, customRemovalDirectory, customRemovalFile);
        }
        _progress.AddCurrent(1, Progress.ProgressSections.Finalization);
        return (true, string.Empty);
    }
}