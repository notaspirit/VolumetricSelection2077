using System;
using SharpDX;
using VolumetricSelection2077.Models;
using System.Collections.Generic;
using System.Text.Json;
using HelixToolkit.Wpf.SharpDX;
using SharpGLTF.IO;

namespace VolumetricSelection2077.Services;

public class CollisionCheckService
{
    private static Vector3 Vec4toVec3(Vector4 v)
    {
        Vector3 result;
        if (Math.Abs(v.X) < float.Epsilon)
        {
            result = new Vector3(
                v.X / v.W,
                v.Y / v.W,
                v.Z / v.W
            );
        }
        else
        {
            result = new Vector3(v.X, v.Y, v.Z);
        }
        return result;
    }
    
    public static bool IsMeshInsideBox(AbbrMesh mesh, OrientedBoundingBox selectionBoxOBB, BoundingBox selectionBoxAabb, List<AbbrSectorTransform> transforms)
    {
        foreach (var submesh in mesh.SubMeshes)
        {
            foreach (var transform in transforms)
            {
                Quaternion normalizedQuaternion = transform.Rotation;
                OrientedBoundingBox localMeshObb = submesh.BoundingBox;
                normalizedQuaternion.Normalize();
                Matrix meshRotationMatrix = Matrix.RotationQuaternion(normalizedQuaternion);
                localMeshObb.Scale(transform.Scale);
                localMeshObb.Transform(meshRotationMatrix);
                localMeshObb.Translate(transform.Position);
                BoundingBox newSubmeshBoundingBox = localMeshObb.GetBoundingBox();
                ContainmentType contained = selectionBoxAabb.Contains(newSubmeshBoundingBox);
                if (contained != ContainmentType.Disjoint)
                {
                    Matrix scaleMatrix = Matrix.Scaling(transform.Scale);
                    Matrix rotationMatrix = Matrix.RotationQuaternion(normalizedQuaternion);
                    Matrix translationMatrix = Matrix.Translation(transform.Position);
                    Matrix worldMatrix = scaleMatrix * rotationMatrix * translationMatrix;
                    
                    foreach (Vector3 vertex in submesh.Vertices)
                    {
                        Vector4 scaledVector = Vector4.Transform(new Vector4(vertex, 1.0f), scaleMatrix);
                        Vector4 rotatedVector = Vector4.Transform(scaledVector, rotationMatrix);
                        Vector4 translatedVectorTest = Vector4.Transform(rotatedVector, translationMatrix);
                        ContainmentType vertexContained = selectionBoxOBB.Contains(Vec4toVec3(translatedVectorTest));
                        if (vertexContained != ContainmentType.Disjoint)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    public static bool IsCollisonMeshInsideBox(AbbrMesh mesh, OrientedBoundingBox selectionBoxObb,
        BoundingBox selectionBoxAabb, AbbrSectorTransform actorTransform, AbbrSectorTransform shapeTransform)
    {
        Vector3 combinedScale = actorTransform.Scale * shapeTransform.Scale;
        Quaternion combinedRotation = actorTransform.Rotation * shapeTransform.Rotation;
        Vector3 combinedTranslation = actorTransform.Position + shapeTransform.Position;

        List<AbbrSectorTransform> transforms = new List<AbbrSectorTransform>();
        var transform = new AbbrSectorTransform()
        {
            Position = combinedTranslation,
            Rotation = combinedRotation,
            Scale = combinedScale
        };
        transforms.Add(transform);
        bool isInside = IsMeshInsideBox(mesh, selectionBoxObb, selectionBoxAabb, transforms);
        Logger.Debug("\n" +
                     $"Build Collision Mesh Scale with {actorTransform.Scale} * {shapeTransform.Scale} => {combinedScale}\n" +
                     $"Build Collision Mesh Rotation with {actorTransform.Rotation} * {shapeTransform.Rotation} => {combinedRotation}\n" +
                     $"Build Collision Mesh Position with {actorTransform.Position} * {shapeTransform.Position} => {combinedTranslation}\n" +
                     $"Mesh is inside: {isInside}.");
        return isInside;
    }

    public static bool IsCollisionBoxInsideBox(AbbrActorShapes shape, AbbrSectorTransform actorTransform, BoundingBox selectionBoxAabb, OrientedBoundingBox selectionBoxObb)
    {
        Vector3 combinedTranslation = shape.Transform.Position + actorTransform.Position;
        Vector3 combinedScale = shape.Transform.Scale * actorTransform.Scale;
        Quaternion combinedRotation = shape.Transform.Rotation * actorTransform.Rotation;
        combinedRotation.Normalize();
        Vector3 halfExtends = new Vector3(combinedScale.X / 2f, combinedScale.Y / 2f, combinedScale.Z / 2f);
        OrientedBoundingBox boxObb = new OrientedBoundingBox(new Vector3(-halfExtends.X, -halfExtends.Y, -halfExtends.Z), halfExtends);

        boxObb.Transform(Matrix.RotationQuaternion(combinedRotation));
        boxObb.Translate(combinedTranslation);
                                
        BoundingBox boxAABB = boxObb.GetBoundingBox();
        ContainmentType aabbContainmentType = selectionBoxAabb.Contains(boxAABB);
        bool isInside = false;
        if (aabbContainmentType != ContainmentType.Disjoint)
        {
            ContainmentType obbContainmentType =  selectionBoxObb.Contains(ref boxObb);
            if (obbContainmentType != ContainmentType.Disjoint)
            {
                isInside = true;
            }
        }
        Logger.Debug("\n" +
                     $"Build Collision Mesh Scale with {actorTransform.Scale} * {shape.Transform.Scale} => {combinedScale}\n" +
                     $"Build Collision Mesh Rotation with {actorTransform.Rotation} * {shape.Transform.Rotation} => {combinedRotation}\n" +
                     $"Build Collision Mesh Position with {actorTransform.Position} * {shape.Transform.Position} => {combinedTranslation}\n" +
                     $"Mesh is inside: {isInside}.");
        return isInside;
    }
    
    public static bool IsCollisionCapsuleInsideBox(AbbrActorShapes shape, AbbrSectorTransform actorTransform, BoundingBox selectionBoxAabb, OrientedBoundingBox selectionBoxObb)
    {
        float height = shape.Transform.Scale.Y + 2 * actorTransform.Scale.X;   
        Vector3 shapeSizeAsBox = new Vector3(shape.Transform.Scale.X * 2, shape.Transform.Scale.X * 2, height);
        
        Vector3 combinedTranslation = shape.Transform.Position + actorTransform.Position;
        Vector3 combinedScale = shape.Transform.Scale * shapeSizeAsBox;
        Quaternion combinedRotation = shape.Transform.Rotation * actorTransform.Rotation;
        combinedRotation.Normalize();
        Vector3 halfExtends = new Vector3(combinedScale.X / 2f, combinedScale.Y / 2f, combinedScale.Z / 2f);
        OrientedBoundingBox boxObb = new OrientedBoundingBox(new Vector3(0f - halfExtends.X, 0f - halfExtends.Y, 0f - halfExtends.Z), halfExtends);

        boxObb.Transform(Matrix.RotationQuaternion(combinedRotation));
        boxObb.Translate(combinedTranslation);
                                
        BoundingBox boxAABB = boxObb.GetBoundingBox();
        ContainmentType aabbContainmentType = selectionBoxAabb.Contains(boxAABB);
        bool isInside = false;
        if (aabbContainmentType != ContainmentType.Disjoint)
        {
            ContainmentType obbContainmentType =  selectionBoxObb.Contains(ref boxObb);
            if (obbContainmentType != ContainmentType.Disjoint)
            {
                isInside = true;
            }
        }
        Logger.Debug("\n" +
                     $"Build Collision Mesh Scale with {actorTransform.Scale} * {shape.Transform.Scale} => {combinedScale}\n" +
                     $"Build Collision Mesh Rotation with {actorTransform.Rotation} * {shape.Transform.Rotation} => {combinedRotation}\n" +
                     $"Build Collision Mesh Position with {actorTransform.Position} * {shape.Transform.Position} => {combinedTranslation}\n" +
                     $"Mesh is inside: {isInside}.");
        return isInside;
    }
}