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
    
    public static bool isMeshInsideBox(AbbrMesh mesh, OrientedBoundingBox selectionBoxOBB, BoundingBox selectionBoxAabb, List<AbbrSectorTransform> transforms)
    {
        foreach (var submesh in mesh.SubMeshes)
        {
            foreach (var transform in transforms)
            {
                Quaternion normalizedQuaternion = transform.Rotation;
                normalizedQuaternion.Normalize();
                Matrix meshRotationMatrix = Matrix.RotationQuaternion(normalizedQuaternion);
                submesh.BoundingBox.Scale(transform.Scale);
                submesh.BoundingBox.Transform(meshRotationMatrix);
                submesh.BoundingBox.Translate(transform.Position);

   
                BoundingBox newSubmeshBoundingBox = submesh.BoundingBox.GetBoundingBox();
                
                ContainmentType contained = selectionBoxAabb.Contains(newSubmeshBoundingBox);
                if (contained != ContainmentType.Disjoint)
                {
                    Matrix scaleMatrix = Matrix.Scaling(transform.Scale);
                    Matrix rotationMatrix = Matrix.RotationQuaternion(normalizedQuaternion);
                    Matrix translationMatrix = Matrix.Translation(transform.Position);
                    Matrix worldMatrix = scaleMatrix * rotationMatrix * translationMatrix;
                    
                    Logger.Info($"Build Matrix with Scale: {transform.Scale}\n Rotation: {normalizedQuaternion}\n Translation: {transform.Position} \n WorldMatrix: {worldMatrix}");
                    
                    foreach (Vector3 vertex in submesh.Vertices)
                    {
                        /*
                        Vector3 scaledVector = vertex * transform.Scale;
                        Vector3 rotatedVector = Vec4toVec3(Vector3.Transform(scaledVector, meshRotationMatrix));
                        Vector3 translatedVector = rotatedVector + transform.Position;
                        */

                        Vector4 scaledVector = Vector4.Transform(new Vector4(vertex, 1.0f), scaleMatrix);
                        // Logger.Info($"Scaled Vector: {scaledVector}");
                        Vector4 rotatedVector = Vector4.Transform(scaledVector, rotationMatrix);
                        // Logger.Info($"Rotated Vector: {rotatedVector}");
                        Vector4 translatedVectorTest = Vector4.Transform(rotatedVector, translationMatrix);
                        // Logger.Info($"Translated Vector: {translatedVectorTest}");
                        
                        
                        
                        // Vector4 translatedVector = Vector4.Transform(new Vector4(vertex, 1.0f), worldMatrix);
                        // Logger.Info($"Transformed Vertex: {vertex} with\n Scale: {transform.Scale}\n Rotation: {normalizedQuaternion}\n Position: {transform.Position}\n Translation: {translatedVector}");
                        ContainmentType vertexContained = selectionBoxOBB.Contains(Vec4toVec3(translatedVectorTest));
                        if (vertexContained != ContainmentType.Disjoint)
                        {
                            // Logger.Info("Vertex is inside selection box");
                            return true;
                        }
                        // Logger.Info("Vertex is not inside selection box");
                    }
                }
            }
        }
        return false;
    }
}