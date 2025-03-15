using System;
using SharpDX;
using VolumetricSelection2077.Models;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using HelixToolkit.Wpf.SharpDX;
using SharpGLTF.IO;
using Geometry3D = HelixToolkit.SharpDX.Core.Geometry3D;

namespace VolumetricSelection2077.Services;

public static class CollisionCheckService
{
    private const float Epsilon = 1e-5f;
    public static bool CheckIntersectionBoxTri(Vector3[] triangle, OrientedBoundingBox obb)
    {
        if (triangle == null || triangle.Length != 3)
            throw new ArgumentException("Triangle must consist of exactly 3 points");

        Vector3 v0 = triangle[0];
        Vector3 v1 = triangle[1];
        Vector3 v2 = triangle[2];

        Vector3 e0 = v1 - v0;
        Vector3 e1 = v2 - v1;
        Vector3 e2 = v0 - v2;
        
        Vector3 triangleNormal = Vector3.Cross(e0, e2);
        triangleNormal = Vector3.Normalize(triangleNormal);
        
        Vector3 obbCenter = obb.Center;
        Vector3 halfExtents = obb.Size / 2;
        
        Vector3[] obbAxes = new Vector3[3]
        {
            new Vector3(obb.Transformation.M11, obb.Transformation.M12, obb.Transformation.M13), // X axis
            new Vector3(obb.Transformation.M21, obb.Transformation.M22, obb.Transformation.M23), // Y axis
            new Vector3(obb.Transformation.M31, obb.Transformation.M32, obb.Transformation.M33)  // Z axis
        };

        /*
        //Validate that the OBB axes are orthogonal and normalized
        if (Math.Abs(Vector3.Dot(obbAxes[0], obbAxes[1])) > Epsilon ||
            Math.Abs(Vector3.Dot(obbAxes[0], obbAxes[2])) > Epsilon ||
            Math.Abs(Vector3.Dot(obbAxes[1], obbAxes[2])) > Epsilon ||
            Math.Abs(obbAxes[0].LengthSquared() - 1f) > Epsilon ||
            Math.Abs(obbAxes[1].LengthSquared() - 1f) > Epsilon ||
            Math.Abs(obbAxes[2].LengthSquared() - 1f) > Epsilon)
        {
            // The OBB transformation is invalid.  Handle this error.
            // Possible solutions:
            // 1. Re-normalize the axes (if the matrix is "close" to valid).
            // 2. Use the OBB's rotation component (e.g., a Quaternion) to
            // define the axes, instead of relying on the matrix.
            // 3. Return false (no intersection) as the safest option.
            Logger.Error("Invalid OBB Transformation");
            return false;
        }
        */
        
        Vector3[] axes = new Vector3[12];
        
        axes[0] = obbAxes[0];
        axes[1] = obbAxes[1];
        axes[2] = obbAxes[2];
        
        int axisIndex = 3;

        
        Vector3[] edgeAxes =
        {
            Vector3.Cross(e0, obbAxes[0]),
            Vector3.Cross(e0, obbAxes[1]),
            Vector3.Cross(e0, obbAxes[2]),
            Vector3.Cross(e1, obbAxes[0]),
            Vector3.Cross(e1, obbAxes[1]),
            Vector3.Cross(e1, obbAxes[2]),
            Vector3.Cross(e2, obbAxes[0]),
            Vector3.Cross(e2, obbAxes[1]),
            Vector3.Cross(e2, obbAxes[2])
        };

        foreach (var edgeAxis in edgeAxes)
        {
            if (edgeAxis.LengthSquared() > 1e-6f)  // Skip degenerate axes
                axes[axisIndex++] = Vector3.Normalize(edgeAxis);
        }
        
        foreach (Vector3 axis in axes)
        {
            
            if (axis.LengthSquared() < 1e-6f)  
                continue;
            
            float triMin = float.MaxValue, triMax = float.MinValue;
            foreach (Vector3 vertex in triangle)
            {
                float projection = Vector3.Dot(vertex - obbCenter, axis);
                triMin = Math.Min(triMin, projection);
                triMax = Math.Max(triMax, projection);
            }
            
            float r =
                Math.Abs(Vector3.Dot(obbAxes[0] * halfExtents.X, axis)) +
                Math.Abs(Vector3.Dot(obbAxes[1] * halfExtents.Y, axis)) +
                Math.Abs(Vector3.Dot(obbAxes[2] * halfExtents.Z, axis));

            float boxMin = -r;
            float boxMax = r;
            
            /*
            // ********************  DEBUGGING ********************
            Logger.Info($"Axis: {axis}");
            Logger.Info($"TriMin: {triMin}, TriMax: {triMax}");
            Logger.Info($"BoxMin: {boxMin}, BoxMax: {boxMax}");
            Logger.Info($"r: {r}");
            Logger.Info($"Dot Products: " +
                              $"{Vector3.Dot(obbAxes[0] * halfExtents.X, axis)}, " +
                              $"{Vector3.Dot(obbAxes[1] * halfExtents.Y, axis)}, " +
                              $"{Vector3.Dot(obbAxes[2] * halfExtents.Z, axis)}");
            // ********************  DEBUGGING ********************
            */
            if (triMax < boxMin - Epsilon || triMin > boxMax + Epsilon)
            {
                // Logger.Info("Returned False");
                return false; 
            }
        }
        // Logger.Info("Returned True");
        return true; 
    }

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
        static bool IsInsidePrivate(AbbrSubMeshes submesh, OrientedBoundingBox selectionObb, BoundingBox selectionAabb, Matrix transform)
        {
            OrientedBoundingBox baseObb = new(submesh.BoundingBox);
            baseObb.Transform(transform);
            BoundingBox transformedAabb = baseObb.GetBoundingBox();
            
            ContainmentType aabbContainment = selectionAabb.Contains(transformedAabb);
            if (aabbContainment != ContainmentType.Disjoint)
            {
                /*
                foreach (var vertex in submesh.Vertices)
                {
                    Vector4 translatedVectorTest = Vector4.Transform(new(vertex, 1f), transform);
                    ContainmentType vertexContained = selectionObb.Contains(Vec4toVec3(translatedVectorTest));
                    if (vertexContained != ContainmentType.Disjoint)
                    {
                        return true;
                    }
                }
                */
                if (submesh.Indices.Length % 3 != 0) throw new Exception("Invalid submesh indices.");
                for (int i = 0; i < submesh.Indices.Length; i += 3)
                {
                    if (CheckIntersectionBoxTri(new Vector3[]{submesh.Vertices[submesh.Indices[i]], submesh.Vertices[submesh.Indices[i + 1]], submesh.Vertices[submesh.Indices[i + 2]]}, baseObb))
                        return true;
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
                if (IsInsidePrivate(submesh, selectionBoxOBB, selectionBoxAabb, (Matrix)matrixTransform))
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
                    if (IsInsidePrivate(submesh, selectionBoxOBB, selectionBoxAabb, localTransformMatrix))
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
        return isInside;
    }
}