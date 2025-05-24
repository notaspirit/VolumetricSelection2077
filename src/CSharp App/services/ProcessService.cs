using System;
using System.Collections.Concurrent;
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
using WolvenKit.RED4.Types;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Path = System.IO.Path;
using Octokit;

namespace VolumetricSelection2077.Services;

public class ProcessService
{
    private readonly SettingsService _settings;
    private readonly GameFileService _gameFileService;
    private readonly MergingService _mergingService;
    private readonly DialogService _dialogService;
    private readonly BoundingBoxBuilderService _boundingBoxBuilderService;
    private readonly CacheService _cacheService;
    private readonly ValidationService _validationService;
    private Progress _progress;
    public ProcessService(DialogService dialogService)
    {
        _settings = SettingsService.Instance;
        _gameFileService = GameFileService.Instance;
        _progress = Progress.Instance;
        _mergingService = new MergingService();
        _dialogService = dialogService;
        _boundingBoxBuilderService = new BoundingBoxBuilderService();
        _cacheService = CacheService.Instance;
        _validationService = new ValidationService();
    }

    private ConcurrentBag<KeyValuePair<string, AxlProxyNodeMutationMutation>> proxyNodes = new();
    private Dictionary<string, int> sectorPathToExpectedNodes = new();
    
    private (AxlModificationFile?, SectorMergeChangesCount?)  MergeSectors(string filepath, AxlModificationFile newRemovals)
    {
        
        string fileContent = File.ReadAllText(filepath);
        var existingRemovalFile = UtilService.TryParseAxlRemovalFile(fileContent);
        if (existingRemovalFile != null)
        {
            var mergedFile = _mergingService.MergeAxlFiles(newRemovals, existingRemovalFile);
            var changes = MergingService.CalculateDifference(mergedFile, existingRemovalFile);
            return (mergedFile, changes);
        }
        Logger.Error($"Failed to parse existing removal file {filepath}");
        return (null, null);
        
    }
    
    private void SaveFile(AxlModificationFile removalFile, string? customRemovalDirectory = null, string? customRemovalFilename = null)
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

