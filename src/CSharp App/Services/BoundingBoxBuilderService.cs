using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessagePack;
using SharpDX;
using VolumetricSelection2077.Models;
using VEnums = VolumetricSelection2077.Enums;
using WEnums = WolvenKit.RED4.Types.Enums;
using Vector3 = SharpDX.Vector3;

namespace VolumetricSelection2077.Services;

public class BoundingBoxBuilderService
{
    private readonly CacheService _cacheService;
    private readonly GameFileService _gameFileService;
    private readonly Progress _progress;
    public BoundingBoxBuilderService()
    {
        _cacheService = CacheService.Instance;
        _gameFileService = GameFileService.Instance;
        _progress = Progress.Instance;
    }
    
    /// <summary>
    /// Builds bounding boxes for all sectors matching the provided mode
    /// </summary>
    /// <param name="mode"></param>
    /// <exception cref="Exception">Game File Service is not initialized or Cache Service is not initialized</exception>
    public async Task BuildBounds(BuildBoundsMode mode)
    {
        if (!_gameFileService.IsInitialized)
            throw new Exception("Game file service is not initialized!");
        
        if (!_cacheService.IsInitialized)
            throw new Exception("Cache service is not initialized!");
        
        _progress.Reset();
        _progress.SetWeight(0f, 1f, 0f);

        _progress.AddTarget(1, Progress.ProgressSections.Startup);
        _progress.AddCurrent(1, Progress.ProgressSections.Startup);
        List<string> vanillaSectors = new();
        List<string> moddedSectors = new();

        switch (mode)
        {
            case BuildBoundsMode.All:
                vanillaSectors = _gameFileService.ArchiveManager.GetGameArchives().SelectMany(x => x.Files.Values.Where(y => y.Extension == ".streamingsector").Select(y => y.FileName)).ToList();
                moddedSectors = _gameFileService.ArchiveManager.GetModArchives().SelectMany(x => x.Files.Values.Where(y => y.Extension == ".streamingsector").Select(y => y.FileName)).ToList();
                break;
            case BuildBoundsMode.Vanilla:
                vanillaSectors = _gameFileService.ArchiveManager.GetGameArchives().SelectMany(x => x.Files.Values.Where(y => y.Extension == ".streamingsector").Select(y => y.FileName)).ToList();
                break;
            case BuildBoundsMode.RebuildModded:
                moddedSectors = _gameFileService.ArchiveManager.GetModArchives().SelectMany(x => x.Files.Values.Where(y => y.Extension == ".streamingsector").Select(y => y.FileName)).ToList();
                break;
            case BuildBoundsMode.MissingModded:
                var cachedModdedBounds = _cacheService.GetAllEntries(VEnums.CacheDatabases.ModdedBounds).Select(x => x.Key).ToList();;
                var archiveModdedSectors = _gameFileService.ArchiveManager.GetModArchives().SelectMany(x => x.Files.Values.Where(y => y.Extension == ".streamingsector").Select(y => y.FileName)).ToList();
                moddedSectors = archiveModdedSectors.Except(cachedModdedBounds).ToList();
                break;
        }
        _progress.AddTarget(vanillaSectors.Count + moddedSectors.Count, Progress.ProgressSections.Processing);
        
        _cacheService.StartListening();
        
        List<Task> tasks = vanillaSectors.Distinct().Select(x => Task.Run(() => ProcessStreamingsector(x, VEnums.CacheDatabases.VanillaBounds))).ToList();
        tasks.AddRange(moddedSectors.Distinct().Select(x =>
            Task.Run(() => ProcessStreamingsector(x, VEnums.CacheDatabases.ModdedBounds))));
        Logger.Info($"Building Bounds for {tasks.Count} Sectors...");
        await Task.WhenAll(tasks);
        
        _cacheService.StopListening();
        if (mode == BuildBoundsMode.Vanilla || mode == BuildBoundsMode.All) 
            _cacheService.SetMetaDataVanillaBoundsStatus(true);
        Logger.Info("Finished building bounds!");
    }
    
