using System;
using System.Linq;
using DynamicData;
using SharpDX.Direct3D9;
using VolumetricSelection2077.Converters;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Resources;
using VolumetricSelection2077.Services;
using WolvenKit.RED4.Archive.Buffer;
using WolvenKit.RED4.Archive.CR2W;
using WolvenKit.RED4.Types;
using Int64 = System.Int64;
using Vector3 = SharpDX.Vector3;
using worldNodeData = WolvenKit.RED4.Archive.Buffer.worldNodeData;

namespace VolumetricSelection2077.Parsers;

public class DirectAbbrSectorParser
{
    public static AbbrSector ParseFromCR2W(CR2WFile input)
    {
        if (input.RootChunk is not worldStreamingSector sector)
        {
            throw new Exception("Input file is not a world streaming sector");
        }
        
        var nodes = new AbbrStreamingSectorNodesEntry[sector.Nodes.Count];
        
        int nodeIndex = 0;
        foreach (var node in sector.Nodes)
        {
            var debugName = node.Chunk?.DebugName;
            var type = node.Chunk?.GetType().Name ?? "Unknown";
            var parsedTypeSuccess = NodeTypeProcessingOptions.Enum.TryParse(type, out NodeTypeProcessingOptions.Enum parsedType);
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
                        var actorRotation = WolvenkitToSharpDX.Quaternion(actor.Orientation);
                        var actorScale = WolvenkitToSharpDX.Vector3(actor.Scale);
                        var shapes = new AbbrActorShapes[actor.Shapes.Count];
                        int shapesIndex = 0;
                        foreach (var shape in actor.Shapes)
                        {
                            var shapeTypeString = shape.ShapeType.ToString();
                            var shapePosition = WolvenkitToSharpDX.Vector3(shape.Position);
                            var shapeRotation = WolvenkitToSharpDX.Quaternion(shape.Rotation);
                            var shapeScale = new SharpDX.Vector3(1,1,1);
                            ulong? shapeHash = null;
                            
                            switch (shape)
                            {
                                case CollisionShapeSimple simpleShape:
                                    shapeScale = WolvenkitToSharpDX.Vector3(simpleShape.Size);
                                    break;
                                case CollisionShapeMesh meshShape:
                                    shapeHash = meshShape.Hash;
                                    break;
                            }

                            var parsedShapeType = Enums.physicsShapeType.TryParse(shapeTypeString, out Enums.physicsShapeType shapeType);
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
                            Position = WolvenkitToSharpDX.Vector3(transform.Translation),
                            Rotation = WolvenkitToSharpDX.Quaternion(transform.Rotation),
                            Scale = WolvenkitToSharpDX.Vector3(transform.Scale)
                        };
                        transformsInstancedIndex++;
                    }
                    break;
                case worldInstancedDestructibleMeshNode instancedDestructibleNode:
                    transforms = new AbbrSectorTransform[instancedDestructibleNode.CookedInstanceTransforms.NumElements];
                    var transformsInstancedDestructibleBuffer = instancedDestructibleNode.CookedInstanceTransforms.SharedDataBuffer.Chunk.Buffer.Data as CookedInstanceTransformsBuffer;
                    int transformsInstancedDestructibleIndex = 0;
                    foreach (var transform in transformsInstancedDestructibleBuffer.Transforms.ToArray().AsSpan((int)(uint)instancedDestructibleNode.CookedInstanceTransforms.StartIndex, (int)(uint)instancedDestructibleNode.CookedInstanceTransforms.NumElements))
                    {
                        transforms[transformsInstancedDestructibleIndex] = new AbbrSectorTransform()
                        {
                            Position = WolvenkitToSharpDX.Vector3(transform.Position) + WolvenkitToSharpDX.Vector3(nodeDataEntry.Position),
                            Rotation = WolvenkitToSharpDX.Quaternion(transform.Orientation) * WolvenkitToSharpDX.Quaternion(nodeDataEntry.Orientation),
                            Scale = WolvenkitToSharpDX.Vector3(nodeDataEntry.Scale)
                        };
                        transformsInstancedDestructibleIndex++;
                    }
                    break;
                default:
                    transforms = new AbbrSectorTransform[1];
                    transforms[0] = new AbbrSectorTransform()
                    {
                        Position = WolvenkitToSharpDX.Vector3(nodeDataEntry.Position),
                        Rotation = WolvenkitToSharpDX.Quaternion(nodeDataEntry.Orientation),
                        Scale = WolvenkitToSharpDX.Vector3(nodeDataEntry.Scale)
                    };
                    break;
            }

            nodeDataOut[nodeDataIndex] = new AbbrStreamingSectorNodeDataEntry()
            {
                NodeIndex = nodeDataEntry.NodeIndex,
                Transforms = transforms
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