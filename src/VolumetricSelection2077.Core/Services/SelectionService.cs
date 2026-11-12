using MessagePack;
using SharpDX;
using VolumetricSelection2077.Core.Components;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Extensions;
using VolumetricSelection2077.Core.Models;
using WEnums = WolvenKit.RED4.Types.Enums;

namespace VolumetricSelection2077.Core.Services;

/// <summary>
/// Selects world nodes that intersect a query volume and produces the corresponding AXL removal data.
/// </summary>
/// <remarks>
/// Sectors and nodes are queried in parallel. Depending on the node type, intersection is determined by
/// mesh collision checks, PhysX collision shapes, or node transform positions.
/// </remarks>
public class SelectionService : ISelectionService
{
    private const string SectorBoundsFiltering = "SectorBoundsFiltering";
    private const string SectorQuerying = "SectorQuerying";
    
    /// <summary>
    /// Node types whose mesh instances are reported as actor deletions.
    /// </summary>
    private static readonly string[] InstancedMeshNodes = [
        "worldInstancedMeshNode",
        "worldInstancedDestructibleMeshNode",
        "worldFoliageNode"
    ];
    
    private readonly ICacheService _cacheService;
    private readonly IGameFileService _gameFileService;
    private readonly IProgressService _progressService;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectionService"/> class.
    /// </summary>
    /// <param name="cacheService">Cache service used to enumerate and listen for sector bounds.</param>
    /// <param name="gameFileService">Game file service used to load sectors, meshes, and PhysX collision meshes.</param>
    /// <param name="progressService">Progress service used to track progress.</param>
    public SelectionService(ICacheService cacheService,
        IGameFileService gameFileService,
        IProgressService progressService)
    {
        _cacheService = cacheService;
        _gameFileService = gameFileService;
        _progressService = progressService;
    }
    
    /// <summary>
    /// Queries all cached sectors that intersect the query volume and builds a complete AXL removal file.
    /// </summary>
    /// <param name="query">The selection query containing the bounds, data source, and node filters.</param>
    /// <returns>
    /// A result containing the removal file. The status is <see cref="ResultStatus.Warning"/> when one or
    /// more sectors fail or produce warnings; <c>Data</c> is only set when at least one sector has deletions.
    /// </returns>
    public async Task<Result<AxlRemovalFile>> QueryWorldData(VSQuery query)
    {
        var result = new Result<AxlRemovalFile>
        {
            Status = ResultStatus.Ok
        };
        _progressService.Reset();
        _progressService.AddSection(new ProgressSection(SectorBoundsFiltering, 1, true));
        _progressService.AddSection(new ProgressSection(SectorQuerying, 8, true));
        _progressService.AddTarget(SectorBoundsFiltering, 1);
        var startListeningResult = _cacheService.StartListening();
        if (startListeningResult.Status == ResultStatus.Error)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage(startListeningResult.Message);
            return result;
        }

