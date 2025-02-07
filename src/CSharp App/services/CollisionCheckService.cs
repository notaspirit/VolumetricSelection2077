using System;
using SharpDX;
using VolumetricSelection2077.Models;
using System.Collections.Generic;
using System.Globalization;
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
            foreach (var transform in transforms) // are the transforms it receives correct?
            {
                Quaternion normalizedQuaternion = transform.Rotation;
                OrientedBoundingBox localMeshObb = new OrientedBoundingBox(submesh.BoundingBox);
                
                normalizedQuaternion.Normalize();
                Matrix meshRotationMatrix = Matrix.RotationQuaternion(normalizedQuaternion);
                
                Vector3 originalCenter = localMeshObb.Center;
                Vector3 extends = localMeshObb.Size;
                localMeshObb.Scale(transform.Scale);
                Vector3 afterScale = localMeshObb.Center;
                localMeshObb.Transform(meshRotationMatrix);
                Vector3 afterTransform = localMeshObb.Center;
                localMeshObb.Translate(transform.Position);
                /*
                Logger.Debug($"\n" +
                             $"Original Mesh OBB Center: {originalCenter}\n" +
                             $"Original Mesh OBB Extends: {extends}" +
                             $"After Scale ({transform.Scale}): {afterScale}\n" +
                             $"After Transform ({transform.Rotation}): {afterTransform}" +
                             $"After Translate ({transform.Position}): {localMeshObb.Center}");
                             */
                
                BoundingBox newSubmeshBoundingBox = localMeshObb.GetBoundingBox();

                ContainmentType contained = selectionBoxAabb.Contains(newSubmeshBoundingBox);
                
                // ContainmentType contained = selectionBoxOBB.Contains(ref localMeshObb);
                if (contained != ContainmentType.Disjoint)
                {
                    // return early for testing 
                    //return true;
                    // this part works as expected (both because I extensively tested it and because the issue persists even when just checking OBB vs OBB)
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

    public static bool IsCollisonMeshInsideSelectionBox(AbbrMesh mesh, OrientedBoundingBox selectionBoxObb,
        BoundingBox selectionBoxAabb, AbbrSectorTransform actorTransform, AbbrSectorTransform shapeTransform)
    {
        return true;
        Vector3 combinedScale = actorTransform.Scale * shapeTransform.Scale;
        Quaternion normalizedShapeRotation = shapeTransform.Rotation;
        normalizedShapeRotation.Normalize();

        Quaternion normalizedActorRotation = actorTransform.Rotation;
        normalizedActorRotation.Normalize();
        
        Quaternion combinedRotation = normalizedShapeRotation * normalizedActorRotation;
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
        /* Logger.Debug("\n" +
                     $"Build Collision Mesh Scale with {actorTransform.Scale} * {shapeTransform.Scale} => {combinedScale}\n" +
                     $"Build Collision Mesh Rotation with {actorTransform.Rotation} * {shapeTransform.Rotation} => {combinedRotation}\n" +
                     $"Build Collision Mesh Position with {actorTransform.Position} * {shapeTransform.Position} => {combinedTranslation}\n" +
                     $"Mesh is inside: {isInside}."); */
        return isInside;
    }

    public static bool IsCollisionBoxInsideSelectionBox(AbbrActorShapes shape, AbbrSectorTransform actorTransform, BoundingBox selectionBoxAabb, OrientedBoundingBox selectionBoxObb, string collectionName) // the collectionName is just for test building the blender collections
    {
        // Vector3 combinedTranslation = shape.Transform.Position + actorTransform.Position;
        Vector3 combinedTranslation = Vector3.Add(actorTransform.Position, shape.Transform.Position);
        /*
         Logger.Debug($"\n" +
                     $"Building Collision Box with:" +
                     $"Actor Position: {actorTransform.Position}" +
                     $"Shape Position: {shape.Transform.Position}" +
                     $"Combined Position: {combinedTranslation}");
        */
        // Vector3 combinedScale = shape.Transform.Scale * actorTransform.Scale;
        Vector3 combinedScale = Vector3.Multiply(actorTransform.Scale, shape.Transform.Scale);

        Quaternion normalizedShapeRotation = shape.Transform.Rotation;
        normalizedShapeRotation.Normalize();

        Quaternion normalizedActorRotation = actorTransform.Rotation;
        normalizedActorRotation.Normalize();
        
        // Quaternion combinedRotation = normalizedShapeRotation * normalizedActorRotation;
        Quaternion.Multiply(ref normalizedActorRotation, ref normalizedShapeRotation, out Quaternion combinedRotation);
        OrientedBoundingBox boxObb = new OrientedBoundingBox(-combinedScale, combinedScale);
        
        boxObb.Transform(Matrix.RotationQuaternion(combinedRotation));
        boxObb.Translate(combinedTranslation);
        
        // for testing only
        
        /*
        var verts = boxObb.GetCorners();
        string blenderVerticies = "verts = [\n";
        foreach (var vertex in verts)
        {
            blenderVerticies += $"bm.verts.new(({vertex.X.ToString(CultureInfo.InvariantCulture)}, " +
                                $"{vertex.Y.ToString(CultureInfo.InvariantCulture)}, " +
                                $"{vertex.Z.ToString(CultureInfo.InvariantCulture)})),\n";
        }
        blenderVerticies += "]";
        Logger.Debug($"\n" +
                     $"{blenderVerticies}");
        */
        
        
        // testing end

        bool isInside = false;
        
        BoundingBox boxAABB = boxObb.GetBoundingBox();
        ContainmentType aabbContainmentType = selectionBoxAabb.Contains(boxAABB);
        if (aabbContainmentType != ContainmentType.Disjoint)
        {
            ContainmentType obbContainmentType =  selectionBoxObb.Contains(ref boxObb);
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
        var vertsCollisionBox = boxObb.GetCorners();
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
        Logger.Debug(collisionBoxString /*+ selectionBoxString */);
        
        return isInside;
    }
    
    public static bool IsCollisionCapsuleInsideSelectionBox(AbbrActorShapes shape, AbbrSectorTransform actorTransform, BoundingBox selectionBoxAabb, OrientedBoundingBox selectionBoxObb)
    {
        return true;
        float height = shape.Transform.Scale.Y + 2 * actorTransform.Scale.X;   
        Vector3 shapeSizeAsBox = new Vector3(shape.Transform.Scale.X * 2, shape.Transform.Scale.X * 2, height);
        
        Vector3 combinedTranslation = shape.Transform.Position + actorTransform.Position;
        Vector3 combinedScale = shape.Transform.Scale * shapeSizeAsBox;
        Quaternion normalizedShapeRotation = shape.Transform.Rotation;
        normalizedShapeRotation.Normalize();

        Quaternion normalizedActorRotation = actorTransform.Rotation;
        normalizedActorRotation.Normalize();
        
        Quaternion combinedRotation = normalizedShapeRotation * normalizedActorRotation;
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