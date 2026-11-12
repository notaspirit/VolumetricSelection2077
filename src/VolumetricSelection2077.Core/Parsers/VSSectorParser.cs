using MessagePack;
using SharpDX;
using VolumetricSelection2077.Core.Converters.Simple;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Services;
using WolvenKit.RED4.Archive.Buffer;
using WolvenKit.RED4.Archive.CR2W;
using WolvenKit.RED4.Types;
using Quaternion = SharpDX.Quaternion;
using Vector3 = SharpDX.Vector3;
using worldNodeData = WolvenKit.RED4.Archive.Buffer.worldNodeData;

namespace VolumetricSelection2077.Core.Parsers;

public class VSSectorParser
{
    private readonly GameFileService _gameFileService;
    private readonly CacheService _cacheService;
    
    public VSSectorParser(GameFileService gameFileService, CacheService cacheService)
    {
        _gameFileService = gameFileService;
        _cacheService = cacheService;
    }
    
    /// <summary>
    /// Parses a CR2W file into an VSSector
    /// </summary>
    /// <param name="input"></param>
    /// <returns>Result containing the parsed VSSector, with any warnings collected in the message</returns>
    public Result<VSSector> ParseFromCR2W(CR2WFile input)
    {
        var result = new Result<VSSector>();
        var warnings = new List<string>();
        
        if (input.RootChunk is not worldStreamingSector sector)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage("Input file is not a world streaming sector");
            return result;
        }
    
        foreach (var efile in input.EmbeddedFiles)
        {
            if ((efile?.FileName.ToString()?.EndsWith(".mesh") ?? false) || (efile?.FileName.ToString()?.EndsWith(".w2mesh") ?? false))
            {
                var parsedMesh = VSMeshParser.ParseFromEmbedded(efile);
                if (parsedMesh is null)
                {
                    warnings.Add($"Failed to parse embedded file {efile.FileName}");
                    continue;
                }
                var writeResult = _cacheService.WriteSingleEntry(new WriteCacheRequest(efile.FileName, MessagePackSerializer.Serialize(parsedMesh)));
                if (writeResult.Status == ResultStatus.Error)
                    warnings.Add($"Failed to write embedded mesh {efile.FileName} to cache: {writeResult.Message}");
            }
        }
        
        var nodes = new VSNode[sector.Nodes.Count];
        
