using System;
using System.Linq;
using DynamicData;
using SharpDX.Direct3D9;
using VolumetricSelection2077.Converters;
using VolumetricSelection2077.Models;
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
    public static AbbrSector? Parse(CR2WFile input)
    {
        if (input.RootChunk is not worldStreamingSector)
        {
            throw new Exception("Input file is not a world streaming sector");
        }
        var sector = input.RootChunk as worldStreamingSector;

        var nodes = new AbbrStreamingSectorNodesEntry[sector.Nodes.Count];
        
        int nodeIndex = 0;
        foreach (var node in sector.Nodes)
        {
            var debugName = node.Chunk?.DebugName;
            var type = node.Chunk?.GetType().Name ?? "Unknown";
            ulong? sectorHash = null;
            AbbrCollisionActors[]? actors = null;
            string? meshPath = null;
            
            switch (node.Chunk)
            {
                case worldMeshNode meshNode:
                    meshPath = meshNode.Mesh.DepotPath;
                    break;
                case worldInstancedMeshNode instancedMeshNode:
                    meshPath = instancedMeshNode.Mesh.DepotPath;
                    break;
                case worldInstancedOccluderNode instancedOccluderNode:
                    meshPath = instancedOccluderNode.Mesh.DepotPath;
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
                            var shapeType = shape.ShapeType.ToString();
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
                MeshDepotPath = meshPath,
                Type = type
            };
            
            nodeIndex++;
            
            Logger.Info($"{nodeIndex}");
            Logger.Info($"{type}");
            Logger.Info($"{debugName ?? "none"}");
            Logger.Info($"{meshPath ?? "none"}");
            Logger.Info($"{sectorHash}");
            Logger.Info($"{actors?.Length}");
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
                    Logger.Info("worldInstancedMeshNode");
                    transforms = new AbbrSectorTransform[instancedNode.WorldTransformsBuffer.NumElements];
                    var transformsInstancedBuffer = instancedNode.WorldTransformsBuffer.SharedDataBuffer.Chunk.Buffer.Data as CookedInstanceTransformsBuffer;
                    int transformsInstancedIndex = 0;
                    foreach (var transform in transformsInstancedBuffer.Transforms.ToArray().AsSpan((int)(uint)instancedNode.WorldTransformsBuffer.StartIndex, (int)(uint)instancedNode.WorldTransformsBuffer.NumElements))
                    {
                        transforms[transformsInstancedIndex] = new AbbrSectorTransform()
                        {
                            Position = WolvenkitToSharpDX.Vector3(transform.Position),
                            Rotation = WolvenkitToSharpDX.Quaternion(transform.Orientation),
                            Scale = new Vector3(1,1,1)
                        };
                        transformsInstancedIndex++;
                    }
                    break;
                case worldInstancedDestructibleMeshNode instancedDestructibleNode:
                    Logger.Info("worldInstancedDestructibleMeshNode");
                    transforms = new AbbrSectorTransform[instancedDestructibleNode.CookedInstanceTransforms.NumElements];
                    var transformsInstancedDestructibleBuffer = instancedDestructibleNode.CookedInstanceTransforms.SharedDataBuffer.Chunk.Buffer.Data as CookedInstanceTransformsBuffer;
                    int transformsInstancedDestructibleIndex = 0;
                    foreach (var transform in transformsInstancedDestructibleBuffer.Transforms.ToArray().AsSpan((int)(uint)instancedDestructibleNode.CookedInstanceTransforms.StartIndex, (int)(uint)instancedDestructibleNode.CookedInstanceTransforms.NumElements))
                    {
                        transforms[transformsInstancedDestructibleIndex] = new AbbrSectorTransform()
                        {
                            Position = WolvenkitToSharpDX.Vector3(transform.Position),
                            Rotation = WolvenkitToSharpDX.Quaternion(transform.Orientation),
                            Scale = new Vector3(1,1,1)
                        };
                        transformsInstancedDestructibleIndex++;
                    }

                    break;
                default:
                    Logger.Info("Default");
                    transforms = new AbbrSectorTransform[1];
                    transforms[0] = new AbbrSectorTransform()
                    {
                        Position = WolvenkitToSharpDX.Vector3(nodeDataEntry.Position),
                        Rotation = WolvenkitToSharpDX.Quaternion(nodeDataEntry.Orientation),
                        Scale = WolvenkitToSharpDX.Vector3(nodeDataEntry.Scale)
                    };
                    break;
            }
            Logger.Info($"{transforms.Length}");
            nodeDataIndex++;
        }
        
        
        return null;
    }
}