using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using VolumetricSelection2077.Models;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using DynamicData;
using MessagePack;
using VolumetricSelection2077.Parsers;
using VEnums = VolumetricSelection2077.Enums;
using WEnums = WolvenKit.RED4.Types.Enums;
using SharpDX;
using Path = System.IO.Path;

namespace VolumetricSelection2077.Services;

public class ProcessService
{
    private readonly SettingsService _settings;
    private readonly GameFileService _gameFileService;
    private readonly DialogService _dialogService;
    private readonly BoundingBoxBuilderService _boundingBoxBuilderService;
    private readonly CacheService _cacheService;
    private readonly ValidationService _validationService;
    private readonly PostProcessingService _postProcessingService;
    private Progress _progress;
    public ProcessService(DialogService dialogService)
    {
        _settings = SettingsService.Instance;
        _gameFileService = GameFileService.Instance;
        _progress = Progress.Instance;
        _dialogService = dialogService;
        _boundingBoxBuilderService = new BoundingBoxBuilderService();
        _cacheService = CacheService.Instance;
        _validationService = new ValidationService();
        _postProcessingService = new PostProcessingService();
    }
    
    private async Task<AxlRemovalNodeDeletion?> ProcessNodeAsync(AbbrStreamingSectorNodeDataEntry nodeDataEntry, int index, AbbrSector sector, SelectionInput selectionBox)
        {
            var nodeEntry = sector.Nodes[nodeDataEntry.NodeIndex];
            
            bool? matchesDebugFilter = null;
            bool? matchesResourceFilter = null;
            
            if (_settings.DebugNameFilter.Count > 0)
            {
                matchesDebugFilter = false;
                foreach (var filter in _settings.DebugNameFilter)
                {
                    if (Regex.IsMatch(nodeEntry.DebugName ?? "", filter, RegexOptions.IgnoreCase))
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
                    if (Regex.IsMatch(nodeEntry.ResourcePath ?? "", filter, RegexOptions.IgnoreCase))
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
            
            if (_settings.NodeTypeFilter[(int)nodeEntry.Type] != true)
            {
                return null;
            }
            
            
            VEnums.CollisionCheckTypes entryType = VEnums.CollisionCheckTypes.Default;
            if ((nodeEntry.ResourcePath?.EndsWith(@".mesh") ?? false) || (nodeEntry.ResourcePath?.EndsWith(@".w2mesh") ?? false))
            {
                entryType = VEnums.CollisionCheckTypes.Mesh;
            } 
            else if (nodeEntry.SectorHash != null && (nodeEntry.Actors != null || nodeEntry.Actors?.Length > 0))
            {
                entryType = VEnums.CollisionCheckTypes.Collider;
            }

            switch (entryType)
            {
                case VEnums.CollisionCheckTypes.Mesh:
                    if (nodeDataEntry.AABB != null)
                    {
                        BoundingBox nodeAABB = (BoundingBox)nodeDataEntry.AABB;
                        if (selectionBox.Obb.Contains(ref nodeAABB) == ContainmentType.Disjoint)
                            return null;
                    }
                    
                    bool isInstanced = nodeEntry.Type is VEnums.NodeTypeProcessingOptions.worldInstancedDestructibleMeshNode 
                                                        or VEnums.NodeTypeProcessingOptions.worldInstancedMeshNode;
                    
                    if (!isInstanced && _settings.SaveFileFormat == VEnums.SaveFileFormat.WorldBuilder)
                        isInstanced = nodeEntry.Type is VEnums.NodeTypeProcessingOptions.worldFoliageNode;
                    
                    var mesh = _gameFileService.GetCMesh(nodeEntry.ResourcePath);
                    if (mesh == null)
                    {
                        Logger.Warning($"Failed to get CMesh from {nodeEntry.ResourcePath}");
                        return null;
                    }
                    var (isInside, instances) = CollisionCheckService.IsMeshInsideBox(mesh,
                        selectionBox.Obb,
                        selectionBox.Aabb,
                        nodeDataEntry.Transforms,
                        checkAllTransforms: isInstanced);

                    if (!isInside) 
                        return null;
                    
                    if (isInstanced)
                        return new AxlRemovalNodeDeletion
                        {
                            Index = index,
                            Type = nodeEntry.Type.ToString(),
                            DebugName = nodeEntry.DebugName,
                            ActorDeletions = instances,
                            ExpectedActors = nodeDataEntry.Transforms.Length
                        };
                    return new AxlRemovalNodeDeletion
                    {
                        Index = index,
                        Type = nodeEntry.Type.ToString(),
                        DebugName = nodeEntry.DebugName
                    };
                case VEnums.CollisionCheckTypes.Collider:
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
                                case WEnums.physicsShapeType.TriangleMesh:
                                case WEnums.physicsShapeType.ConvexMesh:
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
                                case WEnums.physicsShapeType.Box:
                                    bool isCollisionBoxInsideBox = CollisionCheckService.IsCollisionBoxInsideSelectionBox(shape, transformActor, selectionBox.Aabb,  selectionBox.Obb);
                                    if (isCollisionBoxInsideBox)
                                    {
                                        shapeIntersects = true;
                                        goto breakShapeLoop;
                                    }
                                    break;
                                case WEnums.physicsShapeType.Capsule:
                                    bool isCollisionCapsuleInsideBox = CollisionCheckService.IsCollisionCapsuleInsideSelectionBox(shape, transformActor, selectionBox.Aabb,  selectionBox.Obb);
                                    if (isCollisionCapsuleInsideBox)
                                    {
                                        shapeIntersects = true;
                                        goto breakShapeLoop;
                                    }
                                    break;
                                case WEnums.physicsShapeType.Sphere:
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
                case VEnums.CollisionCheckTypes.Default:
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
        var tasks = sector.NodeData.Select((input, index) => Task.Run(() => ProcessNodeAsync(input, index, sector, selectionBox))).ToArray();

        var nodeDeletionsRaw = await Task.WhenAll(tasks);
        
        List<AxlRemovalNodeDeletion> nodeDeletions = nodeDeletionsRaw.OfType<AxlRemovalNodeDeletion>().ToList();

        _progress.AddCurrent(1, Progress.ProgressSections.Processing);
        
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
        if (vr.OutputFileName == ValidationService.PathValidationResult.Valid)
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
            string invalidReason = vr.SelectionFilePathValidationResult == ValidationService.PathValidationResult.Valid ? "Not found" : $"Invalid file path {vr.SelectionFilePathValidationResult}";
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

        if (_settings.SaveMode == VEnums.SaveFileMode.Subtract)
        {
            if (vr.SubtractionTargetExists)
            {
                Logger.Success("Subtraction Target       : OK");
            }
            else
            {
                Logger.Error("Subtraction Target       : Not found");
                invalidCount++;
            }
        }
        
        if (vr.VanillaSectorBBsBuild)
            Logger.Success("Vanilla Sector BBs       : OK");
        else
        {
            var dialogResult = await _dialogService.ShowDialog("Vanilla Sector Bounds not found!", "Vanilla Sector Bounds are not built, do you want to build them now (this will take a while) or fetch prebuild ones from remote?", 
                [new DialogButton("Fetch Remote", VEnums.DialogButtonStyling.Primary),
                    new DialogButton("Build", VEnums.DialogButtonStyling.Secondary),
                    new DialogButton("Cancel", VEnums.DialogButtonStyling.Destructive)]);
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
                        var failedToFetchRemoteDialogResult = await _dialogService.ShowDialog("Failed to fetch remote sector bounds!", "Failed to fetch remote sector bounds, do you want to retry or build them now (this will take a while)?", 
                        [new DialogButton("Retry", VEnums.DialogButtonStyling.Primary),
                            new DialogButton("Build", VEnums.DialogButtonStyling.Secondary),
                            new DialogButton("Cancel", VEnums.DialogButtonStyling.Destructive)]);
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
                        await _boundingBoxBuilderService.BuildBounds(BuildBoundsMode.Vanilla);
                        Logger.Success("Vanilla Sector BBs       : OK");
                    }
                    catch (Exception e)
                    {
                        Logger.Exception(e, $"Failed to build sector bounds with error {e.Message}");
                        var failedToBuildSectorBoundsDialog = await _dialogService.ShowDialog("Failed to build sector bounds!", "Failed to build sector bounds, do you want to retry or fetch them from remote?", 
                        [new DialogButton("Retry", VEnums.DialogButtonStyling.Primary),
                            new DialogButton("Fetch Remote", VEnums.DialogButtonStyling.Secondary),
                            new DialogButton("Cancel", VEnums.DialogButtonStyling.Destructive)]);
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
            var dialogResult = await _dialogService.ShowDialog("Modded Sector Bounds not found!", "Not all modded sectors have a build bounding box!", 
            [new DialogButton("Build Missing", VEnums.DialogButtonStyling.Primary),
                new DialogButton("Rebuild All", VEnums.DialogButtonStyling.Secondary),
                new DialogButton("Ignore", VEnums.DialogButtonStyling.Secondary),
                new DialogButton("Cancel", VEnums.DialogButtonStyling.Destructive)]);
            switch (dialogResult)
            {
                case 0:
                    await _boundingBoxBuilderService.BuildBounds(
                        BuildBoundsMode.MissingModded);
                    break;
                case 1:
                    await _boundingBoxBuilderService.BuildBounds(
                        BuildBoundsMode.RebuildModded);
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
    
    private async Task<AxlRemovalSector?> SectorProcessThread(string streamingSectorName, SelectionInput CETOutputFile)
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
        _progress.SetWeight(0.05f, 0.9f, 0.05f);
        
        _progress.AddTarget(2, Progress.ProgressSections.Startup);
        
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
            string CETOuputFilepath;
            if (string.IsNullOrWhiteSpace(_settings.CustomSelectionFilePath))
                CETOuputFilepath= Path.Combine(_settings.GameDirectory, "bin", "x64", "plugins", "cyber_engine_tweaks",
                "mods", "VolumetricSelection2077", "data", "selection.json");
            else
                CETOuputFilepath= Path.Combine(_settings.CustomSelectionFilePath, "bin", "x64", "plugins", "cyber_engine_tweaks",
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

        _progress.AddCurrent(1, Progress.ProgressSections.Startup);
        
        AxlRemovalSector?[] sectorsOutputRaw;

        var vanillaBoundingBoxes = _cacheService.GetAllEntries(VEnums.CacheDatabases.VanillaBounds);
        
        CETOutputFile.Sectors.Add(vanillaBoundingBoxes
            .Where(x => CETOutputFile.Aabb.Contains(MessagePackSerializer.Deserialize<BoundingBox>(x.Value)) != ContainmentType.Disjoint)
            .Select(x => x.Key));

        if (_settings.SupportModdedResources)
        {
            var moddedBoundingBoxes = _cacheService.GetAllEntries(VEnums.CacheDatabases.ModdedBounds);
            CETOutputFile.Sectors.Add(moddedBoundingBoxes
                .Where(x => CETOutputFile.Aabb.Contains(MessagePackSerializer.Deserialize<BoundingBox>(x.Value)) != ContainmentType.Disjoint)
                .Select(x => x.Key));
        }

        Logger.Info($"Found {CETOutputFile.Sectors.Count} sectors to process...");
        
        _progress.AddCurrent(1, Progress.ProgressSections.Startup);
        
        try
        {
            _progress.AddTarget(CETOutputFile.Sectors.Count, Progress.ProgressSections.Processing);
            var tasks = CETOutputFile.Sectors.Select(input => Task.Run(() => SectorProcessThread(input, CETOutputFile)));
            sectorsOutputRaw = await Task.WhenAll(tasks);
        }
        finally
        {
            _cacheService.StopListening();
        }
        
        
        _postProcessingService.Run(sectorsOutputRaw);
        
        return (true, string.Empty);
    }
}