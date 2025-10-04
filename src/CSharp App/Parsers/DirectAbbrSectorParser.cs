using System;
using System.Linq;
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
    /// <summary>
    /// Parses a CR2W file into an AbbrSector
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="Exception">cr2w file is not a streamingsector</exception>
    public static AbbrSector ParseFromCR2W(CR2WFile input)
    {
        var gfs = GameFileService.Instance;
        
        if (input.RootChunk is not worldStreamingSector sector)
        {
            throw new Exception("Input file is not a world streaming sector");
        }
    
        foreach (var efile in input.EmbeddedFiles)
        {
            if ((efile?.FileName.ToString()?.EndsWith(".mesh") ?? false) || (efile?.FileName.ToString()?.EndsWith(".w2mesh") ?? false))
            {
                var parsedMesh = DirectAbbrMeshParser.ParseFromEmbedded(efile);
                if (parsedMesh is null)
                {
                    Logger.Warning($"Failed to parse embedded file {efile.FileName}");
                    continue;
                }
                CacheService.Instance.WriteSingleEntry(new WriteCacheRequest(efile.FileName, MessagePackSerializer.Serialize(parsedMesh)));
            }
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
                    foreach (var transform in transformsInstancedBuffer.Transforms.ToArray().AsSpan((int)(uint)instancedNode.WorldTransformsBuffer.StartIndex, (int)(uint)instancedNode.WorldTransformsBuffer.NumElements))
                    {
                        transforms[transformsInstancedIndex] = new AbbrSectorTransform()
                        {
                            Position = WolvenkitToSharpDXConverter.Vector3(transform.Translation),
                            Rotation = WolvenkitToSharpDXConverter.Quaternion(transform.Rotation),
                            Scale = WolvenkitToSharpDXConverter.Vector3(transform.Scale)
                        };
                        transformsInstancedIndex++;
                    }
                    break;
                case worldInstancedDestructibleMeshNode instancedDestructibleNode:
                    try
                    {
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
                            transformsInstancedDestructibleIndex++;
                        }
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
                        if (!gfs?.IsInitialized ?? false)
                        {
                            Logger.Warning($"Resource {instancedFoliageNode.FoliageResource.DepotPath} is not embedded and game file service is not initialized! Using fallback method, cache should be manually cleared once issue is resolved.");
                            goto DefaultLabel;
                        }
                        
                        var foliageCR2W = gfs?.ArchiveManager?.GetCR2WFile(instancedFoliageNode.FoliageResource.DepotPath);
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