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
        BoundingBox selectionBoxAabb, AbbrSectorTransform nodeTransform, AbbrSectorTransform actorTransform, AbbrSectorTransform shapeTransform)
    {
        Vector3 combinedScale = nodeTransform.Scale * actorTransform.Scale * shapeTransform.Scale;
        Quaternion combinedRotation = nodeTransform.Rotation * actorTransform.Rotation * shapeTransform.Rotation;
        Vector3 combinedTranslation = nodeTransform.Position + actorTransform.Position + shapeTransform.Position;

        List<AbbrSectorTransform> transforms = new List<AbbrSectorTransform>();
        var transform = new AbbrSectorTransform()
        {
            Position = combinedTranslation,
            Rotation = combinedRotation,
            Scale = combinedScale
        };
        transforms.Add(transform);
        return IsMeshInsideBox(mesh, selectionBoxObb, selectionBoxAabb, transforms);
    }

    public static bool IsCollisionBoxInsideBox(AbbrActorShapes shape, AbbrSectorTransform actorTransform, BoundingBox selectionBoxAabb, OrientedBoundingBox selectionBoxObb, AbbrSectorTransform nodeTransform)
    {
        Vector3 shapePosition = shape.Transform.Position + actorTransform.Position + nodeTransform.Position;
        Vector3 shapeScale = shape.Transform.Scale * actorTransform.Scale * nodeTransform.Scale;
        Quaternion shapeRotation = shape.Transform.Rotation * actorTransform.Rotation * nodeTransform.Rotation;
        shapeRotation.Normalize();
        Vector3 halfExtends = new Vector3(shapeScale.X / 2f, shapeScale.Y / 2f, shapeScale.Z / 2f);
        OrientedBoundingBox boxObb = new OrientedBoundingBox(new Vector3(-halfExtends.X, -halfExtends.Y, -halfExtends.Z), halfExtends);

        boxObb.Transform(Matrix.RotationQuaternion(shapeRotation));
        boxObb.Translate(shapePosition);
                                
        BoundingBox boxAABB = boxObb.GetBoundingBox();
        ContainmentType aabbContainmentType = selectionBoxAabb.Contains(boxAABB);
        if (aabbContainmentType != ContainmentType.Disjoint)
        {
            ContainmentType obbContainmentType =  selectionBoxObb.Contains(ref boxObb);
            if (obbContainmentType != ContainmentType.Disjoint)
            {
                return true;
            }
        }
        return false;
    }
    
    public static bool IsCollisionCapsuleInsideBox(AbbrActorShapes shape, AbbrSectorTransform actorTransform, BoundingBox selectionBoxAabb, OrientedBoundingBox selectionBoxObb, AbbrSectorTransform nodeTransform)
    {
        float height = shape.Transform.Scale.Y + 2 * actorTransform.Scale.X * nodeTransform.Scale.Z;   
        Vector3 shapeSizeAsBox = new Vector3(shape.Transform.Scale.X * 2, shape.Transform.Scale.X * 2, height);
        
        Vector3 combinedPosition = shape.Transform.Position + actorTransform.Position + nodeTransform.Position;
        Vector3 combinedScale = shape.Transform.Scale * shapeSizeAsBox * nodeTransform.Scale;
        Quaternion combinedRotation = shape.Transform.Rotation * actorTransform.Rotation * nodeTransform.Rotation;
        combinedRotation.Normalize();
        Vector3 halfExtends = new Vector3(combinedScale.X / 2f, combinedScale.Y / 2f, combinedScale.Z / 2f);
        OrientedBoundingBox boxObb = new OrientedBoundingBox(new Vector3(0f - halfExtends.X, 0f - halfExtends.Y, 0f - halfExtends.Z), halfExtends);

        boxObb.Transform(Matrix.RotationQuaternion(combinedRotation));
        boxObb.Translate(combinedPosition);
                                
        BoundingBox boxAABB = boxObb.GetBoundingBox();
        ContainmentType aabbContainmentType = selectionBoxAabb.Contains(boxAABB);
        if (aabbContainmentType != ContainmentType.Disjoint)
        {
            ContainmentType obbContainmentType =  selectionBoxObb.Contains(ref boxObb);
            if (obbContainmentType != ContainmentType.Disjoint)
            {
                return true;
            }
        }
        return false;
    }
}