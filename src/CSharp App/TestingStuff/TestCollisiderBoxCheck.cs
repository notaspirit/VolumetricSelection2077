using SharpDX;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class TestCollisiderBoxCheck
{
    public static void Run()
    {
        Vector3 selectionScale = new(5.07f, 7.62f, 5.64f);
        Vector3 genericTranslate = new Vector3(10, 1, 5);
        OrientedBoundingBox selectionBox = new(-(selectionScale / 2f), selectionScale / 2f);
        selectionBox.Transform(Matrix.RotationQuaternion(new Quaternion(0.119383f, 0.763533f, 0.304914f, 0.55659f)));
        selectionBox.Translate(genericTranslate + new Vector3(9.89f, 8.48f, 16.5f));

        AbbrSectorTransform genericActor = new()
        {
            Position = genericTranslate,
            Rotation = new(0.223181f, 0.864762f,0.37646f, 0.246282f),
            Scale = new(1.5f, 23f, 4.3f)
        };
        
        AbbrActorShapes collisionBox1 = new()
        {
            ShapeType = "Box",
            Transform = new()
            {
                Position = new(-9.12f, -5.52f, -5.09f),
                Rotation = new(-0.27416f, 0.623853f, 0.34494f, 0.645492f),
                Scale = new(1.64f, 2.36f, 0.45f)
            },
        };
        
        AbbrActorShapes collisionBox2 = new()
        {
            ShapeType = "Box",
            Transform = new()
            {
                Position = new(0f, 13.5991f, 5.57848f),
                Rotation = new(-0.069802f, 0.62638f, 0.313481f, 0.710285f),
                Scale = new(2.44f, 2.14f, 2.15f)
            },
        };
        
        AbbrActorShapes collisionBox3 = new()
        {
            ShapeType = "Box",
            Transform = new()
            {
                Position = new(10.036f, 9.38214f, 14.743f),
                Rotation = new(0.572493f, -0.107495f, -0.148663f, -0.799122f),
                Scale = new(2.32f, 2.46f, 3.17f)
            },
        };
        /*
        bool isCollision1Inside = CollisionCheckService.IsCollisionBoxInsideSelectionBox(collisionBox1, genericActor, selectionBox.GetBoundingBox(), selectionBox);
        bool isCollision2Inside = CollisionCheckService.IsCollisionBoxInsideSelectionBox(collisionBox2, genericActor, selectionBox.GetBoundingBox(), selectionBox);
        bool isCollision3Inside = CollisionCheckService.IsCollisionBoxInsideSelectionBox(collisionBox3, genericActor, selectionBox.GetBoundingBox(), selectionBox);
        
        Logger.Debug($"\n" +
                     $"Collider 1: {isCollision1Inside}\n" +
                     $"Collider 2: {isCollision2Inside}\n" +
                     $"Collider 3: {isCollision3Inside}");
    */
    }
}