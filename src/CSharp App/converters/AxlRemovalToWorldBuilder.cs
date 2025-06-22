using System.Collections.Generic;
using System.Linq;
using DynamicData;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Models.WorldBuilder.Spawn;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Mesh;
using VolumetricSelection2077.Models.WorldBuilder.Visual;
using VolumetricSelection2077.Services;
using WolvenKit.RED4.Archive.Buffer;
using WolvenKit.RED4.Archive.CR2W;
using WolvenKit.RED4.Types;
using Vector4 = SharpDX.Vector4;

namespace VolumetricSelection2077.Converters;

public class AxlRemovalToWorldBuilder
{
    private GameFileService _gfs;
    private List<string> _warnedTypes;
    
    public AxlRemovalToWorldBuilder()
    {
        _gfs = GameFileService.Instance;
        _warnedTypes = new List<string>();
    }

    public Element Convert(AxlRemovalFile axlFile, string rootName)
    {
        _warnedTypes.Clear();
        
        var root = new PositionableGroup
        {
            Name = rootName
        };

        foreach (var sector in axlFile.Streaming.Sectors)
        {
            if (sector.NodeDeletions.Count <= 0)
                continue;

            var worldSectorCR2W = _gfs?.ArchiveManager?.GetCR2WFile(sector.Path);
            if (worldSectorCR2W is not { RootChunk: worldStreamingSector wse })
            {
                Logger.Warning($"Failed to get sector {sector.Path}, no nodes from that sector will be added.");
                continue;
            }

            AbbrSector? worldSectorAbbr = null;
            
            var sectorGroup = new PositionableGroup
            {
                Name = sector.Path
            };

            foreach (var node in sector.NodeDeletions)
            {
                sectorGroup.Children.AddRange(ConvertNode(node, wse, ref worldSectorAbbr, sector.Path));
            }
            if (sectorGroup.Children.Count > 0)
                root.Children.Add(sectorGroup);
        }
        
        _warnedTypes.Clear();
        return root;
    }
    private List<Element> ConvertNode(AxlRemovalNodeDeletion remNode, worldStreamingSector sector, ref AbbrSector? worldSectorAbbr, string sectorPath)
    {
        var nodeData = sector.NodeData.Data as CArray<worldNodeData>;
        
        var nodeDataEntry = nodeData[remNode.Index];
        var node = sector.Nodes[nodeDataEntry.NodeIndex].Chunk;

        var spawnableElements = new List<Element>();
        
        switch (node)
        {
            case worldWaterPatchNode waterPatchNode:
                if ((string?)waterPatchNode.Mesh.DepotPath is null)
                    return spawnableElements;
                
                var spawnableWaterPatchNode = new SpawnableElement
                {
                    Name = GetSpawnableName(waterPatchNode),
                    Spawnable = new WaterPatch()
                    {
                        Depth = waterPatchNode.Depth
                    }
                };
                PopulateBaseMesh(ref spawnableWaterPatchNode, waterPatchNode, nodeDataEntry);
                spawnableElements.Add(spawnableWaterPatchNode);
                break;
            case worldClothMeshNode clothMeshNode:
                if ((string?)clothMeshNode.Mesh.DepotPath is null)
                    return spawnableElements;
                
                var spawnableClothMeshNode = new SpawnableElement
                {
                    Name = GetSpawnableName(clothMeshNode),
                    Spawnable = new ClothMesh
                    {
                        AffectedByWind = clothMeshNode.AffectedByWind
                    }
                };
            
                PopulateBaseMesh(ref spawnableClothMeshNode, clothMeshNode, nodeDataEntry);
                spawnableElements.Add(spawnableClothMeshNode);
                break;
            case worldRotatingMeshNode rotatingMeshNode:
                if ((string?)rotatingMeshNode.Mesh.DepotPath is null)
                    return spawnableElements;

                var spawnableRotatingMeshNode = new SpawnableElement
                {
                    Name = GetSpawnableName(rotatingMeshNode),
                    Spawnable = new RotatingMesh
                    {
                        Duration = rotatingMeshNode.FullRotationTime,
                        Axis = rotatingMeshNode.RotationAxis,
                        Reverse = rotatingMeshNode.ReverseDirection
                    }
                };
                
                PopulateBaseMesh(ref spawnableRotatingMeshNode, rotatingMeshNode, nodeDataEntry);
                spawnableElements.Add(spawnableRotatingMeshNode);
                break;
            case worldInstancedMeshNode instancedMeshNode:
                if ((string?)instancedMeshNode.Mesh.DepotPath is null)
                    return spawnableElements;

                worldSectorAbbr ??= _gfs.GetSector(sectorPath);
                if (worldSectorAbbr == null)
                    return spawnableElements;
                var abbrInstancedMeshNodeDataEntry = worldSectorAbbr.NodeData[remNode.Index];
                foreach (var transform in abbrInstancedMeshNodeDataEntry.Transforms)
                {
                    if (remNode.ActorDeletions != null && remNode.ActorDeletions.Count != 0)
                    {
                        if (remNode.ActorDeletions.All(x => x != abbrInstancedMeshNodeDataEntry.Transforms.IndexOf(transform)))
                            continue;
                    }
                    
                    spawnableElements.Add(new SpawnableElement
                    {
                        Name = GetSpawnableName(instancedMeshNode),
                        Spawnable = new Mesh
                        {
                            Position = new Vector4(transform.Position, 1),
                            EulerRotation = transform.Rotation,
                            Scale = transform.Scale,
                    
                            CastLocalShadows = instancedMeshNode.CastLocalShadows,
                            CastShadows = instancedMeshNode.CastShadows,
                    
                            PrimaryRange = nodeDataEntry.MaxStreamingDistance,
                            SecondaryRange = nodeDataEntry.UkFloat1,
                            Uk10 = nodeDataEntry.Uk10,
                            Uk11 = nodeDataEntry.Uk11,
                    
                            ResourcePath = instancedMeshNode.Mesh.DepotPath,
                            Appearance = instancedMeshNode.MeshAppearance,
                        }
                    });
                }
                break;
            case worldMeshNode meshNode:
                if ((string?)meshNode.Mesh.DepotPath is null)
                    return spawnableElements;

                var spawnableMeshNode = new SpawnableElement
                {
                    Name = GetSpawnableName(meshNode),
                    Spawnable = new Mesh()
                };
                
                PopulateBaseMesh(ref spawnableMeshNode, meshNode, nodeDataEntry);
                
                spawnableElements.Add(spawnableMeshNode);
                break;
            default:
                var nodeTypeString = node?.GetType().ToString() ?? "";
                if (!_warnedTypes.Contains(nodeTypeString))
                {
                    Logger.Warning($"{nodeTypeString} is not a supported node type. Skipping...");
                    _warnedTypes.Add(nodeTypeString);
                }
                break;
        }
        return spawnableElements;
    }

