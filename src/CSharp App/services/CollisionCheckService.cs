using System;
using BulletSharp;
using VolumetricSelection2077.Models;
using BulletSharp.Math;
using System.Collections.Generic;

namespace VolumetricSelection2077.Services;

public class CollisionCheckService
{
    private static Aabb BuildAABB(List<Vector3> points)
    {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (Vector3 point in points)
        {
            min.X = Math.Min(point.X, min.X);
            min.Y = Math.Min(point.Y, min.Y);
            min.Z = Math.Min(point.Z, min.Z);
            
            max.X = Math.Max(point.X, max.X);
            max.Y = Math.Max(point.Y, max.Y);
            max.Z = Math.Max(point.Z, max.Z);
        }

        return new Aabb()
        {
            Min = min,
            Max = max,
        };
    }

    public static BvhTriangleMeshShape CreateTriMeshShape(List<Vector3> vertices, IList<uint> indices)
    {
        var triangleMesh = new TriangleMesh();

        for (int i = 0; i < indices.Count; i += 3)
        {
            triangleMesh.AddTriangle(
                vertices[(int)indices[i]],
                vertices[(int)indices[i + 1]],
                vertices[(int)indices[i + 2]],
                true
                );
        }

        return new BvhTriangleMeshShape(triangleMesh, true);
    }
    
    public static bool checkMesh(AbbrMesh mesh, SelectionBox selectionBox, Quaternion rotation, Vector3 position, Vector3 scale)
    {
        float width = selectionBox.Max.X - selectionBox.Min.X;
        float height = selectionBox.Max.Y - selectionBox.Min.Y;
        float depth = selectionBox.Max.Z - selectionBox.Min.Z;
        
        Vector3 halfExtents = new Vector3(
            width * 0.5f, height * 0.5f, depth * 0.5f
            );
        var selectionBoxShape = new BoxShape(halfExtents);
        
        foreach (var submesh in mesh.SubMeshes)
        {
            if (submesh.IsConvexCollider ?? false)
            {
                continue;
            }
            
            var meshShape = CreateTriMeshShape(submesh.Vertices, submesh.Indices);
            
            var meshGhost = new GhostObject();
            var boxGhost = new GhostObject();
            
            meshGhost.CollisionShape = meshShape;
            boxGhost.CollisionShape = selectionBoxShape;
            
            Matrix meshMatrix = Matrix.Scaling(scale) *
                                Matrix.RotationQuaternion(rotation) *
                                Matrix.Translation(position);
            
            Matrix selectionBoxMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                        Matrix.RotationQuaternion(selectionBox.Rotation) *
                                        Matrix.Translation(selectionBox.Origin);
            
            meshGhost.WorldTransform = meshMatrix;
            boxGhost.WorldTransform = selectionBoxMatrix;
            
            bool doCollide = boxGhost.CheckCollideWith(meshGhost);
            if (doCollide)
            {
                return true;
            }
        }
        return false;
    }
}