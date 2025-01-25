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
    public static bool isMeshInsideBox(AbbrMesh mesh, OrientedBoundingBox selectionBoxOBB, BoundingBox selectionBoxAabb, List<AbbrSectorTransform> transforms)
    {
        foreach (var submesh in mesh.SubMeshes)
        {
            foreach (var transform in transforms)
            {
                Matrix meshRotationMatrix = Matrix.RotationQuaternion(transform.Rotation);
                submesh.BoundingBox.Scale(transform.Scale);
                submesh.BoundingBox.Transform(meshRotationMatrix);
                submesh.BoundingBox.Translate(transform.Position);

   
                BoundingBox newSubmeshBoundingBox = submesh.BoundingBox.GetBoundingBox();
                
                ContainmentType contained = selectionBoxAabb.Contains(newSubmeshBoundingBox);
                if (contained != ContainmentType.Disjoint)
                {
                    foreach (Vector3 vertex in submesh.Vertices)
                    {
                        Vector3 scaledVector = vertex * transform.Scale;
                        Vector4 rotatedVector = Vector3.Transform(scaledVector, meshRotationMatrix);
                        Vector3 translatedVector = new Vector3(rotatedVector.X, rotatedVector.Y, rotatedVector.Z) + transform.Position;

                        
                        ContainmentType vertexContained = selectionBoxOBB.Contains(translatedVector);
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
}