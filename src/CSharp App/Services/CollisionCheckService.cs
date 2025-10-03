using System;
using SharpDX;
using VolumetricSelection2077.Models;
using System.Collections.Generic;
using System.Linq;
using OrientedBoundingBox = SharpDX.OrientedBoundingBox;
using Plane = SharpDX.Plane;
using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace VolumetricSelection2077.Services;

public static class CollisionCheckService
{
    /// <summary>
    /// Checks if a mesh node has overlap with the obb
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="selectionBoxOBB"></param>
    /// <param name="selectionBoxAabb"></param>
    /// <param name="transforms"></param>
    /// <param name="matrixTransform"></param>
    /// <param name="checkAllTransforms"></param>
    /// <returns></returns>
    public static (bool, List<int>) IsMeshInsideBox(AbbrMesh mesh, OrientedBoundingBox selectionBoxOBB, BoundingBox selectionBoxAabb, AbbrSectorTransform[]? transforms, Matrix? matrixTransform = null, bool checkAllTransforms = false)
    {
        var outInstances = new List<int>();
        
        if (transforms == null && matrixTransform == null)
        {
            Logger.Error("IsMeshInsideBox: No transform provided, aborting.");
            return (false, outInstances);
        }

        if (matrixTransform != null)
        {
            foreach (var submesh in mesh.SubMeshes)
            {
                if (!IsSubMeshInsideBox(submesh, selectionBoxOBB, selectionBoxAabb, (Matrix)matrixTransform))
                    continue;
                if (!checkAllTransforms)
                    return (true, outInstances);
                outInstances.Add(0);
            }
        }

        if (transforms != null)
        {
            var transformIndex = 0;
            foreach (var transform in transforms)
            {
                foreach (var submesh in mesh.SubMeshes)
                {
                    Matrix localTransformMatrix = Matrix.Scaling(transform.Scale) * 
                                                  Matrix.RotationQuaternion(transform.Rotation) * 
                                                  Matrix.Translation(transform.Position);
                    if (!IsSubMeshInsideBox(submesh, selectionBoxOBB, selectionBoxAabb, localTransformMatrix))
                        continue;
                    if (!checkAllTransforms)
                        return (true, outInstances);
                    outInstances.Add(transformIndex);
                }
                transformIndex++;
            }
        }
        
        return (outInstances.Count > 0, outInstances);
    }

    /// <summary>
    /// Checks if a collision mesh actor is overlapping with the obb
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="selectionBoxObb"></param>
    /// <param name="selectionBoxAabb"></param>
    /// <param name="actorTransform"></param>
    /// <param name="shapeTransform"></param>
    /// <returns></returns>
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
        
