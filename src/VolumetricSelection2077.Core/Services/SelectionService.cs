using MessagePack;
using SharpDX;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Extensions;
using VolumetricSelection2077.Core.Models;
using WEnums = WolvenKit.RED4.Types.Enums;

namespace VolumetricSelection2077.Core.Services;

public class SelectionService : ISelectionService
{
    private static readonly string[] InstancedMeshNodes = [
        "worldInstancedMeshNode",
        "worldInstancedDestructibleMeshNode",
        "worldFoliageNode"
    ];

    private readonly CacheService _cacheService;
    private readonly GameFileService _gameFileService;
    
    public SelectionService(CacheService cacheService,
        GameFileService gameFileService)
    {
        _cacheService = cacheService;
        _gameFileService = gameFileService;
    }
    
    public async Task<Result<AxlRemovalFile>> QueryWorldData(VSQuery query)
    {
        var result = new Result<AxlRemovalFile>
        {
            Status = ResultStatus.Ok
        };
        
        _cacheService.StartListening();

        var localSectors = _cacheService.GetAllEntries(query.DataSource.ToCacheDatabaseBounds())?
            .Where(s =>
                query.AABB.Contains(MessagePackSerializer.Deserialize<BoundingBox>(s.Value)) != ContainmentType.Disjoint)
            .Select(s => s.Key).ToArray() ?? [];
        
        var sectorResults = 
            localSectors.Select(sectorPath => Task.Run(() => QuerySector(sectorPath, query))).ToArray();
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
                    result.Message += $"Failed to query sector with path {localSectors[i]}: {sectorResult.Result.Message}\n";
                    break;
                case ResultStatus.Warning:
                    result.Status = ResultStatus.Warning;
                    result.Message += $"Warning when querying sector with path {localSectors[i]}: {sectorResult.Result.Message}\n";
                    break;
                case ResultStatus.Ok when sectorResult.Result.Data != null:
                    removalFile.Streaming.Sectors.Add(sectorResult.Result.Data);
                    break;
            }

            i++;
        }
        
        if (removalFile.Streaming.Sectors.Count > 0)
            result.Data = removalFile;
        
        _cacheService.StopListening();
        
        return result;
    }
    
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
            result.Message = $"Failed to get sector {sectorPath}: {sectorResult.Message}";
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
                    result.Message += $"Failed to query node at index {i}: {nodeResult.Result.Message}\n";
                    break;
                case ResultStatus.Warning:
                    result.Status = ResultStatus.Warning;
                    result.Message += $"Warning when querying node at index {i}: {nodeResult.Result.Message}\n";
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

    private static bool IsMeshNode(VSNode node)
    {
        return (node.ResourcePath?.EndsWith(@".mesh", StringComparison.Ordinal) ?? false) ||
               (node.ResourcePath?.EndsWith(@".w2mesh", StringComparison.Ordinal) ?? false);
    }

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
            result.Message = $"Failed to get mesh {node.ResourcePath}: {meshResult.Message}";
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
                result.Message += actorResult.Message;
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
                        result.Message += $"Failed to get collision mesh for actor {actorIndex} shape index {shapeIndex} in node {nodeDataIndex}: {collisionMeshResult.Message}\n";
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