        SectorMergeChangesCount mergeChanges = new();
        if (_settings.SaveMode == SaveFileMode.Enum.Extend && File.Exists(outputFilePath))
        {
           var mergedSectors = MergeSectors(outputFilePath, removalFile);
           if (mergedSectors.Item1 != null)
           {
               removalFile = mergedSectors.Item1;
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
            var newInstances = mergeChanges?.newInstances != 1 ? "s" : "";
            Logger.Info($"Extended file {outputFilePath} with {mergeChanges?.newSectors} new sector{newSectorS}, {mergeChanges?.newNodes} new node{newNodesS}, {mergeChanges?.newInstances} new instance{newInstances}, {mergeChanges?.newActors} new actor{newActorsS}.");
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

    private List<AxlSector> ProcessProxyNodes(ConcurrentBag<KeyValuePair<string, AxlProxyNodeMutationMutation>> proxyNodes)
    {
        var result = new List<AxlSector>();
        foreach (var proxyNode in proxyNodes)
        {
            if (result.All(x => x.Path != proxyNode.Key))
            {
                var expectedNodes = 0;
                if (!sectorPathToExpectedNodes.TryGetValue(proxyNode.Key, out expectedNodes))
                {
                    var sector = _gameFileService.GetSector(proxyNode.Key);
                    if (sector == null)
                    {
                        Logger.Warning($"Failed to get sector {proxyNode.Key}");
                        continue;
                    }
                    expectedNodes = sector.NodeData.Length;
                }

                result.Add(new AxlSector
                {
                    Path = proxyNode.Key,
                    ExpectedNodes = expectedNodes,
                    NodeMutations = new List<AxlNodeMutation> { proxyNode.Value }
                });
            }
            else
            {
                var sector = result.First(x => x.Path == proxyNode.Key);
                sector?.NodeMutations?.Add(proxyNode.Value);
            }
        }
        return result;
    }
    
    private static bool IsNodeTypeProxy(NodeTypeProcessingOptions.Enum nodeType)
    {
        return nodeType.ToString().ToLower().Contains("proxy");
    }
    
    private async Task<AxlNodeDeletion?> ProcessNodeAsync(AbbrStreamingSectorNodeDataEntry nodeDataEntry, int index, AbbrSector sector, SelectionInput selectionBox, string sectorPath)
    {
        var nodeEntry = sector.Nodes[nodeDataEntry.NodeIndex];

        if (IsNodeTypeProxy(nodeEntry.Type) && _settings.ResolveProxies)
        {
            if (nodeEntry.ProxyRef == null)
                return null;
            
            proxyNodes.Add(new KeyValuePair<string, AxlProxyNodeMutationMutation>(sectorPath, new AxlProxyNodeMutationMutation
            {
                DebugName = nodeEntry.DebugName,
                Index = index,
                ProxyRef = nodeEntry.ProxyRef,
                Type = nodeEntry.Type.ToString(),
                NbNodesUnderProxyDiff = 0
            }));
            
            return null;
        }
        
        if (_settings.NukeOccluders && nodeEntry.Type.ToString().ToLower().Contains("occluder"))
        {
            return new AxlNodeDeletion()
            {
                Type = nodeEntry.Type.ToString(),
                Index = index,
                DebugName = nodeEntry.DebugName,
                ProxyRef = nodeEntry.ProxyRef,
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

                var isInstanced = nodeEntry.Type == NodeTypeProcessingOptions.Enum.worldInstancedDestructibleMeshNode
                                  ||  nodeEntry.Type == NodeTypeProcessingOptions.Enum.worldInstancedMeshNode;
                
                var (isInside, indices) = CollisionCheckService.IsMeshInsideBox(mesh,
                    selectionBox.Obb,
                    selectionBox.Aabb,
                    nodeDataEntry.Transforms, checkAllTransforms: isInstanced);
                
                
                if (isInside && !isInstanced)
                {
                    return new AxlNodeDeletion()
                    {
                        Index = index,
                        Type = nodeEntry.Type.ToString(),
                        DebugName = nodeEntry.DebugName,
                        ProxyRef = nodeEntry.ProxyRef,
                    };
                }
                if (isInside && isInstanced)
                {
                    return new AxlInstancedNodeDeletion
                    {
                        Index = index,
                        Type = nodeEntry.Type.ToString(),
                        DebugName = nodeEntry.DebugName,
                        ExpectedInstances = nodeDataEntry.Transforms.Length,
                        InstanceDeletions = indices,
                        ProxyRef = nodeEntry.ProxyRef,
                    };
                }
                break;
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
                    return new AxlCollisionNodeDeletion
                        {
                            Index = index,
                            Type = nodeEntry.Type.ToString(),
                            ActorDeletions = actorRemoval,
                            ExpectedActors = nodeEntry.Actors.Length,
                            DebugName = nodeEntry.DebugName,
                            ProxyRef = nodeEntry.ProxyRef,
                        };
                }
                break;
            case CollisionCheck.Types.Default:
                foreach (var transform in nodeDataEntry.Transforms)
                {
                    var intersection = selectionBox.Obb.Contains(transform.Position);
                    if (intersection != ContainmentType.Disjoint)
                    {
                        return new AxlNodeDeletion
                        {
                            Index = index,
                            Type = nodeEntry.Type.ToString(),
                            DebugName = nodeEntry.DebugName,
                            ProxyRef = nodeEntry.ProxyRef,
                        };
                    }
                }
                break;
        }
        return null;
    }

    /// <summary>
    /// Processes a streaming sector by identifying and filtering nodes based on the selection input.
    /// </summary>
    /// <param name="sector">The sector data to be processed, containing node entries.</param>
    /// <param name="sectorPath">The file path associated with the sector.</param>
    /// <param name="selectionBox">The selection criteria used for filtering nodes.</param>
    /// <returns>A tuple containing a success status, an error message if applicable, and the resulting sector object if successful or no nodes selected</returns>
    private async Task<(bool success, string error, AxlSector? result)> ProcessStreamingsector(AbbrSector sector,
        string sectorPath, SelectionInput selectionBox)
    {
        async Task<AxlNodeDeletion?> ProcessNodeAsyncWithReport(AbbrStreamingSectorNodeDataEntry nodeDataEntry, int index, AbbrSector sector, SelectionInput selectionBox)
        {
            try
            {
                return await ProcessNodeAsync(nodeDataEntry, index, sector, selectionBox, sectorPath);
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
        List<AxlNodeDeletion> nodeDeletions = new();
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
        
        var result = new AxlSector()
        {
            NodeDeletions = nodeDeletions,
            ExpectedNodes = sector.NodeData.Length,
            Path = sectorPath
        };
        return (true, "", result);
    }
    
    /// <summary>
    /// Fetches remote sector bounds from VS2077 Resource repo 
    /// </summary>
    /// <returns>true if successful</returns>
    private async Task<bool> FetchRemoteSectorBBs()
    {
        var cacheMetadata = _cacheService.GetMetadata();
        string fileUrl = $"https://github.com/notaspirit/VolumetricSelection2077Resources/raw/refs/heads/main/SectorBounds/{cacheMetadata.GameVersion}-{cacheMetadata.VS2077Version}.bin";
        string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VolumetricSelection2077", "temp", $"{cacheMetadata.GameVersion}-{cacheMetadata.VS2077Version}.bin");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
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
        
        _cacheService.LoadSectorBBFromFile(filePath);
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
                    Logger.Info("Fetching Sector Bounds from remote...");
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
                    Logger.Info("Building Sector Bounds...");
                    try
                    {
                        await _boundingBoxBuilderService.BuildBounds(BoundingBoxBuilderService.BuildBoundsMode.Vanilla);
                        Logger.Success("Vanilla Sector BBs       : OK");
                    }
                    catch (Exception e)
                    {
                        Logger.Exception(e, $"Failed to build sector bounds with error {e.Message}");
                        var failedToBuildSectorBoundsDialog = await _dialogService.ShowDialog("Failed to build sector bounds!", "Failed to build sector bounds, do you want to retry or fetch them from remote?", ["Retry", "Fetch Remote", "Cancel"]);
                        switch (failedToBuildSectorBoundsDialog)
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
        
        if (vr.ModdedSectorBBsBuild)
            Logger.Success("Modded Sector BBs        : OK");
        else
        {
            var dialogResult = await _dialogService.ShowDialog("Modded Sector Bounds not found!", "Not all modded sectors have a build bounding box!", ["Build Missing", "Rebuild All", "Ignore", "Cancel"]);
            switch (dialogResult)
            {
                case 0:
                    await _boundingBoxBuilderService.BuildBounds(
                        BoundingBoxBuilderService.BuildBoundsMode.MissingModded);
                    break;
                case 1:
                    await _boundingBoxBuilderService.BuildBounds(
                        BoundingBoxBuilderService.BuildBoundsMode.RebuildModded);
                    break;
                case 2:
                    Logger.Warning("Modded Sector BBs        : User Ignored");
                    break;
                case 3:
                    Logger.Error("Modded Sector BBs        : User Canceled");
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
    
    private async Task<AxlSector?> SectorProcessThread(string streamingSectorName, SelectionInput CETOutputFile)
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
            sectorPathToExpectedNodes.TryAdd(streamingSectorNameFix, sector.NodeData.Length);
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
            Logger.Exception(e, $"Failed to process sector {streamingSectorName} with error {e.Message}");;
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
            var validationResult = _validationService.ValidateInput(_settings.GameDirectory, _settings.OutputFilename);
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
        
        proxyNodes.Clear();
        sectorPathToExpectedNodes.Clear();
        
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
            _cacheService.StartListening();
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "Failed to start listening to write requests in cache service!", fileOnly: true);
            return (false, "Failed to start listening to write requests in cache service!");
        }

        var vanillaBoundingBoxes = _cacheService.GetAllEntries(CacheDatabases.VanillaBounds);
        AxlSector?[] sectorsOutputRaw;
        
        CETOutputFile.Sectors.Add(vanillaBoundingBoxes
            .Where(x => CETOutputFile.Aabb.Contains(MessagePackSerializer.Deserialize<BoundingBox>(x.Value)) != ContainmentType.Disjoint)
            .Select(x => x.Key));

        if (_settings.SupportModdedResources)
        {
            var moddedBoundingBoxes = _cacheService.GetAllEntries(CacheDatabases.ModdedBounds);
            CETOutputFile.Sectors.Add(moddedBoundingBoxes
                .Where(x => CETOutputFile.Aabb.Contains(MessagePackSerializer.Deserialize<BoundingBox>(x.Value)) != ContainmentType.Disjoint)
                .Select(x => x.Key));
        }

        Logger.Info($"Found {CETOutputFile.Sectors.Count} sectors to process...");
        
        try
        {
            _progress.AddTarget(CETOutputFile.Sectors.Count * 2, Progress.ProgressSections.Startup);
            var tasks = CETOutputFile.Sectors.Select(input => Task.Run(() => SectorProcessThread(input, CETOutputFile)))
                .ToArray();
            sectorsOutputRaw = await Task.WhenAll(tasks);
        }
        finally
        {
            _cacheService.StopListening();
        }
        
        _progress.AddTarget(3, Progress.ProgressSections.Finalization);
        List<AxlSector> sectorRemovals = new();
        foreach (var sector in sectorsOutputRaw)
        {
            if (sector != null)
            {
                sectorRemovals.Add(sector);
            }
        }
        _progress.AddCurrent(1, Progress.ProgressSections.Finalization);

        var sectorMutations = ProcessProxyNodes(proxyNodes);
        var sectors = _mergingService.MergeSectors(sectorRemovals, sectorMutations);
        
        _progress.AddCurrent(1, Progress.ProgressSections.Finalization);
        if (sectors.Count == 0)
        {
            Logger.Warning("No sectors intersect, no output file generated!");
        }
        else
        {
            var removalFile = new AxlModificationFile()
            {
                Streaming = new AxlStreaming()
                {
                    Sectors = sectors
                }
            };
            
            int nodeCount = 0;
            foreach (var sector in sectors)
            {
                nodeCount += sector?.NodeDeletions?.Count ?? 0;
                nodeCount += sector?.NodeMutations?.Count ?? 0;
            }
            Logger.Success($"Found {nodeCount} nodes across {sectors.Count} sectors.");
            
            SaveFile(removalFile, customRemovalDirectory, customRemovalFile);
        }
        _progress.AddCurrent(1, Progress.ProgressSections.Finalization);
        return (true, string.Empty);
    }
}