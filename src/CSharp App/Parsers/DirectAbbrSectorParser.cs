using System;
using System.Linq;
using HelixToolkit.SharpDX.Core;
using MessagePack;
using SharpDX;
using VolumetricSelection2077.Converters.Simple;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;
using WolvenKit.RED4.Archive.Buffer;
using WolvenKit.RED4.Archive.CR2W;
using WolvenKit.RED4.Types;
using Quaternion = SharpDX.Quaternion;
using Vector3 = SharpDX.Vector3;
using worldNodeData = WolvenKit.RED4.Archive.Buffer.worldNodeData;

namespace VolumetricSelection2077.Parsers;

public class DirectAbbrSectorParser
{
    private readonly GameFileService _gameFileService;
    private readonly CacheService _cacheService;
    
    public DirectAbbrSectorParser(GameFileService gameFileService, CacheService cacheService)
    {
        _gameFileService = gameFileService;
        _cacheService = cacheService;
    }
    
    /// <summary>
    /// Parses a CR2W file into an AbbrSector
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="Exception">cr2w file is not a streamingsector</exception>
    public AbbrSector ParseFromCR2W(CR2WFile input)
    {
        if (input.RootChunk is not worldStreamingSector sector)
        {
            throw new Exception("Input file is not a world streaming sector");
        }
    
        foreach (var efile in input.EmbeddedFiles)
        {
            var fileName = efile.FileName.ToString() ?? "";

            if (!fileName.EndsWith(".mesh") && !fileName.EndsWith(".w2mesh"))
                continue;
            
            var parsedMesh = DirectAbbrMeshParser.ParseFromEmbedded(efile);
            if (parsedMesh is null)
            {
                Logger.Warning($"Failed to parse embedded file {efile.FileName}");
                continue;
            }
            _cacheService.WriteSingleEntry(new WriteCacheRequest(fileName, MessagePackSerializer.Serialize(parsedMesh)));
        }
        
        var nodes = new AbbrStreamingSectorNodesEntry[sector.Nodes.Count];
        
        int nodeIndex = 0;
        foreach (var node in sector.Nodes)
        {
            var debugName = node.Chunk?.DebugName;
            var type = node.Chunk?.GetType().Name ?? "Unknown";
            var parsedTypeSuccess = Enum.TryParse(type, out NodeTypeProcessingOptions parsedType);
            if (!parsedTypeSuccess)
                Logger.Error($"Invalid node type: {type}");
            
            ulong? sectorHash = null;
            AbbrCollisionActors[]? actors = null;
            string? resourcePath = null;
            
            switch (node.Chunk)
            {
                case worldMeshNode meshNode:
                    resourcePath = meshNode.Mesh.DepotPath;
                    break;
                case worldInstancedMeshNode instancedMeshNode:
                    resourcePath = instancedMeshNode.Mesh.DepotPath;
                    break;
                case worldInstancedOccluderNode instancedOccluderNode:
                    resourcePath = instancedOccluderNode.Mesh.DepotPath;
                    break;
                case worldTerrainMeshNode terrainMeshNode:
                    resourcePath = terrainMeshNode.MeshRef.DepotPath;
                    break;
                case worldBendedMeshNode bendedMeshNode:
                    resourcePath = bendedMeshNode.Mesh.DepotPath;
                    break;
                case worldPhysicalDestructionNode destructionNode:
                    resourcePath = destructionNode.Mesh.DepotPath;
                    break;
                case worldFoliageNode foliageNode:
                    resourcePath = foliageNode.Mesh.DepotPath;
                    break;
                case worldStaticOccluderMeshNode staticOccluderMeshNode:
                    resourcePath = staticOccluderMeshNode.Mesh.DepotPath;
                    break;
                case worldStaticDecalNode staticDecalNode:
                    resourcePath = staticDecalNode.Material.DepotPath;
                    break;
                case worldEffectNode effectNode:
                    resourcePath = effectNode.Effect.DepotPath;
                    break;
                case worldStaticParticleNode staticParticleNode:
                    resourcePath = staticParticleNode.ParticleSystem.DepotPath;
                    break;
                case worldPrefabNode prefabNode:
                    resourcePath = prefabNode.Prefab.DepotPath;
                    break;
                case worldEntityNode entityNode:
                    resourcePath = entityNode.EntityTemplate.DepotPath;
                    break;
                case worldCollisionNode collisionNode:
                    sectorHash = collisionNode.SectorHash;
                    actors = new AbbrCollisionActors[collisionNode.NumActors];
                    var collisionActorBuffer = collisionNode.CompiledData.Data as CollisionBuffer;
                    int actorIndex = 0;
                    foreach (var actor in collisionActorBuffer.Actors)
                    {
                        var actorPosition = FixedPointVector3Converter.PosBitsToVec3(actor.Position);
                        var actorRotation = WolvenkitToSharpDXConverter.Quaternion(actor.Orientation);
                        var actorScale = WolvenkitToSharpDXConverter.Vector3(actor.Scale);
                        var shapes = new AbbrActorShapes[actor.Shapes.Count];
                        int shapesIndex = 0;
                        foreach (var shape in actor.Shapes)
                        {
                            var shapeTypeString = shape.ShapeType.ToString();
                            var shapePosition = WolvenkitToSharpDXConverter.Vector3(shape.Position);
                            var shapeRotation = WolvenkitToSharpDXConverter.Quaternion(shape.Rotation);
                            var shapeScale = new SharpDX.Vector3(1,1,1);
                            ulong? shapeHash = null;
                            
                            switch (shape)
                            {
                                case CollisionShapeSimple simpleShape:
                                    shapeScale = WolvenkitToSharpDXConverter.Vector3(simpleShape.Size);
                                    break;
                                case CollisionShapeMesh meshShape:
                                    shapeHash = meshShape.Hash;
                                    break;
                            }

                            var parsedShapeType = Enum.TryParse(shapeTypeString, out WolvenKit.RED4.Types.Enums.physicsShapeType shapeType);
                            if (!parsedShapeType)
                                Logger.Error($"Invalid shape type: {shapeTypeString}");
                            
                            shapes[shapesIndex] = new AbbrActorShapes()
                            {
                                ShapeType = shapeType,
                                Hash = shapeHash,
                                Transform = new AbbrSectorTransform()
                                {
                                    Position = shapePosition,
                                    Rotation = shapeRotation,
                                    Scale = shapeScale
                                }
                            };
                            shapesIndex++;
                        }

                        actors[actorIndex] = new AbbrCollisionActors()
                        {
                            Shapes = shapes,
                            Transform = new AbbrSectorTransform()
                            {
                                Position = actorPosition,
                                Rotation = actorRotation,
                                Scale = actorScale
                            }
                        };
                        
                        actorIndex++;
                    }
                    break;
            }

            nodes[nodeIndex] = new AbbrStreamingSectorNodesEntry()
            {
                SectorHash = sectorHash,
                Actors = actors,
                DebugName = debugName,
                ResourcePath = resourcePath,
                Type = parsedType
            };
            
            nodeIndex++;
        }

        var nodeDataBuffer = sector.NodeData.Data as CArray<worldNodeData>;
        var nodeDataOut = new AbbrStreamingSectorNodeDataEntry[nodeDataBuffer.Count];
        
        int nodeDataIndex = 0;
        foreach (var nodeDataEntry in nodeDataBuffer)
        {
            AbbrSectorTransform[] transforms;
            BoundingBox? nodeBoundingBox = null;
            
            switch (sector.Nodes[nodeDataEntry.NodeIndex].Chunk)
            {
                case worldInstancedMeshNode instancedNode:
                    transforms = new AbbrSectorTransform[instancedNode.WorldTransformsBuffer.NumElements];
                    var transformsInstancedBuffer = instancedNode.WorldTransformsBuffer.SharedDataBuffer.Chunk.Buffer.Data as WorldTransformsBuffer;
                    int transformsInstancedIndex = 0;
                    
                    var instancedMeshNodeBoundsMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                    var instancedMeshNodeBoundsMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                    var token = _gameFileService.GetCMesh(instancedNode.Mesh.DepotPath);
                    if (token.Resource is not AbbrMesh instancedMesh)
                        goto DefaultLabel;
                    
                    foreach (var transform in transformsInstancedBuffer.Transforms.ToArray().AsSpan((int)(uint)instancedNode.WorldTransformsBuffer.StartIndex, (int)(uint)instancedNode.WorldTransformsBuffer.NumElements))
                    {
                        transforms[transformsInstancedIndex] = new AbbrSectorTransform()
                        {
                            Position = WolvenkitToSharpDXConverter.Vector3(transform.Translation),
                            Rotation = WolvenkitToSharpDXConverter.Quaternion(transform.Rotation),
                            Scale = WolvenkitToSharpDXConverter.Vector3(transform.Scale)
                        };
                        
                        var transformMatrix = Matrix.Scaling(transform.Scale.ToSDX()) * 
                                              Matrix.RotationQuaternion(transform.Rotation.ToSDX()) *  
                                              Matrix.Translation(transform.Translation.ToSDX());

                        foreach (var submesh in instancedMesh.SubMeshes)
                        {
                            var obb = new OrientedBoundingBox(submesh.BoundingBox);
                            obb.Transform(transformMatrix);
                            
                            foreach (var corner in obb.GetCorners())
                            {
                                instancedMeshNodeBoundsMin = Vector3.Min(instancedMeshNodeBoundsMin, corner);
                                instancedMeshNodeBoundsMax = Vector3.Max(instancedMeshNodeBoundsMax, corner);
                            }
                        }
                        transformsInstancedIndex++;
                    }
                    
                    nodeBoundingBox = new BoundingBox(instancedMeshNodeBoundsMin, instancedMeshNodeBoundsMax);
                    break;
                case worldInstancedDestructibleMeshNode instancedDestructibleNode:
                    try
                    {
                        var instancedDestMeshNodeBoundsMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                        var instancedDestMeshNodeBoundsMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                        var instancedDestToken = _gameFileService.GetCMesh(instancedDestructibleNode.Mesh.DepotPath);
                        if (instancedDestToken.Resource is not AbbrMesh instancedDestMesh)
                            goto DefaultLabel;
                        
                        transforms =
                            new AbbrSectorTransform[instancedDestructibleNode.CookedInstanceTransforms.NumElements];
                        var transformsInstancedDestructibleBuffer =
                            instancedDestructibleNode.CookedInstanceTransforms.SharedDataBuffer.Chunk.Buffer.Data as
                                CookedInstanceTransformsBuffer;
                        int transformsInstancedDestructibleIndex = 0;
                        foreach (var transform in transformsInstancedDestructibleBuffer.Transforms.ToArray()
                                     .AsSpan((int)(uint)instancedDestructibleNode.CookedInstanceTransforms.StartIndex,
                                         (int)(uint)instancedDestructibleNode.CookedInstanceTransforms.NumElements))
                        {
                            transforms[transformsInstancedDestructibleIndex] = new AbbrSectorTransform()
                            {
                                Position = WolvenkitToSharpDXConverter.Vector3(transform.Position) +
                                           WolvenkitToSharpDXConverter.Vector3(nodeDataEntry.Position),
                                Rotation = WolvenkitToSharpDXConverter.Quaternion(transform.Orientation) *
                                           WolvenkitToSharpDXConverter.Quaternion(nodeDataEntry.Orientation),
                                Scale = WolvenkitToSharpDXConverter.Vector3(nodeDataEntry.Scale)
                            };
                            
                            var transformMatrix = Matrix.Scaling(
                                                      (transform.Position.ToSDX() +
                                                      nodeDataEntry.Position.ToSDX())
                                                        .ToVector3()) * 
                                                  Matrix.RotationQuaternion(
                                                      transform.Orientation.ToSDX() *
                                                      nodeDataEntry.Orientation.ToSDX()) *  
                                                  Matrix.Translation(nodeDataEntry.Scale.ToSDX());
                            
                            foreach (var submesh in instancedDestMesh.SubMeshes)
                            {
                                var obb = new OrientedBoundingBox(submesh.BoundingBox);
                                obb.Transform(transformMatrix);
                            
                                foreach (var corner in obb.GetCorners())
                                {
                                    instancedDestMeshNodeBoundsMin = Vector3.Min(instancedDestMeshNodeBoundsMin, corner);
                                    instancedDestMeshNodeBoundsMax = Vector3.Max(instancedDestMeshNodeBoundsMax, corner);
                                }
                            }
                            
                            transformsInstancedDestructibleIndex++;
                        }
                        
                        nodeBoundingBox = new BoundingBox(instancedDestMeshNodeBoundsMin, instancedDestMeshNodeBoundsMax);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to process worldInstancedDestructibleMeshNode with error: {ex}");
                        goto DefaultLabel;
                    }
                    break;
                case worldFoliageNode instancedFoliageNode:
                    transforms = new AbbrSectorTransform[instancedFoliageNode.PopulationSpanInfo.StancesCount];
                    
                    FoliageBuffer fb;
                    
                    if (instancedFoliageNode.FoliageResource.Flags == InternalEnums.EImportFlags.Embedded)
                    {
                        var foliageResourceFile = input.EmbeddedFiles.FirstOrDefault(efile =>
                            efile.FileName == instancedFoliageNode.FoliageResource.DepotPath);
                        
                        if (foliageResourceFile.Content is not worldFoliageCompiledResource wfcr)
                        {
                            Logger.Warning($"Embedded resource {instancedFoliageNode.FoliageResource.DepotPath} is not worldFoliageCompiledResource!");
                            goto DefaultLabel;
                        }
                    
                        if (wfcr.DataBuffer.Data is not FoliageBuffer foliagebuffer)
                        {
                            Logger.Warning("Failed to process worldFoliage resource, Data is not FoliageBuffer!");
                            goto DefaultLabel;
                        }

                        fb = foliagebuffer;
                        
                    }
                    else
                    {
                        if (!_gameFileService.IsInitialized)
                        {
                            Logger.Warning($"Resource {instancedFoliageNode.FoliageResource.DepotPath} is not embedded and game file service is not initialized! Using fallback method, cache should be manually cleared once issue is resolved.");
                            goto DefaultLabel;
                        }
                        
                        var foliageCR2W = _gameFileService.ArchiveManager.GetCR2WFile(instancedFoliageNode.FoliageResource.DepotPath);
                        if (foliageCR2W is null)
                        {
                            Logger.Warning($"Failed to get {instancedFoliageNode.FoliageResource.DepotPath} from archive files!");
                            goto DefaultLabel;
                        }
        
                        if (foliageCR2W.RootChunk is not worldFoliageCompiledResource { DataBuffer.Data: FoliageBuffer foliageBuffer })
                        {
                            Logger.Warning($"Failed to get {instancedFoliageNode.FoliageResource.DepotPath} is not worldFoliageCompiledResource!");
                            goto DefaultLabel;
                        }
                        
                        fb = foliageBuffer;
                    }
                    
                    int foliageTransformIndex = 0;
                    foreach (var transform in fb.Populations.ToArray()
                                 .AsSpan((int)(uint)instancedFoliageNode.PopulationSpanInfo.StancesBegin,
                                     (int)(uint)instancedFoliageNode.PopulationSpanInfo.StancesCount))
                    {
                        transforms[foliageTransformIndex] = new AbbrSectorTransform()
                        {
                            Position = WolvenkitToSharpDXConverter.Vector3(transform.Position) +
                                       WolvenkitToSharpDXConverter.Vector3(nodeDataEntry.Position),
                            Rotation = new Quaternion(transform.Rotation.X, transform.Rotation.Y,
                                transform.Rotation.Z, transform.Rotation.W),
                            Scale = new Vector3(transform.Scale, transform.Scale, transform.Scale)
                        };
                        foliageTransformIndex++;
                    }
                    nodeBoundingBox = new BoundingBox(WolvenkitToSharpDXConverter.Vector3(instancedFoliageNode.FoliageLocalBounds.Min) + WolvenkitToSharpDXConverter.Vector3(nodeDataEntry.Position),
                        WolvenkitToSharpDXConverter.Vector3(instancedFoliageNode.FoliageLocalBounds.Max) + WolvenkitToSharpDXConverter.Vector3(nodeDataEntry.Position));
                    break;
                case worldInstancedOccluderNode instancedOccluderNode:
                    transforms = new AbbrSectorTransform[instancedOccluderNode.Buffer.Count];
                    
                    int instancedOccluderInstanceIndex = 0;
                    foreach (var instance in instancedOccluderNode.Buffer)
                    {
                        var matrix = new Matrix()
                        {
                            M11 = instance.Unknown1.X,
                            M12 = instance.Unknown1.Y,
                            M13 = instance.Unknown1.Z,
                            M14 = instance.Unknown1.W,
                            M21 = instance.Unknown2.X,
                            M22 = instance.Unknown2.Y,
                            M23 = instance.Unknown2.Z,
                            M24 = instance.Unknown2.W,
                            M31 = instance.Unknown3.X,
                            M32 = instance.Unknown3.Y,
                            M33 = instance.Unknown3.Z,
                            M34 = instance.Unknown3.W,
                            M41 = instance.Unknown4.X,
                            M42 = instance.Unknown4.Y,
                            M43 = instance.Unknown4.Z,
                            M44 = instance.Unknown4.W
                        };

                        transforms[instancedOccluderInstanceIndex] = new AbbrSectorTransform
                        {
                            Position = matrix.TranslationVector,
                            Scale = matrix.ScaleVector,
                            Rotation = Quaternion.RotationMatrix(matrix)
                        };
                        
                        instancedOccluderInstanceIndex++;
                    }   
                    break;
                case worldWaterPatchNode waterPatchNode:
                    var waterPatchNodeToken = _gameFileService.GetCMesh(waterPatchNode.Mesh.DepotPath);
                    if (waterPatchNodeToken.Resource is AbbrMesh waterPatchNodeMesh)
                    {
                        var waterPatchNodeBoundsMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                        var waterPatchNodeBoundsMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                        var transformMatrix = Matrix.Scaling(nodeDataEntry.Position.ToSDX().ToVector3()) * 
                                                 Matrix.RotationQuaternion(nodeDataEntry.Orientation.ToSDX()) *  
                                                 Matrix.Translation(nodeDataEntry.Scale.ToSDX());
                        
                        foreach (var submesh in waterPatchNodeMesh.SubMeshes)
                        {
                            var obb = new OrientedBoundingBox(submesh.BoundingBox);
                            obb.Transform(transformMatrix);
                            
                            foreach (var corner in obb.GetCorners())
                            {
                                waterPatchNodeBoundsMin = Vector3.Min(waterPatchNodeBoundsMin, corner);
                                waterPatchNodeBoundsMax = Vector3.Max(waterPatchNodeBoundsMax, corner);
                            }
                        }

                        waterPatchNodeBoundsMin.Z -= waterPatchNode.Depth;
                        
                        nodeBoundingBox = new BoundingBox(waterPatchNodeBoundsMin, waterPatchNodeBoundsMax);
                    }
                    goto DefaultLabel;
                case worldMeshNode meshNode:
                    var resourceToken = _gameFileService.GetCMesh(meshNode.Mesh.DepotPath);
                    if (resourceToken.Resource is AbbrMesh abbrMesh)
                    {
                        var meshNodeBoundsMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                        var meshNodeBoundsMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                        var transformMatrix = Matrix.Scaling(nodeDataEntry.Position.ToSDX().ToVector3()) * 
                                                      Matrix.RotationQuaternion(nodeDataEntry.Orientation.ToSDX()) *  
                                                      Matrix.Translation(nodeDataEntry.Scale.ToSDX());
                        
                        foreach (var submesh in abbrMesh.SubMeshes)
                        {
                            var obb = new OrientedBoundingBox(submesh.BoundingBox);
                            obb.Transform(transformMatrix);
                            
                            foreach (var corner in obb.GetCorners())
                            {
                                meshNodeBoundsMin = Vector3.Min(meshNodeBoundsMin, corner);
                                meshNodeBoundsMax = Vector3.Max(meshNodeBoundsMax, corner);
                            }
                        }
                        
                        nodeBoundingBox = new BoundingBox(meshNodeBoundsMin, meshNodeBoundsMax);
                    }
                    goto DefaultLabel;
                default:
                    DefaultLabel:
                    {
                        transforms = new AbbrSectorTransform[1];
                        transforms[0] = new AbbrSectorTransform()
                        {
                            Position = WolvenkitToSharpDXConverter.Vector3(nodeDataEntry.Position),
                            Rotation = WolvenkitToSharpDXConverter.Quaternion(nodeDataEntry.Orientation),
                            Scale = WolvenkitToSharpDXConverter.Vector3(nodeDataEntry.Scale)
                        };
                    }
                    break;
            }

            nodeDataOut[nodeDataIndex] = new AbbrStreamingSectorNodeDataEntry()
            {
                NodeIndex = nodeDataEntry.NodeIndex,
                Transforms = transforms,
                AABB = nodeBoundingBox
            };
            nodeDataIndex++;
        }

        return new AbbrSector()
        {
            Nodes = nodes,
            NodeData = nodeDataOut
        };
    }
}