    private static void PopulateBaseMesh(ref SpawnableElement se, worldMeshNode meshNode, worldNodeData nodeDataEntry)
    {
        var mesh = (Mesh)se.Spawnable;
        mesh.Scale = WolvenkitToSharpDX.Vector3(nodeDataEntry.Scale);

        mesh.CastLocalShadows = meshNode.CastLocalShadows;
        mesh.CastRayTracedLocalShadows = meshNode.CastRayTracedLocalShadows;
        mesh.CastRayTracedGlobalShadows = meshNode.CastRayTracedGlobalShadows;
        mesh.CastShadows = meshNode.CastShadows;

        mesh.WindImpulseEnabled = meshNode.WindImpulseEnabled;
        
        PopulateSpawnable(ref se, meshNode, nodeDataEntry);
    }
    
    private static void PopulateSpawnable(ref SpawnableElement se, worldMeshNode meshNode, worldNodeData nodeDataEntry)
    {
        se.Spawnable.Position = WolvenkitToSharpDX.Vector4(nodeDataEntry.Position);
        se.Spawnable.EulerRotation = WolvenkitToSharpDX.Quaternion(nodeDataEntry.Orientation);

        se.Spawnable.PrimaryRange = nodeDataEntry.MaxStreamingDistance;
        se.Spawnable.SecondaryRange = nodeDataEntry.UkFloat1;
        se.Spawnable.Uk10 = nodeDataEntry.Uk10;
        se.Spawnable.Uk11 = nodeDataEntry.Uk11;

        se.Spawnable.ResourcePath = meshNode.Mesh.DepotPath;
        se.Spawnable.Appearance = meshNode.MeshAppearance;

    }

    private static string GetSpawnableName(worldNode node)
    {
        switch (node)
        {
            case worldMeshNode meshNode:
                return meshNode?.DebugName ?? meshNode?.Mesh.DepotPath.GetString() ?? "Generated by VS2077";
            default:
                return node?.DebugName?? "Generated by VS2077";
        }
    }
}