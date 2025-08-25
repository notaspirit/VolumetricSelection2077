using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpDX;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;
using WolvenKit.RED4.Types;
using Vector3 = SharpDX.Vector3;

namespace VolumetricSelection2077.TestingStuff;

public class BuildKnownBadSectorVerbose : IDebugTool
{
    public void Run()
    {
       Logger.Info("Building bounding box for sector \"-15_-16_0_1\'");
       ProcessStreamingsector(@"base\worlds\03_night_city\_compiled\default\exterior_-15_-16_0_1.streamingsector", null);
    }
    
    private GameFileService _gameFileService = GameFileService.Instance;
    
    
    private async Task ProcessStreamingsector(string sectorPath, CacheDatabases? database)
    {
        try
        {
            Logger.Info($"Building bounds for {sectorPath}...");
            var sector = _gameFileService.GetSector(sectorPath);
            if (sector == null)
            {
                Logger.Warning($"Failed to get sector {sectorPath}");
                return;
            }

            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            
            Logger.Info($"Sector {sectorPath} has {sector.NodeData.Length} nodeData Entries and {sector.Nodes.Length} nodes.");
            var i = 0;
            foreach (var nodeDataEntry in sector.NodeData)
            {
                var nodeEntry = sector.Nodes[nodeDataEntry.NodeIndex];
                Logger.Info($"Processing nodeDataEntry {i} with {nodeDataEntry.Transforms.Length} transforms, type {nodeEntry?.Type}, debug name {nodeEntry?.DebugName} and resource path {nodeEntry?.ResourcePath}");
                
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
                        var transformMatrix = Matrix.Scaling(transform.Scale) *
                                              Matrix.RotationQuaternion(transform.Rotation) *
                                              Matrix.Translation(transform.Position);
                        foreach (var submesh in mesh.SubMeshes)
                        {
                            var transformedObb = new OrientedBoundingBox(submesh.BoundingBox);
                            transformedObb.Transform(transformMatrix);
                            Logger.Info($"Transformed OBB: {transformedObb}");
                            Logger.Info($"Min before: {min}, max before: {max}");
                            foreach (var corner in transformedObb.GetCorners())
                            {
                                min = Vector3.Min(min, corner);
                                max = Vector3.Max(max, corner);
                            }
                            Logger.Info($"Min after: {min}, max after: {max}");
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

                i++;
            }
            BoundingBox bb;
            if (min == max)
                bb = new BoundingBox(min - new Vector3(1,1,1), max + new Vector3(1,1,1));
            else
                bb = new BoundingBox(min, max);
            Logger.Info($"Bounding box is\n{JsonConvert.SerializeObject(bb, Formatting.Indented)}");
        }
        catch (Exception e)
        {
            Logger.Exception(e, $"Failed to build bounding box for {sectorPath}");
        }
    }
}