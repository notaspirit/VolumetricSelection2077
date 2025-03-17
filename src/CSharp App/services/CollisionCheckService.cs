using System;
using SharpDX;
using VolumetricSelection2077.Models;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using HelixToolkit.Wpf.SharpDX;
using SharpGLTF.IO;
using WolvenKit.Common.PhysX;
using Geometry3D = HelixToolkit.SharpDX.Core.Geometry3D;
using Polygon = VolumetricSelection2077.Models.Polygon;

namespace VolumetricSelection2077.Services;

public static class CollisionCheckService
{
    private const float Epsilon = 1e-5f;

    private static Vector3[] GetObbAxes(OrientedBoundingBox obb)
    {
        var x = new Vector3(obb.Transformation.M11, obb.Transformation.M12, obb.Transformation.M13);
        var y = new Vector3(obb.Transformation.M21, obb.Transformation.M22, obb.Transformation.M23);
        var z = new Vector3(obb.Transformation.M31, obb.Transformation.M32, obb.Transformation.M33);
        x.Normalize();
        y.Normalize();
        z.Normalize();
        
        return new[]
        {
            x, y, z
        };
    }
    /*
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
        
        Vector3[] obbAxes = GetObbAxes(obb);

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
        /*
        Vector3[] axes = new Vector3[13];

        axes[0] = triangleNormal;
        axes[1] = obbAxes[0];
        axes[2] = obbAxes[1];
        axes[3] = obbAxes[2];
        
        int axisIndex = 4;

        
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
    /*
            if (triMax < boxMin - Epsilon || triMin > boxMax + Epsilon)
            {
                // Logger.Info("Returned False");
                return false; 
            }
        }
        // Logger.Info("Returned True");
        return true; 
    }

    private static bool CheckIntersectionBoxConvex(ConvexSubMesh convexSubMesh, OrientedBoundingBox obb)
    {
        var obbCorners = obb.GetCorners();
        var obbAxes = GetObbAxes(obb);
        
        List<Vector3> axesToTest = new List<Vector3>();
        axesToTest.AddRange(obbAxes);

        foreach (var polygon in convexSubMesh.Planes)
        {
            var normal = polygon.Normal;
            normal.Normalize();
            axesToTest.Add(normal);
        }
        
        AddEdgeCrossProductAxes(convexSubMesh.Vertices, obbAxes, axesToTest);
        
        foreach (Vector3 axis in axesToTest)
        {
            // Skip degenerate axes
            if (axis.LengthSquared() < 0.001f)
                continue;
                
            // Project both shapes onto the axis
            ProjectionInterval obbInterval = ProjectShapeOntoAxis(obbCorners, axis);
            ProjectionInterval meshInterval = ProjectShapeOntoAxis(convexSubMesh.Vertices, axis);
            
            // If we find a separating axis, there's no collision
            if (!obbInterval.Overlaps(meshInterval))
            {
                return false;
            }
        }
        
        // No separating axis found, shapes must be intersecting
        return true;
    }
*/
    private static void AddEdgeCrossProductAxes(
        Vector3[] meshVertices, 
        Vector3[] obbAxes, 
        List<Vector3> axesToTest)
    {
        // For a true implementation, we would extract the edges of the convex mesh
        // and perform cross products with the OBB axes
        // This is an approximation using adjacent vertices
        for (int i = 0; i < meshVertices.Length; i++)
        {
            for (int j = i + 1; j < meshVertices.Length; j++)
            {
                Vector3 edge = meshVertices[j] - meshVertices[i];
                if (edge.LengthSquared() < 0.001f)
                    continue;
                
                edge.Normalize();
                
                foreach (Vector3 obbAxis in obbAxes)
                {
                    Vector3 crossAxis = Vector3.Cross(edge, obbAxis);
                    if (crossAxis.LengthSquared() > 0.001f)
                    {
                        crossAxis.Normalize();
                        
                        bool isDuplicate = axesToTest.Any(a => 
                            Math.Abs(Vector3.Dot(a, crossAxis)) > 0.9999f);
                            
                        if (!isDuplicate)
                        {
                            axesToTest.Add(crossAxis);
                        }
                    }
                }
            }
        }
    }

    public static bool CheckOverlapPolygonBox(Polygon polygon, OrientedBoundingBox obb)
    {
        var obbAxes = GetObbAxes(obb);
        var obbCorners = obb.GetCorners();
        List<Vector3> axes = new List<Vector3>();
        axes.AddRange(obbAxes);
        axes.Add(polygon.Plane.Normal);
        
        
        // AddEdgeCrossProductAxes(verts, obbAxes, axes);
        
        
        for (int i = 0; i < polygon.Vertices.Length; i++)
        {
            Vector3 edge = polygon.Vertices[(i + 1) % polygon.Vertices.Length] - polygon.Vertices[i];
            foreach (var obbAxis in obbAxes)
            {
                Vector3 crossAxis = Vector3.Cross(edge, obbAxis);
                if (crossAxis.LengthSquared() > 1e-6f) // Avoid near-zero vectors
                    axes.Add(Vector3.Normalize(crossAxis));
            }
        }
        
        foreach (Vector3 axis in axes)
        {
            if (axis.LengthSquared() < 0.001f)
                continue;
            ProjectionInterval obbInterval = ProjectShapeOntoAxis(obbCorners, axis);
            ProjectionInterval meshInterval = ProjectShapeOntoAxis(polygon.Vertices, axis);
            
            if (!obbInterval.Overlaps(meshInterval))
            {
                return false;
            }
        }
        return true;
    }
    