        var cachedEntriesResult = _cacheService.GetAllEntries(query.DataSource.ToCacheDatabaseBounds());
        if (cachedEntriesResult.Status == ResultStatus.Error || cachedEntriesResult.Data == null)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage($"Failed to get cached sectors: {cachedEntriesResult.Message}");
            _cacheService.StopListening();
            return result;
        }

        var localSectors = cachedEntriesResult.Data
            .Where(s =>
                query.AABB.Contains(MessagePackSerializer.Deserialize<BoundingBox>(s.Value)) != ContainmentType.Disjoint)
            .Select(s => s.Key).ToArray();
        
        _progressService.AddTarget(SectorQuerying, (uint)localSectors.Length);
        _progressService.AddProgress(SectorBoundsFiltering, 1);
        
        var sectorResults = 
            localSectors.Select(sectorPath => Task.Run(() =>
            {
                var res = QuerySector(sectorPath, query);
                _progressService.AddProgress(SectorQuerying, 1);
                return res;
            })).ToArray();
        await Task.WhenAll(sectorResults);

        var removalFile = new AxlRemovalFile
        {
            Streaming = new AxlRemovalStreaming
            {
                Sectors = []
            }
        };
        
        var i = 0;
        foreach (var sectorResult in sectorResults)
        {
            switch (sectorResult.Result.Status)
            {
                case ResultStatus.Error:
                    result.Status = ResultStatus.Warning;
                    result.AddMessage($"Failed to query sector with path {localSectors[i]}: {sectorResult.Result.Message}");
                    break;
                case ResultStatus.Warning:
                    result.Status = ResultStatus.Warning;
                    result.AddMessage($"Warning when querying sector with path {localSectors[i]}: {sectorResult.Result.Message}");
                    break;
                case ResultStatus.Ok when sectorResult.Result.Data != null:
                    removalFile.Streaming.Sectors.Add(sectorResult.Result.Data);
                    break;
            }

            i++;
        }
        
        if (removalFile.Streaming.Sectors.Count > 0)
            result.Data = removalFile;
        
        var stopListeningResult = _cacheService.StopListening();
        if (stopListeningResult.Status == ResultStatus.Error)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage(stopListeningResult.Message);
        }
        
        return result;
    }
    
    /// <summary>
    /// Queries a single streaming sector and builds the AXL removal data for its matching nodes.
    /// </summary>
    /// <param name="sectorPath">Path of the sector resource to query.</param>
    /// <param name="query">The selection query containing the bounds and node filters.</param>
    /// <returns>
    /// A result containing the sector removal data. The status is <see cref="ResultStatus.Error"/> if the
    /// sector cannot be loaded, <see cref="ResultStatus.Warning"/> if any node query fails or warns, and
    /// <c>Data</c> is only set when at least one node is deleted.
    /// </returns>
    public async Task<Result<AxlRemovalSector>> QuerySector(string sectorPath, VSQuery query)
    {
        var result = new Result<AxlRemovalSector>
        {
            Status = ResultStatus.Ok
        };

        var sectorResult = _gameFileService.GetResource<VSSector>(sectorPath);
        if (sectorResult.Status == ResultStatus.Error || sectorResult.Data == null)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage($"Failed to get sector {sectorPath}: {sectorResult.Message}");
            return result;
        }

        var removalSector = new AxlRemovalSector
        {
            ExpectedNodes = sectorResult.Data.NodeData.Length,
            Path = sectorPath,
            NodeDeletions = []
        };

        var nodeResults =
            sectorResult.Data.NodeData.Select((nodeData, i) => Task.Run(() => QueryNode(nodeData, sectorResult.Data.Nodes[i], i, query))).ToArray();
        await Task.WhenAll(nodeResults);

        var i = 0;
        foreach (var nodeResult in nodeResults)
        {
            switch (nodeResult.Result.Status)
            {
                case ResultStatus.Error:
                    result.Status = ResultStatus.Warning;
                    result.AddMessage($"Failed to query node at index {i}: {nodeResult.Result.Message}");
                    break;
                case ResultStatus.Warning:
                    result.Status = ResultStatus.Warning;
                    result.AddMessage($"Warning when querying node at index {i}: {nodeResult.Result.Message}");
                    break;
                case ResultStatus.Ok when nodeResult.Result.Data != null:
                    removalSector.NodeDeletions.Add(nodeResult.Result.Data);
                    break;
            }

            i++;
        }
        
        if (removalSector.NodeDeletions.Count > 0)
            result.Data = removalSector;
        
        return result;
    }

    /// <summary>
    /// Queries a single node and determines whether it should be removed.
    /// </summary>
    /// <param name="nodeData">Transform and bounds data for the node.</param>
    /// <param name="node">Node metadata such as type, resource path, and collision actors.</param>
    /// <param name="nodeDataIndex">Index of the node data entry within the sector.</param>
    /// <param name="query">The selection query containing the bounds and node filters.</param>
    /// <returns>
    /// A result whose <c>Data</c> is set when the node should be removed. The result is empty when the node
    /// is filtered out or does not intersect the query volume.
    /// </returns>
    /// <remarks>
    /// Dispatches to the mesh, collision actor, or transform based query depending on the node.
    /// </remarks>
    private async Task<Result<AxlRemovalNodeDeletion>> QueryNode(VSNodeData nodeData,
        VSNode node,
        int nodeDataIndex,
        VSQuery query)
    {
        if (!ShouldProcessNode(nodeData, node, nodeDataIndex, query))
            return new Result<AxlRemovalNodeDeletion> { Status = ResultStatus.Ok };

        if (IsMeshNode(node))
            return QueryMeshNode(nodeData, node, nodeDataIndex, query);

        if (node is { SectorHash: not null, Actors.Length: > 0 })
            return await QueryActorNode(nodeData, node, nodeDataIndex, query);

        return QueryTransformNode(nodeData, node, nodeDataIndex, query);
    }

    /// <summary>
    /// Determines whether a node references a mesh resource.
    /// </summary>
    /// <param name="node">Node metadata to inspect.</param>
    /// <returns>
    /// <c>true</c> if the resource path ends with <c>.mesh</c> or <c>.w2mesh</c>; otherwise <c>false</c>.
    /// </returns>
    private static bool IsMeshNode(VSNode node)
    {
        return (node.ResourcePath?.EndsWith(@".mesh", StringComparison.Ordinal) ?? false) ||
               (node.ResourcePath?.EndsWith(@".w2mesh", StringComparison.Ordinal) ?? false);
    }

    /// <summary>
    /// Queries a mesh node by testing its mesh instances against the selection volume.
    /// </summary>
    /// <param name="nodeData">Transform and bounds data for the node.</param>
    /// <param name="node">Node metadata containing the mesh resource path and type.</param>
    /// <param name="nodeDataIndex">Index of the node data entry within the sector.</param>
    /// <param name="query">The selection query containing the bounds.</param>
    /// <returns>
    /// A result whose <c>Data</c> contains the node deletion when any mesh instance intersects the query.
    /// The result is empty when the node's bounds or instances do not intersect the query volume.
    /// </returns>
    /// <remarks>
    /// Returns a <see cref="ResultStatus.Error"/> result when the mesh resource cannot be loaded.
    /// </remarks>
    private Result<AxlRemovalNodeDeletion> QueryMeshNode(VSNodeData nodeData,
        VSNode node,
        int nodeDataIndex,
        VSQuery query)
    {
        var result = new Result<AxlRemovalNodeDeletion>
        {
            Status = ResultStatus.Ok
        };

        if (nodeData.AABB != null)
        {
            var nodeDataAABB = (BoundingBox)nodeData.AABB;
            if (query.OBB.Contains(ref nodeDataAABB) == ContainmentType.Disjoint)
                return result;
        }

        var meshResult = _gameFileService.GetResource<VSMesh>(node.ResourcePath!);
        if (meshResult.Status == ResultStatus.Error || meshResult.Data == null)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage($"Failed to get mesh {node.ResourcePath}: {meshResult.Message}");
            return result;
        }

        var matchingInstances = CollisionCheckService.IsMeshInsideBox(meshResult.Data,
            query.OBB,
            query.AABB,
            nodeData.Transforms);

        if (matchingInstances.Count == 0)
            return result;

        result.Data = new AxlRemovalNodeDeletion
        {
            Type = node.Type,
            Index = nodeDataIndex
        };

        if (InstancedMeshNodes.Contains(node.Type))
        {
            result.Data.ActorDeletions = matchingInstances;
            result.Data.ExpectedActors = nodeData.Transforms.Length;
        }

        return result;
    }

    /// <summary>
    /// Queries a collision node by testing each actor's collision shapes against the selection volume.
    /// </summary>
    /// <param name="nodeData">Transform and bounds data for the node.</param>
    /// <param name="node">Node metadata containing the collision actors and sector hash.</param>
    /// <param name="nodeDataIndex">Index of the node data entry within the sector.</param>
    /// <param name="query">The selection query containing the bounds.</param>
    /// <returns>
    /// A result whose <c>Data</c> contains the node deletion with the matching actor indices. The result is
    /// empty when no actor intersects the query volume.
    /// </returns>
    /// <remarks>
    /// Warnings from individual actors are aggregated into the result message, and the result status is
    /// <see cref="ResultStatus.Warning"/> when any actor reports one.
    /// </remarks>
    private async Task<Result<AxlRemovalNodeDeletion>> QueryActorNode(VSNodeData nodeData,
        VSNode node,
        int nodeDataIndex,
        VSQuery query)
    {
        var result = new Result<AxlRemovalNodeDeletion>
        {
            Status = ResultStatus.Ok
        };

        List<int> actorRemovals = [];
        var actorIndex = 0;

        foreach (var actor in node.Actors!)
        {
            var actorResult = await ShouldRemoveActor(node, actor, actorIndex, nodeDataIndex, query);

            if (actorResult.Status == ResultStatus.Warning)
            {
                result.Status = ResultStatus.Warning;
                result.AddMessage(actorResult.Message);
            }

            if (actorResult.Data)
                actorRemovals.Add(actorIndex);

            actorIndex++;
        }

        if (actorRemovals.Count == 0)
            return result;

        result.Data = new AxlRemovalNodeDeletion
        {
            Index = nodeDataIndex,
            Type = node.Type,
            ActorDeletions = actorRemovals,
            ExpectedActors = node.Actors!.Length
        };
        return result;
    }

    /// <summary>
    /// Determines whether a collision actor intersects the selection volume.
    /// </summary>
    /// <param name="node">Node that owns the actor; its sector hash is used to load PhysX meshes.</param>
    /// <param name="actor">Collision actor to test.</param>
    /// <param name="actorIndex">Index of the actor within the node, used in warning messages.</param>
    /// <param name="nodeDataIndex">Index of the node data entry, used in warning messages.</param>
    /// <param name="query">The selection query containing the bounds.</param>
    /// <returns>
    /// A result whose <c>Data</c> is <c>true</c> when any shape intersects the selection volume, otherwise
    /// <c>false</c>. Failed collision mesh loads produce a <see cref="ResultStatus.Warning"/> with the
    /// details in the message.
    /// </returns>
    private async Task<Result<bool>> ShouldRemoveActor(VSNode node,
        VSCollisionActor actor,
        int actorIndex,
        int nodeDataIndex,
        VSQuery query)
    {
        var result = new Result<bool>
        {
            Status = ResultStatus.Ok,
            Data = false
        };

        var shapeIndex = 0;
        foreach (var shape in actor.Shapes)
        {
            switch (shape.ShapeType)
            {
                case WEnums.physicsShapeType.TriangleMesh:
                case WEnums.physicsShapeType.ConvexMesh:
                    var collisionMeshResult = await _gameFileService.GetPhysXMesh((ulong)node.SectorHash!, (ulong)shape.Hash!);
                    if (collisionMeshResult.Status == ResultStatus.Error || collisionMeshResult.Data == null)
                    {
                        result.Status = ResultStatus.Warning;
                        result.AddMessage($"Failed to get collision mesh for actor {actorIndex} shape index {shapeIndex} in node {nodeDataIndex}: {collisionMeshResult.Message}");
                        break;
                    }

                    if (CollisionCheckService.IsCollisonMeshInsideSelectionBox(collisionMeshResult.Data, query.OBB, query.AABB, actor.Transform, shape.Transform))
                    {
                        result.Data = true;
                        return result;
                    }
                    break;
                case WEnums.physicsShapeType.Box:
                    if (CollisionCheckService.IsCollisionBoxInsideSelectionBox(shape, actor.Transform, query.AABB, query.OBB))
                    {
                        result.Data = true;
                        return result;
                    }
                    break;
                case WEnums.physicsShapeType.Capsule:
                    if (CollisionCheckService.IsCollisionCapsuleInsideSelectionBox(shape, actor.Transform, query.AABB, query.OBB))
                    {
                        result.Data = true;
                        return result;
                    }
                    break;
                case WEnums.physicsShapeType.Sphere:
                    if (CollisionCheckService.IsCollisionSphereInsideSelectionBox(shape, actor.Transform, query.OBB))
                    {
                        result.Data = true;
                        return result;
                    }
                    break;
            }
            shapeIndex++;
        }

        return result;
    }

    /// <summary>
    /// Queries a node without mesh or collision data by testing its transforms against the selection volume.
    /// </summary>
    /// <param name="nodeData">Transform data for the node.</param>
    /// <param name="node">Node metadata containing the node type.</param>
    /// <param name="nodeDataIndex">Index of the node data entry within the sector.</param>
    /// <param name="query">The selection query containing the bounds.</param>
    /// <returns>
    /// A result whose <c>Data</c> contains the node deletion when any transform position is inside the query
    /// volume; otherwise the result is empty.
    /// </returns>
    private static Result<AxlRemovalNodeDeletion> QueryTransformNode(VSNodeData nodeData,
        VSNode node,
        int nodeDataIndex,
        VSQuery query)
    {
        var result = new Result<AxlRemovalNodeDeletion>
        {
            Status = ResultStatus.Ok
        };

        foreach (var transform in nodeData.Transforms)
        {
            if (query.OBB.Contains(transform.Position) == ContainmentType.Disjoint)
                continue;

            result.Data = new AxlRemovalNodeDeletion
            {
                Index = nodeDataIndex,
                Type = node.Type
            };
            return result;
        }

        return result;
    }
    
    /// <summary>
    /// Determines whether a node passes the query's node filters.
    /// </summary>
    /// <param name="nodeData">Transform and bounds data for the node.</param>
    /// <param name="node">Node metadata to evaluate.</param>
    /// <param name="nodeDataIndex">Index of the node data entry within the sector.</param>
    /// <param name="query">The selection query containing the filters.</param>
    /// <returns>
    /// <c>true</c> when no filters are configured, or when all required filters pass and at least one
    /// non-required filter matches; otherwise <c>false</c>.
    /// </returns>
    private static bool ShouldProcessNode(VSNodeData nodeData, VSNode node, int nodeDataIndex, VSQuery query)
    {
        if (query.Filters.Count == 0)
            return true;
        
        if (query.Filters.Where(f => f.Required).Any(f => !f.Filter(nodeData, node, nodeDataIndex)))
            return false;

        if (query.Filters.Where(f => !f.Required).Any(f => f.Filter(nodeData, node, nodeDataIndex)))
            return true;
        
        return false;
    }
}
