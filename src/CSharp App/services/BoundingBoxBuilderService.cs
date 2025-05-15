using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessagePack;
using SharpDX;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Resources;
using WolvenKit.RED4.Types;
using Vector3 = SharpDX.Vector3;

namespace VolumetricSelection2077.Services;

public class BoundingBoxBuilderService
{
    private readonly CacheService _cacheService;
    private readonly GameFileService _gameFileService;
    private readonly SettingsService _settings;
    private readonly Progress _progress;
    public BoundingBoxBuilderService()
    {
        _cacheService = CacheService.Instance;
        _gameFileService = GameFileService.Instance;
        _settings = SettingsService.Instance;
        _progress = Progress.Instance;
    }

    public async Task ProcessStreamingsector(string sectorPath, CacheDatabases? database)
    {
        try
        {
            Logger.Debug($"Building bounds for {sectorPath}...");
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
                if (nodeEntry.Type == NodeTypeProcessingOptions.Enum.worldInstancedOccluderNode)
                    continue;
                
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
                        foreach (var submesh in mesh.SubMeshes)
                        {
                            var halfExtends = submesh.BoundingBox.Size / 2 * transform.Scale;
                            var transformedAabb = new BoundingBox(submesh.BoundingBox.Center - halfExtends, submesh.BoundingBox.Center + halfExtends);
                            foreach (var corner in transformedAabb.GetCorners())
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
                        foreach (var shape in actor.Shapes)
                        {
                            Matrix actorTransformMatrix = Matrix.Scaling(actor.Transform.Scale) *
                                                              Matrix.RotationQuaternion(actor.Transform.Rotation) *
                                                              Matrix.Translation(actor.Transform.Position);
                            switch (shape.ShapeType)
                            {
                                case Enums.physicsShapeType.TriangleMesh:
                                case Enums.physicsShapeType.ConvexMesh:
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
                                    
                                    Matrix shapeTransformMatrixMesh = Matrix.Scaling(new Vector3(1,1,1)) * 
                                                                     Matrix.RotationQuaternion(shape.Transform.Rotation) * 
                                                                     Matrix.Translation(shape.Transform.Position);
                                    
                                    Matrix transformMatrixMesh = shapeTransformMatrixMesh * actorTransformMatrix;
                                    foreach (var submesh in collisionMesh.SubMeshes)
                                    {
                                        OrientedBoundingBox obb = new(submesh.BoundingBox);
                                        obb.Transform(transformMatrixMesh);
                                        foreach (var corner in obb.GetCorners())
                                        {
                                            min = Vector3.Min(min, corner);
                                            max = Vector3.Max(max, corner);
                                        }
                                    }
                                    break;
                                case Enums.physicsShapeType.Box:
                                    Matrix shapeTransformMatrixBox = Matrix.Scaling(new Vector3(1,1,1)) * 
                                                                  Matrix.RotationQuaternion(shape.Transform.Rotation) * 
                                                                  Matrix.Translation(shape.Transform.Position);

                                    Matrix transformMatrixBox = shapeTransformMatrixBox * actorTransformMatrix;
                                    
                                    OrientedBoundingBox collisionBox = new(-shape.Transform.Scale, shape.Transform.Scale);
                                    collisionBox.Transform(transformMatrixBox);
                                    foreach (var corner in collisionBox.GetCorners())
                                    {
                                        min = Vector3.Min(min, corner);
                                        max = Vector3.Max(max, corner);
                                    }
                                    break;
                                case Enums.physicsShapeType.Capsule:
                                    float height = shape.Transform.Scale.Y + 2 * actor.Transform.Scale.X;   
                                    Vector3 shapeSizeAsBox = new Vector3(shape.Transform.Scale.X, shape.Transform.Scale.X, height / 2f);
                                    
                                    Matrix shapeTransformMatrixCapsule = Matrix.Scaling(new Vector3(1,1,1)) * 
                                                                  Matrix.RotationQuaternion(shape.Transform.Rotation) * 
                                                                  Matrix.Translation(shape.Transform.Position);

                                    Matrix transformMatrixCapsule = shapeTransformMatrixCapsule * actorTransformMatrix;
        
                                    OrientedBoundingBox capsuleObb = new(-shapeSizeAsBox, shapeSizeAsBox);
                                    capsuleObb.Transform(transformMatrixCapsule);
                                    foreach (var corner in capsuleObb.GetCorners())
                                    {
                                        min = Vector3.Min(min, corner);
                                        max = Vector3.Max(max, corner);
                                    }
                                    break;
                                case Enums.physicsShapeType.Sphere:
                                    Matrix shapeTransformMatrixSphere = Matrix.Scaling(new Vector3(1,1,1)) * 
                                                                         Matrix.RotationQuaternion(shape.Transform.Rotation) * 
                                                                         Matrix.Translation(shape.Transform.Position);

                                    Matrix transformMatrixSphere = shapeTransformMatrixSphere * actorTransformMatrix;
                                    
                                    OrientedBoundingBox sphereObb = new(new Vector3(-shape.Transform.Scale.X), new Vector3(shape.Transform.Scale.X));
                                    sphereObb.Transform(transformMatrixSphere);
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
                _cacheService.WriteEntry(new WriteRequest(sectorPath, MessagePackSerializer.Serialize(bb), (CacheDatabases)database));
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

    public enum BuildBoundsMode
    {
        All,
        Vanilla,
        RebuildModded,
        MissingModded
    }
    public async Task BuildBounds(BuildBoundsMode mode)
    {
        if (!_gameFileService.IsInitialized)
            throw new Exception("Game file service is not initialized!");
        
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
                var cachedModdedBounds = _cacheService.GetAllEntries(CacheDatabases.ModdedBounds).Select(x => x.Key).ToList();;
                var archiveModdedSectors = _gameFileService.ArchiveManager.GetModArchives().SelectMany(x => x.Files.Values.Where(y => y.Extension == ".streamingsector").Select(y => y.FileName)).ToList();
                moddedSectors = archiveModdedSectors.Except(cachedModdedBounds).ToList();
                break;
        }
        _progress.AddTarget(vanillaSectors.Count + moddedSectors.Count, Progress.ProgressSections.Processing);

        try
        {
            CacheService.Instance.StartListening();
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "Failed to start listening to write requests in cache service!");
            return;
        }
        
        List<Task> tasks = vanillaSectors.Select(x => Task.Run(() => ProcessStreamingsector(x, CacheDatabases.VanillaBounds))).ToList();
        tasks.AddRange(moddedSectors.Select(x =>
            Task.Run(() => ProcessStreamingsector(x, CacheDatabases.ModdedBounds))));
        await Task.WhenAll(tasks);
        
        CacheService.Instance.StopListening();
        if (mode == BuildBoundsMode.Vanilla || mode == BuildBoundsMode.All) 
            _cacheService.SetMetaDataVanillaBoundsStatus(true);
        Logger.Info("Finished building bounds!");
    }
}