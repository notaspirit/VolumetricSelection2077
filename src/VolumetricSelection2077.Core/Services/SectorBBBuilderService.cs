using MessagePack;
using SharpDX;
using VolumetricSelection2077.Core.Components;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;
using WEnums = WolvenKit.RED4.Types.Enums;
using Vector3 = SharpDX.Vector3;
using Result = VolumetricSelection2077.Core.Models.Result;

namespace VolumetricSelection2077.Core.Services;

/// <summary>
/// Builds accurate bounding boxes for streaming sectors and writes them to the sector bounds cache.
/// </summary>
public class SectorBBBuilderService
{
    private const string StartupProgressSection = "BoundingBoxBuilderStartup";
    private const string ProcessingProgressSection = "BoundingBoxBuilderProcessing";

    private readonly CacheService _cacheService;
    private readonly GameFileService _gameFileService;
    private readonly IProgressService _progressService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SectorBBBuilderService"/> class.
    /// </summary>
    /// <param name="cacheService">Cache service used to enumerate and write sector bounds.</param>
    /// <param name="gameFileService">Game file service used to load sectors, meshes, and PhysX collision meshes.</param>
    /// <param name="progressService">Progress service used to track progress.</param>
    public SectorBBBuilderService(CacheService cacheService,
        GameFileService gameFileService,
        IProgressService progressService)
    {
        _cacheService = cacheService;
        _gameFileService = gameFileService;
        _progressService = progressService;
    }