    public static bool CheckOverlapSubMeshBox(AbbrSubMesh submesh, OrientedBoundingBox obb)
    {
        // var obbAxes = GetObbAxes(obb);
        foreach (var poly in submesh.Polygons)
        {
            if (CheckOverlapPolygonBox(poly, obb))
                return true;
            
            /*
            List<Vector3> axes = new List<Vector3>();
            axes.AddRange(obbAxes);
            axes.Add(poly.Plane.Normal);

            var verts = poly.Indices.Select(index => submesh.Vertices[index]).ToArray();
            
            AddEdgeCrossProductAxes(verts, obbAxes, axes);
            
            /*
            for (int i = 0; i < poly.Indices.Length; i++)
            {
                uint currentIndex = poly.Indices[i];
                uint nextIndex = poly.Indices[(i + 1) % poly.Indices.Length];

                Vector3 edge = submesh.Vertices[nextIndex] - submesh.Vertices[currentIndex];

                foreach (var obbAxis in obbAxes)
                {
                    Vector3 crossAxis = Vector3.Cross(edge, obbAxis);
                    if (crossAxis.LengthSquared() > 1e-6f)
                        axes.Add(Vector3.Normalize(crossAxis));
                }
            }
            */
            /*
            foreach (Vector3 axis in axes)
            {
                if (axis.LengthSquared() < 0.001f)
                    continue;
                ProjectionInterval obbInterval = ProjectShapeOntoAxis(obb.GetCorners(), axis);
                ProjectionInterval meshInterval = ProjectShapeOntoAxis(verts, axis);
                
                if (!obbInterval.Overlaps(meshInterval))
                {
                    return false;
                }
            }
            */
        }
        return false;
    }
    
    private struct ProjectionInterval
    {
        public float Min { get; }
        public float Max { get; }
        
        public ProjectionInterval(float min, float max)
        {
            Min = min;
            Max = max;
        }
        
        public bool Overlaps(ProjectionInterval other)
        {
            return Max >= other.Min && Min <= other.Max;
        }
    }
    
    private static ProjectionInterval ProjectShapeOntoAxis(Vector3[] vertices, Vector3 axis)
    {
        float min = float.MaxValue;
        float max = float.MinValue;
        
        foreach (Vector3 vertex in vertices)
        {
            float projection = Vector3.Dot(vertex, axis);
            min = Math.Min(min, projection);
            max = Math.Max(max, projection);
        }
        
        return new ProjectionInterval(min, max);
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
        static bool IsInsidePrivate(AbbrSubMesh submesh, OrientedBoundingBox selectionObb, BoundingBox selectionAabb, Matrix transform)
        {
            OrientedBoundingBox baseObb = new(submesh.BoundingBox);
            baseObb.Transform(transform);
            BoundingBox transformedAabb = baseObb.GetBoundingBox();
            
            ContainmentType aabbContainment = selectionAabb.Contains(transformedAabb);
            if (aabbContainment != ContainmentType.Disjoint)
            {
                var adjustedSubmesh = submesh;
                for (int i = 0; i < adjustedSubmesh.Polygons.Length; i++)
                {
                    var adjustedPolygon = adjustedSubmesh.Polygons[i];
                    for (int j = 0; j < adjustedPolygon.Vertices.Length; j++)
                    {
                        Vector4 translatedVertex = Vector4.Transform(new(adjustedPolygon.Vertices[j], 1f), transform);
                        adjustedPolygon.Vertices[j] = Vec4toVec3(translatedVertex);
                    }
                    adjustedSubmesh.Polygons[i] = adjustedPolygon;
                }
                if (CheckOverlapSubMeshBox(adjustedSubmesh, selectionObb))
                    return true;
                /*
                switch (submesh)
                {
                    case ConvexSubMesh convexSubMesh:
                        var adjSubMesh = convexSubMesh;
                        for (int j = 0; j < adjSubMesh.Vertices.Length; j++)
                        {
                            Vector4 translatedVertex = Vector4.Transform(new(adjSubMesh.Vertices[j], 1f), transform);
                            adjSubMesh.Vertices[j] = Vec4toVec3(translatedVertex);
                        }
                        if (CheckIntersectionBoxConvex(adjSubMesh, selectionObb))
                            return true;
                        break;
                    case TriangleSubMesh triangleSubMesh:
                        if (triangleSubMesh.Indices.Length % 3 != 0) throw new Exception("Invalid submesh indices count.");
                        for (int i = 0; i < triangleSubMesh.Indices.Length; i += 3)
                        {
                            var triangle = new Vector3[]
                            {
                                triangleSubMesh.Vertices[triangleSubMesh.Indices[i]],
                                triangleSubMesh.Vertices[triangleSubMesh.Indices[i + 1]],
                                triangleSubMesh.Vertices[triangleSubMesh.Indices[i + 2]]
                            };
                            for (int j = 0; j < triangle.Length; j++)
                            {
                                Vector4 translatedVertex = Vector4.Transform(new(triangle[j], 1f), transform);
                                triangle[j] = Vec4toVec3(translatedVertex);
                            }

                            if (CheckIntersectionBoxTri(triangle, selectionObb))
                                return true;
                        }
                        break;
                    default:
                        throw new Exception("Invalid submesh type.");
                }
                */
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