    /// <summary>
    /// Builds an accurate bounding box for the given sector, and writes it to the given database
    /// </summary>
    /// <param name="sectorPath">path to the sector within the archives</param>
    /// <param name="database">database to save the output to</param>
    public async Task ProcessStreamingsector(string sectorPath, VEnums.CacheDatabases? database)
    {
        try
        {
            Logger.Info($"Building bounds for {sectorPath}...");
            var sector = _gameFileService.GetSector(sectorPath);
            if (sector == null)
            {
                Logger.Warning($"Failed to get sector {sectorPath}");
                return;
            }

            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            
            foreach (var nodeDataEntry in sector.NodeData)
            {
                var nodeEntry = sector.Nodes[nodeDataEntry.NodeIndex];
                
                if ((nodeEntry?.ResourcePath?.EndsWith(".mesh") ?? false) || (nodeEntry?.ResourcePath?.EndsWith(".w2mesh") ?? false))
                {
                    var mesh = _gameFileService.GetCMesh(nodeEntry.ResourcePath);
                    if (mesh == null)
                    {
                        Logger.Warning($"Failed to get CMesh from {nodeEntry.ResourcePath}, using only position data");
                        foreach (var transform in nodeDataEntry.Transforms)
                        {
                            min = Vector3.Min(min, transform.Position);
                            max = Vector3.Max(max, transform.Position);
                        }
                        continue;
                    }

                    foreach (var transform in nodeDataEntry.Transforms)
                    {
                        var transformMatrix = Matrix.Scaling(transform.Scale) *
                                              Matrix.RotationQuaternion(transform.Rotation) *
                                              Matrix.Translation(transform.Position);
                        
                        foreach (var submesh in mesh.SubMeshes)
                        {
                            var transformedObb = new OrientedBoundingBox(submesh.BoundingBox);
                            transformedObb.Transform(transformMatrix);
                            foreach (var corner in transformedObb.GetCorners())
                            {
                                min = Vector3.Min(min, corner);
                                max = Vector3.Max(max, corner);
                            }
                        }
                    }
                } else if (nodeEntry?.Actors != null)
                {
                    foreach (var actor in nodeEntry.Actors)
                    {
                        var actorTransformMatrix = Matrix.Scaling(actor.Transform.Scale) *
                                                      Matrix.RotationQuaternion(actor.Transform.Rotation) *
                                                      Matrix.Translation(actor.Transform.Position);
                        
                        foreach (var shape in actor.Shapes)
                        {
                            var shapeTransformMatrix = Matrix.Scaling(new Vector3(1,1,1)) * 
                                                              Matrix.RotationQuaternion(shape.Transform.Rotation) * 
                                                              Matrix.Translation(shape.Transform.Position);
                            var summedTransformMatrix = shapeTransformMatrix * actorTransformMatrix;
                            switch (shape.ShapeType)
                            {
                                case WEnums.physicsShapeType.TriangleMesh:
                                case WEnums.physicsShapeType.ConvexMesh:
                                    var collisionMesh = await _gameFileService.GetPhysXMesh((ulong)nodeEntry.SectorHash,
                                        (ulong)shape.Hash);
                                    if (collisionMesh == null)
                                    {
                                        Logger.Warning(
                                            $"Failed to get PhysX Mesh from {(ulong)nodeEntry.SectorHash} : {shape.Hash}, using only position data");
                                        var combinedPosition = shape.Transform.Position + actor.Transform.Position;
                                        min = Vector3.Min(min, combinedPosition);
                                        max = Vector3.Max(max, combinedPosition);
                                        continue;
                                    }
                                    
                                    foreach (var submesh in collisionMesh.SubMeshes)
                                    {
                                        OrientedBoundingBox obb = new(submesh.BoundingBox);
                                        obb.Transform(summedTransformMatrix);
                                        foreach (var corner in obb.GetCorners())
                                        {
                                            min = Vector3.Min(min, corner);
                                            max = Vector3.Max(max, corner);
                                        }
                                    }
                                    break;
                                case WEnums.physicsShapeType.Box:
                                    OrientedBoundingBox collisionBox = new(-shape.Transform.Scale, shape.Transform.Scale);
                                    collisionBox.Transform(summedTransformMatrix);
                                    foreach (var corner in collisionBox.GetCorners())
                                    {
                                        min = Vector3.Min(min, corner);
                                        max = Vector3.Max(max, corner);
                                    }
                                    break;
                                case WEnums.physicsShapeType.Capsule:
                                    float height = shape.Transform.Scale.Y + 2 * actor.Transform.Scale.X;   
                                    Vector3 shapeSizeAsBox = new Vector3(shape.Transform.Scale.X, shape.Transform.Scale.X, height / 2f);
                                    
                                    OrientedBoundingBox capsuleObb = new(-shapeSizeAsBox, shapeSizeAsBox);
                                    capsuleObb.Transform(summedTransformMatrix);
                                    foreach (var corner in capsuleObb.GetCorners())
                                    {
                                        min = Vector3.Min(min, corner);
                                        max = Vector3.Max(max, corner);
                                    }
                                    break;
                                case WEnums.physicsShapeType.Sphere:
                                    OrientedBoundingBox sphereObb = new(new Vector3(-shape.Transform.Scale.X), new Vector3(shape.Transform.Scale.X));
                                    sphereObb.Transform(summedTransformMatrix);
                                    foreach (var corner in sphereObb.GetCorners())
                                    {
                                        min = Vector3.Min(min, corner);
                                        max = Vector3.Max(max, corner);
                                    }
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var transform in nodeDataEntry.Transforms)
                    {
                        min = Vector3.Min(min, transform.Position);
                        max = Vector3.Max(max, transform.Position);
                    }
                }
            }
            BoundingBox bb;
            if (min == max)
                bb = new BoundingBox(min - new Vector3(1,1,1), max + new Vector3(1,1,1));
            else
                bb = new BoundingBox(min, max);
            if (database != null)
                _cacheService.WriteEntry(new WriteCacheRequest(sectorPath, MessagePackSerializer.Serialize(bb), (VEnums.CacheDatabases)database));
        }
        catch (Exception e)
        {
            Logger.Exception(e, $"Failed to build bounding box for {sectorPath}");
        }
        finally
        {
            _progress.AddCurrent(1, Progress.ProgressSections.Processing);
        }
    }
}