        int nodeIndex = 0;
        foreach (var node in sector.Nodes)
        {
            
            ulong? sectorHash = null;
            VSCollisionActor[]? actors = null;
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
                    actors = new VSCollisionActor[collisionNode.NumActors];
                    var collisionActorBuffer = collisionNode.CompiledData.Data as CollisionBuffer;
                    int actorIndex = 0;
                    foreach (var actor in collisionActorBuffer.Actors)
                    {
                        var actorPosition = FixedPointVector3Converter.PosBitsToVec3(actor.Position);
                        var actorRotation = actor.Orientation.ToSDXQ();
                        var actorScale = actor.Scale.ToSDXV3();
                        var shapes = new VSActorShape[actor.Shapes.Count];
                        int shapesIndex = 0;
                        foreach (var shape in actor.Shapes)
                        {
                            var shapeTypeString = shape.ShapeType.ToString();
                            var shapePosition = shape.Position.ToSDXV3();
                            var shapeRotation = shape.Rotation.ToSDXQ();
                            var shapeScale = new SharpDX.Vector3(1,1,1);
                            ulong? shapeHash = null;
                            
                            switch (shape)
                            {
                                case CollisionShapeSimple simpleShape:
                                    shapeScale = simpleShape.Size.ToSDXV3();
                                    break;
                                case CollisionShapeMesh meshShape:
                                    shapeHash = meshShape.Hash;
                                    break;
                            }

                            var parsedShapeType = Enum.TryParse(shapeTypeString, out WolvenKit.RED4.Types.Enums.physicsShapeType shapeType);
                            if (!parsedShapeType)
                                warnings.Add($"Invalid shape type: {shapeTypeString}");
                            
                            shapes[shapesIndex] = new VSActorShape()
                            {
                                ShapeType = shapeType,
                                Hash = shapeHash,
                                Transform = new VSTransform()
                                {
                                    Position = shapePosition,
                                    Rotation = shapeRotation,
                                    Scale = shapeScale
                                }
                            };
                            shapesIndex++;
                        }

                        actors[actorIndex] = new VSCollisionActor()
                        {
                            Shapes = shapes,
                            Transform = new VSTransform()
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

            nodes[nodeIndex] = new VSNode()
            {
                SectorHash = sectorHash,
                Actors = actors,
                DebugName = node.Chunk?.DebugName,
                ResourcePath = resourcePath,
                Type = node.Chunk?.GetType().Name ?? "Unknown"
            };
            
            nodeIndex++;
        }

        var nodeDataBuffer = sector.NodeData.Data as CArray<worldNodeData>;
        var nodeDataOut = new VSNodeData[nodeDataBuffer.Count];
        
        int nodeDataIndex = 0;
        foreach (var nodeDataEntry in nodeDataBuffer)
        {
            VSTransform[] transforms;
            BoundingBox? nodeBoundingBox = null;
            
            switch (sector.Nodes[nodeDataEntry.NodeIndex].Chunk)
            {
                case worldInstancedMeshNode instancedNode:
                    transforms = new VSTransform[instancedNode.WorldTransformsBuffer.NumElements];
                    var transformsInstancedBuffer = instancedNode.WorldTransformsBuffer.SharedDataBuffer.Chunk.Buffer.Data as WorldTransformsBuffer;
                    int transformsInstancedIndex = 0;
                    foreach (var transform in transformsInstancedBuffer.Transforms.ToArray().AsSpan((int)(uint)instancedNode.WorldTransformsBuffer.StartIndex, (int)(uint)instancedNode.WorldTransformsBuffer.NumElements))
                    {
                        transforms[transformsInstancedIndex] = new VSTransform()
                        {
                            Position = transform.Translation.ToSDXV3(),
                            Rotation = transform.Rotation.ToSDXQ(),
                            Scale = transform.Scale.ToSDXV3()
                        };
                        transformsInstancedIndex++;
                    }
                    break;
                case worldInstancedDestructibleMeshNode instancedDestructibleNode:
                    try
                    {
                        transforms =
                            new VSTransform[instancedDestructibleNode.CookedInstanceTransforms.NumElements];
                        var transformsInstancedDestructibleBuffer =
                            instancedDestructibleNode.CookedInstanceTransforms.SharedDataBuffer.Chunk.Buffer.Data as
                                CookedInstanceTransformsBuffer;
                        int transformsInstancedDestructibleIndex = 0;
                        foreach (var transform in transformsInstancedDestructibleBuffer.Transforms.ToArray()
                                     .AsSpan((int)(uint)instancedDestructibleNode.CookedInstanceTransforms.StartIndex,
                                         (int)(uint)instancedDestructibleNode.CookedInstanceTransforms.NumElements))
                        {
                            transforms[transformsInstancedDestructibleIndex] = new VSTransform()
                            {
                                Position = transform.Position.ToSDXV3() +
                                           nodeDataEntry.Position.ToSDXV3(),
                                Rotation = transform.Orientation.ToSDXQ() *
                                           nodeDataEntry.Orientation.ToSDXQ(),
                                Scale = nodeDataEntry.Scale.ToSDXV3()
                            };
                            transformsInstancedDestructibleIndex++;
                        }
                    }
                    catch (Exception ex)
                    {
                        warnings.Add($"Failed to process worldInstancedDestructibleMeshNode with error: {ex}");
                        goto DefaultLabel;
                    }
                    break;
                case worldFoliageNode instancedFoliageNode:
                    transforms = new VSTransform[instancedFoliageNode.PopulationSpanInfo.StancesCount];
                    
                    FoliageBuffer fb;
                    
                    if (instancedFoliageNode.FoliageResource.Flags == InternalEnums.EImportFlags.Embedded)
                    {
                        var foliageResourceFile = input.EmbeddedFiles.FirstOrDefault(efile =>
                            efile.FileName == instancedFoliageNode.FoliageResource.DepotPath);
                        
                        if (foliageResourceFile.Content is not worldFoliageCompiledResource wfcr)
                        {
                            warnings.Add($"Embedded resource {instancedFoliageNode.FoliageResource.DepotPath} is not worldFoliageCompiledResource!");
                            goto DefaultLabel;
                        }
                    
                        if (wfcr.DataBuffer.Data is not FoliageBuffer foliagebuffer)
                        {
                            warnings.Add("Failed to process worldFoliage resource, Data is not FoliageBuffer!");
                            goto DefaultLabel;
                        }

                        fb = foliagebuffer;
                        
                    }
                    else
                    {
                        if (!_gameFileService.IsInitialized)
                        {
                            warnings.Add($"Resource {instancedFoliageNode.FoliageResource.DepotPath} is not embedded and game file service is not initialized! Using fallback method, cache should be manually cleared once issue is resolved.");
                            goto DefaultLabel;
                        }
                        
                        var foliageCR2W = _gameFileService.ArchiveManager.GetCR2WFile(instancedFoliageNode.FoliageResource.DepotPath);
                        if (foliageCR2W is null)
                        {
                            warnings.Add($"Failed to get {instancedFoliageNode.FoliageResource.DepotPath} from archive files!");
                            goto DefaultLabel;
                        }
        
                        if (foliageCR2W.RootChunk is not worldFoliageCompiledResource { DataBuffer.Data: FoliageBuffer foliageBuffer })
                        {
                            warnings.Add($"Failed to get {instancedFoliageNode.FoliageResource.DepotPath} is not worldFoliageCompiledResource!");
                            goto DefaultLabel;
                        }
                        
                        fb = foliageBuffer;
                    }
                    
                    int foliageTransformIndex = 0;
                    foreach (var transform in fb.Populations.ToArray()
                                 .AsSpan((int)(uint)instancedFoliageNode.PopulationSpanInfo.StancesBegin,
                                     (int)(uint)instancedFoliageNode.PopulationSpanInfo.StancesCount))
                    {
                        transforms[foliageTransformIndex] = new VSTransform()
                        {
                            Position = transform.Position.ToSDXV3() +
                                       nodeDataEntry.Position.ToSDXV3(),
                            Rotation = new Quaternion(transform.Rotation.X, transform.Rotation.Y,
                                transform.Rotation.Z, transform.Rotation.W),
                            Scale = new Vector3(transform.Scale, transform.Scale, transform.Scale)
                        };
                        foliageTransformIndex++;
                    }
                    nodeBoundingBox = new BoundingBox(instancedFoliageNode.FoliageLocalBounds.Min.ToSDXV3() + nodeDataEntry.Position.ToSDXV3(),
                        instancedFoliageNode.FoliageLocalBounds.Max.ToSDXV3() + nodeDataEntry.Position.ToSDXV3());
                    break;
                case worldInstancedOccluderNode instancedOccluderNode:
                    transforms = new VSTransform[instancedOccluderNode.Buffer.Count];
                    
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

                        transforms[instancedOccluderInstanceIndex] = new VSTransform
                        {
                            Position = matrix.TranslationVector,
                            Scale = matrix.ScaleVector,
                            Rotation = Quaternion.RotationMatrix(matrix)
                        };
                        
                        instancedOccluderInstanceIndex++;
                    }   
                    break;
                default:
                    DefaultLabel:
                    {
                        transforms = new VSTransform[1];
                        transforms[0] = new VSTransform()
                        {
                            Position = nodeDataEntry.Position.ToSDXV3(),
                            Rotation = nodeDataEntry.Orientation.ToSDXQ(),
                            Scale = nodeDataEntry.Scale.ToSDXV3()
                        };
                    }
                    break;
            }

            nodeDataOut[nodeDataIndex] = new VSNodeData()
            {
                NodeIndex = nodeDataEntry.NodeIndex,
                Transforms = transforms,
                AABB = nodeBoundingBox
            };
            nodeDataIndex++;
        }

        result.Data = new VSSector()
        {
            Nodes = nodes,
            NodeData = nodeDataOut
        };
        result.Status = warnings.Count > 0 ? ResultStatus.Warning : ResultStatus.Ok;
        if (warnings.Count > 0)
            result.AddMessage(string.Join(Environment.NewLine, warnings));
        
        return result;
    }
}