    /// <summary>
    /// Builds bounding boxes for all sectors matching the provided mode.
    /// </summary>
    /// <param name="mode">The sectors to build bounds for.</param>
    /// <returns>
    /// A result with status <see cref="ResultStatus.Error"/> if the services are not initialized or an
    /// unexpected error occurs, <see cref="ResultStatus.Warning"/> if one or more sectors fail or produce
    /// warnings, and <see cref="ResultStatus.Ok"/> otherwise.
    /// </returns>
    public async Task<Result> BuildBounds(BuildBoundsMode mode)
    {
        if (!_gameFileService.IsInitialized)
            return new Result(ResultStatus.Error, "Game file service is not initialized!");

        if (!_cacheService.IsInitialized)
            return new Result(ResultStatus.Error, "Cache service is not initialized!");

        var result = new Result
        {
            Status = ResultStatus.Ok
        };

        try
        {
            _progressService.Reset();
            _progressService.AddSection(new ProgressSection(StartupProgressSection, 1, true));
            _progressService.AddSection(new ProgressSection(ProcessingProgressSection, 8));
            _progressService.AddTarget(StartupProgressSection, 1);

            List<string> vanillaSectors = [];
            List<string> moddedSectors = [];

            switch (mode)
            {
                case BuildBoundsMode.All:
                    vanillaSectors = GetVanillaSectors();
                    moddedSectors = GetModdedSectors();
                    break;
                case BuildBoundsMode.Vanilla:
                    vanillaSectors = GetVanillaSectors();
                    break;
                case BuildBoundsMode.RebuildModded:
                    moddedSectors = GetModdedSectors();
                    break;
                case BuildBoundsMode.MissingModded:
                    var cachedModdedBounds = _cacheService.GetAllEntries(CacheDatabase.ModdedBounds)
                        ?.Select(x => x.Key).ToList() ?? [];
                    moddedSectors = GetModdedSectors().Except(cachedModdedBounds).ToList();
                    break;
            }

            var sectors = vanillaSectors.Distinct()
                .Select(x => (Path: x, Database: CacheDatabase.VanillaBounds))
                .Concat(moddedSectors.Distinct()
                    .Select(x => (Path: x, Database: CacheDatabase.ModdedBounds)))
                .ToList();

            _progressService.AddTarget(ProcessingProgressSection, (uint)sectors.Count);
            _progressService.AddProgress(StartupProgressSection, 1);

            _cacheService.StartListening();

            Task<Result>[] tasks;
            try
            {
                tasks = sectors.Select(s => Task.Run(() => ProcessStreamingsector(s.Path, s.Database))).ToArray();
                await Task.WhenAll(tasks);
            }
            finally
            {
                _cacheService.StopListening();
            }

            for (var i = 0; i < tasks.Length; i++)
            {
                var sectorResult = tasks[i].Result;
                switch (sectorResult.Status)
                {
                    case ResultStatus.Error:
                        result.Status = ResultStatus.Warning;
                        result.AddMessage($"Failed to build bounds for {sectors[i].Path}: {sectorResult.Message}");
                        break;
                    case ResultStatus.Warning:
                        result.Status = ResultStatus.Warning;
                        result.AddMessage($"Warning when building bounds for {sectors[i].Path}: {sectorResult.Message}");
                        break;
                }
            }

            if (mode is BuildBoundsMode.Vanilla or BuildBoundsMode.All)
                _cacheService.SetMetaDataVanillaBoundsStatus(true);

            return result;
        }
        catch (Exception e)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage($"Failed to build sector bounds: {e.Message}");
            return result;
        }
    }

    /// <summary>
    /// Builds an accurate bounding box for the given sector and writes it to the given database.
    /// </summary>
    /// <param name="sectorPath">Path to the sector within the archives.</param>
    /// <param name="database">Database to save the output to.</param>
    /// <returns>
    /// A result with status <see cref="ResultStatus.Error"/> if the sector cannot be loaded or an unexpected
    /// error occurs, <see cref="ResultStatus.Warning"/> if part of the sector could not be resolved and only
    /// position data was used, and <see cref="ResultStatus.Ok"/> otherwise.
    /// </returns>
    private async Task<Result> ProcessStreamingsector(string sectorPath, CacheDatabase? database)
    {
        var result = new Result
        {
            Status = ResultStatus.Ok
        };

        try
        {
            var sectorResult = _gameFileService.GetResource<VSSector>(sectorPath);
            if (sectorResult.Status == ResultStatus.Error || sectorResult.Data == null)
            {
                result.Status = ResultStatus.Error;
                result.AddMessage($"Failed to get sector {sectorPath}: {sectorResult.Message}");
                return result;
            }

            if (sectorResult.Status == ResultStatus.Warning)
            {
                result.Status = ResultStatus.Warning;
                if (!string.IsNullOrEmpty(sectorResult.Message))
                    result.AddMessage(sectorResult.Message);
            }

            var sector = sectorResult.Data;
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);

            foreach (var nodeDataEntry in sector.NodeData)
            {
                var node = sector.Nodes[nodeDataEntry.NodeIndex];

                if ((node.ResourcePath?.EndsWith(".mesh", StringComparison.Ordinal) ?? false) ||
                    (node.ResourcePath?.EndsWith(".w2mesh", StringComparison.Ordinal) ?? false))
                {
                    var meshResult = _gameFileService.GetResource<VSMesh>(node.ResourcePath!);
                    if (meshResult.Status == ResultStatus.Error || meshResult.Data == null)
                    {
                        result.Status = ResultStatus.Warning;
                        result.AddMessage($"Failed to get mesh {node.ResourcePath}, using only position data: {meshResult.Message}");
                        foreach (var transform in nodeDataEntry.Transforms)
                        {
                            min = Vector3.Min(min, transform.Position);
                            max = Vector3.Max(max, transform.Position);
                        }
                        continue;
                    }

                    var mesh = meshResult.Data;
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
                }
                else if (node.Actors != null)
                {
                    foreach (var actor in node.Actors)
                    {
                        var actorTransformMatrix = Matrix.Scaling(actor.Transform.Scale) *
                                                   Matrix.RotationQuaternion(actor.Transform.Rotation) *
                                                   Matrix.Translation(actor.Transform.Position);

                        foreach (var shape in actor.Shapes)
                        {
                            var shapeTransformMatrix = Matrix.Scaling(new Vector3(1, 1, 1)) *
                                                       Matrix.RotationQuaternion(shape.Transform.Rotation) *
                                                       Matrix.Translation(shape.Transform.Position);
                            var summedTransformMatrix = shapeTransformMatrix * actorTransformMatrix;
                            switch (shape.ShapeType)
                            {
                                case WEnums.physicsShapeType.TriangleMesh:
                                case WEnums.physicsShapeType.ConvexMesh:
                                    if (node.SectorHash is not ulong sectorHash ||
                                        shape.Hash is not ulong shapeHash)
                                    {
                                        result.Status = ResultStatus.Warning;
                                        result.AddMessage($"Node data entry contains invalid sector hash {node.SectorHash} or shape hash {shape.Hash}, using only position data");
                                        AddPositionBounds(ref min, ref max, shape.Transform.Position + actor.Transform.Position);
                                        continue;
                                    }

                                    var collisionMeshResult = await _gameFileService.GetPhysXMesh(sectorHash, shapeHash);
                                    if (collisionMeshResult.Status == ResultStatus.Error || collisionMeshResult.Data == null)
                                    {
                                        result.Status = ResultStatus.Warning;
                                        result.AddMessage($"Failed to get collision mesh at sector hash {sectorHash} : shape hash {shapeHash}, using only position data: {collisionMeshResult.Message}");
                                        AddPositionBounds(ref min, ref max, shape.Transform.Position + actor.Transform.Position);
                                        continue;
                                    }

                                    foreach (var submesh in collisionMeshResult.Data.SubMeshes)
                                    {
                                        var obb = new OrientedBoundingBox(submesh.BoundingBox);
                                        obb.Transform(summedTransformMatrix);
                                        AddObbBounds(ref min, ref max, obb);
                                    }
                                    break;
                                case WEnums.physicsShapeType.Box:
                                    var collisionBox = new OrientedBoundingBox(-shape.Transform.Scale, shape.Transform.Scale);
                                    collisionBox.Transform(summedTransformMatrix);
                                    AddObbBounds(ref min, ref max, collisionBox);
                                    break;
                                case WEnums.physicsShapeType.Capsule:
                                    var height = shape.Transform.Scale.Y + 2 * actor.Transform.Scale.X;
                                    var shapeSizeAsBox = new Vector3(shape.Transform.Scale.X, shape.Transform.Scale.X, height / 2f);

                                    var capsuleObb = new OrientedBoundingBox(-shapeSizeAsBox, shapeSizeAsBox);
                                    capsuleObb.Transform(summedTransformMatrix);
                                    AddObbBounds(ref min, ref max, capsuleObb);
                                    break;
                                case WEnums.physicsShapeType.Sphere:
                                    var sphereObb = new OrientedBoundingBox(new Vector3(-shape.Transform.Scale.X), new Vector3(shape.Transform.Scale.X));
                                    sphereObb.Transform(summedTransformMatrix);
                                    AddObbBounds(ref min, ref max, sphereObb);
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

            var bb = min == max
                ? new BoundingBox(min - new Vector3(1, 1, 1), max + new Vector3(1, 1, 1))
                : new BoundingBox(min, max);

            if (database != null)
                _cacheService.WriteEntry(new WriteCacheRequest(sectorPath, MessagePackSerializer.Serialize(bb), database.Value));

            return result;
        }
        catch (Exception e)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage($"Failed to build bounding box for {sectorPath}: {e.Message}");
            return result;
        }
        finally
        {
            _progressService.AddProgress(ProcessingProgressSection, 1);
        }
    }

    private List<string> GetVanillaSectors() =>
        _gameFileService.ArchiveManager.GetGameArchives()
            .SelectMany(x => x.Files.Values
                .Where(y => y.Extension == ".streamingsector")
                .Select(y => y.FileName))
            .ToList();

    private List<string> GetModdedSectors() =>
        _gameFileService.ArchiveManager.GetModArchives()
            .SelectMany(x => x.Files.Values
                .Where(y => y.Extension == ".streamingsector")
                .Select(y => y.FileName))
            .ToList();

    private static void AddPositionBounds(ref Vector3 min, ref Vector3 max, Vector3 position)
    {
        min = Vector3.Min(min, position);
        max = Vector3.Max(max, position);
    }

    private static void AddObbBounds(ref Vector3 min, ref Vector3 max, OrientedBoundingBox obb)
    {
        foreach (var corner in obb.GetCorners())
        {
            min = Vector3.Min(min, corner);
            max = Vector3.Max(max, corner);
        }
    }
}