        return IsMeshInsideBox(mesh, selectionBoxObb, selectionBoxAabb,null,  transformMatrix).Item1;
        /* Logger.Debug("\n" +
                     $"Build Collision Mesh Scale with {actorTransform.Scale} * {shapeTransform.Scale} => {combinedScale}\n" +
                     $"Build Collision Mesh Rotation with {actorTransform.Rotation} * {shapeTransform.Rotation} => {combinedRotation}\n" +
                     $"Build Collision Mesh Position with {actorTransform.Position} * {shapeTransform.Position} => {combinedTranslation}\n" +
                     $"Mesh is inside: {isInside}."); */
    }

    /// <summary>
    /// Checks if a collision box actor overlaps with the obb
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="actorTransform"></param>
    /// <param name="selectionBoxAabb"></param>
    /// <param name="selectionBoxObb"></param>
    /// <returns></returns>
    public static bool IsCollisionBoxInsideSelectionBox(AbbrActorShapes shape, AbbrSectorTransform actorTransform, BoundingBox selectionBoxAabb, OrientedBoundingBox selectionBoxObb)
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
    
    /// <summary>
    /// Checks if a collision capsule actor overlaps with the obb
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="actorTransform"></param>
    /// <param name="selectionBoxAabb"></param>
    /// <param name="selectionBoxObb"></param>
    /// <returns></returns>
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
    
    /// <summary>
    /// Checks if a collision sphere actor overlaps with the obb
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="actorTransform"></param>
    /// <param name="selectionBoxObb"></param>
    /// <returns></returns>
    public static bool IsCollisionSphereInsideSelectionBox(AbbrActorShapes shape, AbbrSectorTransform actorTransform,
        OrientedBoundingBox selectionBoxObb)
    {
        Matrix shapeTransformMatrix = Matrix.Scaling(new Vector3(1,1,1)) * 
                                      Matrix.RotationQuaternion(shape.Transform.Rotation) * 
                                      Matrix.Translation(shape.Transform.Position);

        Matrix actorTransformMatrix = Matrix.Scaling(new Vector3(1,1,1)) * 
                                      Matrix.RotationQuaternion(actorTransform.Rotation) * 
                                      Matrix.Translation(actorTransform.Position);

        Matrix transformMatrix = shapeTransformMatrix * actorTransformMatrix;
        
        Vector4 transformedSpherePosition = Vector4.Transform(Vector3.Transform(new Vector3(0,0,0), transformMatrix), Matrix.Invert(selectionBoxObb.Transformation));
        
        var collisionSphere = new BoundingSphere(Vec4ToVec3(transformedSpherePosition), shape.Transform.Scale.X);
        
        var selectionAsAABB = new BoundingBox(-selectionBoxObb.Size / 2, selectionBoxObb.Size / 2);
        return selectionAsAABB.Contains(ref collisionSphere) != ContainmentType.Disjoint;
    }
    
    /// <summary>
    /// Checks if a submesh overlaps with the obb
    /// </summary>
    /// <param name="submesh"></param>
    /// <param name="obb"></param>
    /// <returns></returns>
    private static bool CheckOverlapSubMeshBox(AbbrSubMesh submesh, OrientedBoundingBox obb)
    {
        return submesh.PolygonIndices.Any(poly => CheckOverlapPolygonBox(poly, submesh.Vertices, obb));
    }
    
    /// <summary>
    /// Checks for overlap (containment and intersection) between a polygon with n edges and an obb
    /// </summary>
    /// <param name="polygon"></param>
    /// <param name="vertices"></param>
    /// <param name="obb"></param>
    /// <returns></returns>
    private static bool CheckOverlapPolygonBox(uint[] polygon, Vector3[] vertices, OrientedBoundingBox obb)
    {
        var polyVerts = polygon.Select(index => vertices[index]).ToArray();
        
        var obbAxes = GetObbAxes(obb);
        var obbHalfExtends = obb.Size / 2;
        List<Vector3> axes = new List<Vector3>();
        
        axes.AddRange(obbAxes);
        var plane = new Plane(polyVerts[0], polyVerts[1], polyVerts[2]);
        axes.Add(plane.Normal);
        
        for (int i = 0; i < polyVerts.Length; i++)
        {
            Vector3 edge = polyVerts[(i + 1) % polyVerts.Length] - polyVerts[i];
            foreach (var obbAxis in obbAxes)
            {
                Vector3 crossAxis = Vector3.Cross(edge, obbAxis);
                if (crossAxis.LengthSquared() > 1e-6f) // Avoid near-zero vectors
                    axes.Add(Vector3.Normalize(crossAxis));
            }
        }
        
        foreach (Vector3 axis in axes)
        {
            if (axis.LengthSquared() < 1e-6f)
                continue;
            float r =
                Math.Abs(Vector3.Dot(obbAxes[0] * obbHalfExtends.X, axis)) +
                Math.Abs(Vector3.Dot(obbAxes[1] * obbHalfExtends.Y, axis)) +
                Math.Abs(Vector3.Dot(obbAxes[2] * obbHalfExtends.Z, axis));
            
            ProjectionInterval obbInterval = new ProjectionInterval(-r, r);
            
            float minPoly = float.MaxValue;
            float maxPoly = float.MinValue;
        
            foreach (Vector3 vertex in polyVerts)
            {
                float projection = Vector3.Dot(vertex - obb.Center, axis);
                minPoly = Math.Min(minPoly, projection);
                maxPoly = Math.Max(maxPoly, projection);
            }
            
            ProjectionInterval polyInterval = new ProjectionInterval(minPoly, maxPoly);
            if (!obbInterval.Overlaps(polyInterval))
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Returns an OBBs 3 axes
    /// </summary>
    /// <param name="obb"></param>
    /// <returns></returns>
    private static Vector3[] GetObbAxes(OrientedBoundingBox obb)
    {
        var x = new Vector3(obb.Transformation.M11, obb.Transformation.M12, obb.Transformation.M13);
        var y = new Vector3(obb.Transformation.M21, obb.Transformation.M22, obb.Transformation.M23);
        var z = new Vector3(obb.Transformation.M31, obb.Transformation.M32, obb.Transformation.M33);
        
        return new[]
        {
            x, y, z
        };
    }
    
    /// <summary>
    /// Converts a sharpdx vector4 to vector3
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    private static Vector3 Vec4ToVec3(Vector4 v)
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
    
    private static bool IsSubMeshInsideBox(AbbrSubMesh submesh, OrientedBoundingBox selectionObb, BoundingBox selectionAabb, Matrix transform)
    {
        OrientedBoundingBox baseObb = new(submesh.BoundingBox);
        baseObb.Transform(transform);
        BoundingBox transformedAabb = baseObb.GetBoundingBox();
            
        ContainmentType aabbContainment = selectionAabb.Contains(transformedAabb);
        if (aabbContainment != ContainmentType.Disjoint)
        {
            if (submesh.Vertices == null || submesh.PolygonIndices == null)
                return true;
                
            var adjustedSubmesh = new AbbrSubMesh()
            {
                BoundingBox = submesh.BoundingBox,
                Vertices = new Vector3[submesh.Vertices.Length],
                PolygonIndices = submesh.PolygonIndices
            };
            for (int i = 0; i < submesh.Vertices.Length; i++)
            {
                Vector4 translatedVertex = Vector4.Transform(new(submesh.Vertices[i], 1f), transform);
                adjustedSubmesh.Vertices[i] = Vec4ToVec3(translatedVertex);
            }
            if (CheckOverlapSubMeshBox(adjustedSubmesh, selectionObb))
                return true;
        }
        return false;
    }
}