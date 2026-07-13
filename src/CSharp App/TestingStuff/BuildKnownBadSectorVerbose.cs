using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpDX;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;
using WEnums = WolvenKit.RED4.Types.Enums;
using Vector3 = SharpDX.Vector3;

namespace VolumetricSelection2077.TestingStuff;

public class BuildKnownBadSectorVerbose : IDebugTool
{
    private readonly GameFileService _gameFileService;

    public BuildKnownBadSectorVerbose(GameFileService gameFileService)
    {
        _gameFileService = gameFileService;
    }

    public void Run()
    {
       Logger.Info("Building bounding box for sector \"-15_-16_0_1\'");
       _ = ProcessStreamingsector(@"base\worlds\03_night_city\_compiled\default\exterior_-15_-16_0_1.streamingsector", null);
    }
    
    
    private async Task ProcessStreamingsector(string sectorPath, CacheDatabases? database)
    {
        try
        {
            Logger.Info($"Building bounds for {sectorPath}...");
            var sectorToken = _gameFileService.GetSector(sectorPath);
            if (sectorToken.Resource is not AbbrSector sector)
            {
                switch (sectorToken.Result)
                {
                    case ResourceTokenResult.KnownBad: Logger.Warning($"Sector {sectorPath} is a known bad resource, skipping..."); break;
                    case ResourceTokenResult.Success: Logger.Error($"Received an unexpected resource type {sectorToken.Resource?.GetType()} for sector {sectorPath}"); break;
                    case ResourceTokenResult.Failure: Logger.Error($"Failed to get sector {sectorPath}"); break;
                    default: Logger.Error($"Unknown resource token result {sectorToken.Result} for sector {sectorPath}"); break;
                }
                return;
            }

            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            
            foreach (var nodeDataEntry in sector.NodeData)
            {
                var nodeEntry = sector.Nodes[nodeDataEntry.NodeIndex];
                
                if ((nodeEntry?.ResourcePath?.EndsWith(".mesh") ?? false) || (nodeEntry?.ResourcePath?.EndsWith(".w2mesh") ?? false))
                {
                    var meshToken = _gameFileService.GetCMesh(nodeEntry.ResourcePath);
                    if (meshToken.Resource is not AbbrMesh mesh)
                    {
                        switch (meshToken.Result)
                        {
                            case ResourceTokenResult.KnownBad: Logger.Warning($"Mesh {nodeEntry.ResourcePath} is a known bad resource, using only position data"); break;
                            case ResourceTokenResult.Success: Logger.Warning($"Received an unexpected resource type {meshToken.Resource?.GetType()} for mesh {nodeEntry.ResourcePath}, using only position data"); break;
                            case ResourceTokenResult.Failure: Logger.Warning($"Failed to get mesh {nodeEntry.ResourcePath}, using only position data"); break;
                            default: Logger.Warning($"Unknown resource token result {meshToken.Result} for mesh {nodeEntry.ResourcePath}, using only position data"); break;
                        }
                        foreach (var transform in nodeDataEntry.Transforms)
                        {
                            min = Vector3.Min(min, transform.TranslationVector);
                            max = Vector3.Max(max, transform.TranslationVector);
                        }
                        continue;
                    }

                    foreach (var transform in nodeDataEntry.Transforms)
                    {
                        foreach (var submesh in mesh.SubMeshes)
                        {
                            var transformedObb = new OrientedBoundingBox(submesh.BoundingBox);
                            transformedObb.Transform(transform);
                            foreach (var corner in transformedObb.GetCorners())
                            {
                                min = Vector3.Min(min, corner);
                                max = Vector3.Max(max, corner);
                            }
                        }
                    }
                } else if (nodeEntry?.Actors != null)
                {
                    foreach (var actor in nodeEntry.Actors)
                    {
                        foreach (var shape in actor.Shapes)
                        {
                            switch (shape.ShapeType)
                            {
                                case WEnums.physicsShapeType.TriangleMesh:
                                case WEnums.physicsShapeType.ConvexMesh:
                                    if (nodeEntry.SectorHash is not ulong sectorHash ||
                                        shape.Hash is not ulong shapeHash)
                                    {
                                        Logger.Warning($"Node Data entry contains invalid sector hash {nodeEntry.SectorHash} or shape hash {shape.Hash}, using only position data");
                                        var combinedPosition = shape.Transform.TranslationVector;
                                        min = Vector3.Min(min, combinedPosition);
                                        max = Vector3.Max(max, combinedPosition);
                                        continue;
                                    }
                                    
                                    var collisionMeshToken = await _gameFileService.GetPhysXMesh(sectorHash,
                                        shapeHash);
                                    if (collisionMeshToken.Resource is not AbbrMesh collisionMesh)
                                    {
                                        switch (collisionMeshToken.Result)
                                        {
                                            case ResourceTokenResult.KnownBad: Logger.Warning($"Collision mesh at sector hash {sectorHash} : shape hash {shapeHash} is a known bad resource, using only position data"); break;
                                            case ResourceTokenResult.Success: Logger.Warning($"Received an unexpected resource type {collisionMeshToken.Resource?.GetType()} for sector hash {sectorHash} : shape hash {shapeHash}, using only position data"); break;
                                            case ResourceTokenResult.Failure: Logger.Warning($"Failed to get collision mesh at sector hash {sectorHash} : shape hash {shapeHash}, using only position data"); break;
                                            default: Logger.Warning($"Unknown resource token result {collisionMeshToken.Result} for collision mesh at sector hash {sectorHash} : shape hash {shapeHash}, using only position data"); break;
                                        }
                                        var combinedPosition = shape.Transform.TranslationVector;
                                        min = Vector3.Min(min, combinedPosition);
                                        max = Vector3.Max(max, combinedPosition);
                                        continue;
                                    }
                                    
                                    foreach (var submesh in collisionMesh.SubMeshes)
                                    {
                                        OrientedBoundingBox obb = new(submesh.BoundingBox);
                                        obb.Transform(shape.Transform);
                                        foreach (var corner in obb.GetCorners())
                                        {
                                            min = Vector3.Min(min, corner);
                                            max = Vector3.Max(max, corner);
                                        }
                                    }
                                    break;
                                case WEnums.physicsShapeType.Box:
                                    OrientedBoundingBox collisionBox = new(-shape.Scale, shape.Scale);
                                    collisionBox.Transform(shape.Transform);
                                    foreach (var corner in collisionBox.GetCorners())
                                    {
                                        min = Vector3.Min(min, corner);
                                        max = Vector3.Max(max, corner);
                                    }
                                    break;
                                case WEnums.physicsShapeType.Capsule:
                                    // M11 = ScaleVector.X
                                    // shape transform is the combination of actor and shape transforms, with the shape scale we can calculate
                                    // the actor scale
                                    var actorScaleX = Math.Abs(shape.Scale.X - shape.Transform.M11);
                                    
                                    var height = shape.Scale.Y + 2 * actorScaleX;   
                                    var shapeSizeAsBox = new Vector3(shape.Scale.X, shape.Scale.X, height / 2f);
                                    
                                    OrientedBoundingBox capsuleObb = new(-shapeSizeAsBox, shapeSizeAsBox);
                                    capsuleObb.Transform(shape.Transform);
                                    foreach (var corner in capsuleObb.GetCorners())
                                    {
                                        min = Vector3.Min(min, corner);
                                        max = Vector3.Max(max, corner);
                                    }
                                    break;
                                case WEnums.physicsShapeType.Sphere:
                                    OrientedBoundingBox sphereObb = new(new Vector3(-shape.Scale.X), new Vector3(shape.Scale.X));
                                    sphereObb.Transform(shape.Transform);
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
                        min = Vector3.Min(min, transform.TranslationVector);
                        max = Vector3.Max(max, transform.TranslationVector);
                    }
                }
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