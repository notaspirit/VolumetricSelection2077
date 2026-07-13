using System;
using SharpDX;
using VolumetricSelection2077.Models;
using System.Collections.Generic;
using System.Linq;
using HelixToolkit.SharpDX.Core;
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
    /// <param name="selectionBoxObb"></param>
    /// <param name="selectionBoxAabb"></param>
    /// <param name="transforms"></param>
    /// <param name="checkAllTransforms"></param>
    /// <returns></returns>
    public static List<int> IsMeshInsideBox(AbbrMesh mesh, OrientedBoundingBox selectionBoxObb, BoundingBox selectionBoxAabb, Matrix[] transforms, bool checkAllTransforms = false)
    {
        var outInstances = new List<int>();
        var transformIndex = 0;
        foreach (var transform in transforms)
        {
            foreach (var submesh in mesh.SubMeshes)
            {
                if (!IsSubMeshInsideBox(submesh, selectionBoxObb, selectionBoxAabb, transform))
                    continue;
                
                outInstances.Add(transformIndex);
                
                if (!checkAllTransforms)
                    return outInstances;
            }
            transformIndex++;
        }
        
        return outInstances;
    }

    /// <summary>
    /// Checks if a collision box actor overlaps with the obb
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="selectionBoxAabb"></param>
    /// <param name="selectionBoxObb"></param>
    /// <returns></returns>
    public static bool IsCollisionBoxInsideSelectionBox(AbbrActorShapes shape, BoundingBox selectionBoxAabb, OrientedBoundingBox selectionBoxObb)
    {

        OrientedBoundingBox collisionBox = new(-shape.Scale, shape.Scale);
        collisionBox.Transform(shape.Transform);
        var collisionBoxAabb = collisionBox.GetBoundingBox();

        return selectionBoxAabb.Contains(collisionBoxAabb) == ContainmentType.Disjoint &&
               selectionBoxObb.Contains(ref collisionBox) != ContainmentType.Disjoint;
    }
    
    /// <summary>
    /// Checks if a collision capsule actor overlaps with the obb
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="actorTransform"></param>
    /// <param name="selectionBoxAabb"></param>
    /// <param name="selectionBoxObb"></param>
    /// <returns></returns>
    public static bool IsCollisionCapsuleInsideSelectionBox(AbbrActorShapes shape, BoundingBox selectionBoxAabb, OrientedBoundingBox selectionBoxObb)
    {
        // M11 = ScaleVector.X
        // shape transform is the combination of actor and shape transforms, with the shape scale we can calculate
        // the actor scale
        var actorScaleX = Math.Abs(shape.Scale.X - shape.Transform.M11);
        
        var height = shape.Scale.Y + 2 * actorScaleX;
        var shapeSizeAsBox = new Vector3(shape.Scale.X, shape.Scale.X, height / 2f);
        
        OrientedBoundingBox capsuleObb = new(-shapeSizeAsBox, shapeSizeAsBox);
        capsuleObb.Transform(shape.Transform);
        var capsuleAabb = capsuleObb.GetBoundingBox();

        return selectionBoxAabb.Contains(capsuleAabb) == ContainmentType.Disjoint &&
               selectionBoxObb.Contains(ref capsuleObb) != ContainmentType.Disjoint;
    }
    
    /// <summary>
    /// Checks if a collision sphere actor overlaps with the obb
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="selectionBoxObb"></param>
    /// <returns></returns>
    public static bool IsCollisionSphereInsideSelectionBox(AbbrActorShapes shape, OrientedBoundingBox selectionBoxObb)
    {
        var transformedSpherePosition = Vector4.Transform(Vector3.Transform(new Vector3(0,0,0), shape.Transform), 
                                                         Matrix.Invert(selectionBoxObb.Transformation));
        
        var collisionSphere = new BoundingSphere(transformedSpherePosition.ToVector3(), shape.Scale.X);
        
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