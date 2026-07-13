using VolumetricSelection2077.Models;
using WEnums = WolvenKit.RED4.Types.Enums;
using SharpDX;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class TestShpereCheck : IDebugTool
{
    public void Run()
    {
        var defaultTransform = Utils.BuildMatrix(new Vector3(0, 0, 0), new Quaternion(), new Vector3(1, 1, 1));
        
        var AABB = new OrientedBoundingBox(new Vector3(0,0,0), new Vector3(1,1,1));
        var OBB = new OrientedBoundingBox(new Vector3(0, 0, 0), new Vector3(1,0,1));
        OBB.Transform(Matrix.RotationQuaternion(new Quaternion(0.72f,0,0.72f,1)));
        
        var sphereInside1 = new AbbrActorShapes()
        {
            ShapeType = WEnums.physicsShapeType.Sphere,
            Transform = Utils.BuildMatrix(new Vector3(1, 1, 1), new Quaternion(), new Vector3(1, 1, 1)) * defaultTransform,
            Scale = new Vector3(1, 1, 1)
        };
        
        var sphereInside2 = new AbbrActorShapes()
        {
            ShapeType = WEnums.physicsShapeType.Sphere,
            Transform = Utils.BuildMatrix(new Vector3(3, 6, 1), new Quaternion(), new Vector3(6, 1, 1)) * defaultTransform,
            Scale = new Vector3(6, 1, 1)
        };
        
        var sphereOutside = new AbbrActorShapes()
        {
            ShapeType = WEnums.physicsShapeType.Sphere,
            Transform = Utils.BuildMatrix(new Vector3(10, 6, 1), new Quaternion(), new Vector3(4, 1, 1)) * defaultTransform,
            Scale = new Vector3(4, 1, 1)
        };

        Logger.Info($"Sphere inside1 against AABB: {CollisionCheckService.IsCollisionSphereInsideSelectionBox(sphereInside1, AABB)}"); 
        Logger.Info($"Sphere inside1 against OBB: {CollisionCheckService.IsCollisionSphereInsideSelectionBox(sphereInside1, OBB)}"); 

        Logger.Info($"Sphere inside2 against AABB: {CollisionCheckService.IsCollisionSphereInsideSelectionBox(sphereInside2, AABB)}"); 
        Logger.Info($"Sphere inside2 against OBB: {CollisionCheckService.IsCollisionSphereInsideSelectionBox(sphereInside2, OBB)}"); 
        
        Logger.Info($"Sphere outside against AABB: {CollisionCheckService.IsCollisionSphereInsideSelectionBox(sphereOutside, AABB)}"); 
        Logger.Info($"Sphere outside against OBB: {CollisionCheckService.IsCollisionSphereInsideSelectionBox(sphereOutside, OBB)}"); 
    }
}