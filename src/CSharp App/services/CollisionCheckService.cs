using System;
using SharpDX;
using VolumetricSelection2077.Models;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using HelixToolkit.Wpf.SharpDX;
using SharpGLTF.IO;

namespace VolumetricSelection2077.Services;

public static class CollisionCheckService
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
    
    public static bool IsMeshInsideBox(AbbrMesh mesh, OrientedBoundingBox selectionBoxOBB, BoundingBox selectionBoxAabb, AbbrSectorTransform[]? transforms, Matrix? matrixTransform = null)
    {
        static bool isInsidePrivate(AbbrSubMeshes submesh, OrientedBoundingBox selectionObb, BoundingBox selectionAabb, Matrix transform)
        {
            OrientedBoundingBox baseObb = new(submesh.BoundingBox);
            baseObb.Transform(transform);
            BoundingBox transformedAabb = baseObb.GetBoundingBox();
            
            ContainmentType aabbContainment = selectionAabb.Contains(transformedAabb);
            if (aabbContainment != ContainmentType.Disjoint)
            {
                foreach (var vertex in submesh.Vertices)
                {
                    Vector4 translatedVectorTest = Vector4.Transform(new(vertex, 1f), transform);
                    ContainmentType vertexContained = selectionObb.Contains(Vec4toVec3(translatedVectorTest));
                    if (vertexContained != ContainmentType.Disjoint)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        if (transforms == null && matrixTransform == null)
        {
            Logger.Error("IsMeshInsideBox: No transform provided, aborting.");
            return false;
        }

        if (matrixTransform != null)
        {
            foreach (var submesh in mesh.SubMeshes)
            {
                if (isInsidePrivate(submesh, selectionBoxOBB, selectionBoxAabb, (Matrix)matrixTransform))
                {
                    return true;
                }
            }
        }

        if (transforms != null)
        {
            foreach (var submesh in mesh.SubMeshes)
            {
                foreach (var transform in transforms)
                {
                    Matrix localTransformMatrix = Matrix.Scaling(transform.Scale) * 
                                                  Matrix.RotationQuaternion(transform.Rotation) * 
                                                  Matrix.Translation(transform.Position);
                    if (isInsidePrivate(submesh, selectionBoxOBB, selectionBoxAabb, localTransformMatrix))
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }

    public static bool IsCollisonMeshInsideSelectionBox(AbbrMesh mesh, OrientedBoundingBox selectionBoxObb,
        BoundingBox selectionBoxAabb, AbbrSectorTransform actorTransform, AbbrSectorTransform shapeTransform)
    {
        // only working way to apply actor and shape transform
        Matrix shapeTransformMatrix = Matrix.Scaling(shapeTransform.Scale) * 
                                      Matrix.RotationQuaternion(shapeTransform.Rotation) * 
                                      Matrix.Translation(shapeTransform.Position);

        Matrix actorTransformMatrix = Matrix.Scaling(actorTransform.Scale) * 
                                      Matrix.RotationQuaternion(actorTransform.Rotation) * 
                                      Matrix.Translation(actorTransform.Position);

        Matrix transformMatrix = shapeTransformMatrix * actorTransformMatrix;
        
        return IsMeshInsideBox(mesh, selectionBoxObb, selectionBoxAabb,null,  transformMatrix);
        /* Logger.Debug("\n" +
                     $"Build Collision Mesh Scale with {actorTransform.Scale} * {shapeTransform.Scale} => {combinedScale}\n" +
                     $"Build Collision Mesh Rotation with {actorTransform.Rotation} * {shapeTransform.Rotation} => {combinedRotation}\n" +
                     $"Build Collision Mesh Position with {actorTransform.Position} * {shapeTransform.Position} => {combinedTranslation}\n" +
                     $"Mesh is inside: {isInside}."); */
    }

    public static bool IsCollisionBoxInsideSelectionBox(AbbrActorShapes shape, AbbrSectorTransform actorTransform, BoundingBox selectionBoxAabb, OrientedBoundingBox selectionBoxObb, string collectionName) // the collectionName is just for test building the blender collections
    {
        // only working way to apply actor and shape transform
        Matrix shapeTransformMatrix = Matrix.Scaling(new Vector3(1,1,1)) * 
                                Matrix.RotationQuaternion(shape.Transform.Rotation) * 
                                Matrix.Translation(shape.Transform.Position);

        Matrix actorTransformMatrix = Matrix.Scaling(actorTransform.Scale) * 
                                Matrix.RotationQuaternion(actorTransform.Rotation) * 
                                Matrix.Translation(actorTransform.Position);

        Matrix transformMatrix = shapeTransformMatrix * actorTransformMatrix;

        OrientedBoundingBox collisionBox = new(-shape.Transform.Scale, shape.Transform.Scale);
        collisionBox.Transform(transformMatrix);
        BoundingBox collisionBoxAabb = collisionBox.GetBoundingBox();
        
        bool isInside = false;
        ContainmentType aabbContainmentType = selectionBoxAabb.Contains(collisionBoxAabb);
        if (aabbContainmentType != ContainmentType.Disjoint)
        {
            ContainmentType obbContainmentType =  selectionBoxObb.Contains(ref collisionBox);
            if (obbContainmentType != ContainmentType.Disjoint)
            {
                isInside = true;
            }
        }
        /*
        if (isInside)
        {
            collectionName = "true " + collectionName;
        }
        else
        {
            collectionName = "false " + collectionName;
        }
        
        string uniqueId = DateTime.UtcNow.Ticks.ToString();
        
        string collisionBoxString = $"collisionBoxVerts{uniqueId} = [ ";
        var vertsCollisionBox = collisionBox.GetCorners();
        foreach (var v in vertsCollisionBox)
        {
            collisionBoxString +=
                $"({v.X.ToString(CultureInfo.InvariantCulture)}, {v.Y.ToString(CultureInfo.InvariantCulture)}, {v.Z.ToString(CultureInfo.InvariantCulture)}),";
        }

        collisionBoxString +=
            $"]\n" +
            $"collisionBox{uniqueId} = create_box(\"collisionBox{uniqueId}\", collisionBoxVerts{uniqueId}, \"{collectionName}\")\n";
        
        /*
        string selectionBoxString = $"selectionBoxVerts{uniqueId} = [ ";
        var vertsSelectionBox = selectionBoxObb.GetCorners();
        foreach (var v in vertsSelectionBox)
        {
            selectionBoxString +=
                $"({v.X.ToString(CultureInfo.InvariantCulture)}, {v.Y.ToString(CultureInfo.InvariantCulture)}, {v.Z.ToString(CultureInfo.InvariantCulture)}),";
        }

        selectionBoxString +=
            $"]\n" +
            $"selectionBox{uniqueId} = create_box(\"selectionBox{uniqueId}\", selectionBoxVerts{uniqueId}, \"{collectionName}\")\n";
        */
        // Logger.Debug(collisionBoxString /*+ selectionBoxString */);
        
        return isInside;
    }
    
    public static bool IsCollisionCapsuleInsideSelectionBox(AbbrActorShapes shape, AbbrSectorTransform actorTransform, BoundingBox selectionBoxAabb, OrientedBoundingBox selectionBoxObb)
    {
        float height = shape.Transform.Scale.Y + 2 * actorTransform.Scale.X;   
        Vector3 shapeSizeAsBox = new Vector3(shape.Transform.Scale.X, shape.Transform.Scale.X, height / 2f);
        
        // only working way to apply actor and shape transform
        Matrix shapeTransformMatrix = Matrix.Scaling(new Vector3(1,1,1)) * 
                                      Matrix.RotationQuaternion(shape.Transform.Rotation) * 
                                      Matrix.Translation(shape.Transform.Position);

        Matrix actorTransformMatrix = Matrix.Scaling(actorTransform.Scale) * 
                                      Matrix.RotationQuaternion(actorTransform.Rotation) * 
                                      Matrix.Translation(actorTransform.Position);

        Matrix transformMatrix = shapeTransformMatrix * actorTransformMatrix;
        
        OrientedBoundingBox capsuleObb = new(-shapeSizeAsBox, shapeSizeAsBox);
        capsuleObb.Transform(transformMatrix);
        
        BoundingBox capsuleAabb = capsuleObb.GetBoundingBox();
        ContainmentType aabbContainmentType = selectionBoxAabb.Contains(capsuleAabb);
        bool isInside = false;
        if (aabbContainmentType != ContainmentType.Disjoint)
        {
            ContainmentType obbContainmentType =  selectionBoxObb.Contains(ref capsuleObb);
            if (obbContainmentType != ContainmentType.Disjoint)
            {
                isInside = true;
            }
        }
        /*
        Logger.Debug("\n" +
                     $"Build Collision Mesh Scale with {actorTransform.Scale} * {shape.Transform.Scale} => {combinedScale}\n" +
                     $"Build Collision Mesh Rotation with {actorTransform.Rotation} * {shape.Transform.Rotation} => {combinedRotation}\n" +
                     $"Build Collision Mesh Position with {actorTransform.Position} * {shape.Transform.Position} => {combinedTranslation}\n" +
                     $"Mesh is inside: {isInside}.");
                     */
        return isInside;
    }
}