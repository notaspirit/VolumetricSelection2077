using System;
using SharpDX;
using VolumetricSelection2077.Models;
using System.Collections.Generic;
using System.Text.Json;
using SharpGLTF.IO;

namespace VolumetricSelection2077.Services;

public class CollisionCheckService
{
    public static bool isMeshInsideBox(AbbrMesh mesh, OrientedBoundingBox selectionBoxOBB, BoundingBox selectionBoxAabb, Vector3 meshPosition, Vector3 meshScale, Quaternion meshRotation)
    {
        //Logger.Info("OBB" + JsonSerializer.Serialize(selectionBoxOBB.Center.X) + " " + JsonSerializer.Serialize(selectionBoxOBB.Center.Y) + " " + JsonSerializer.Serialize(selectionBoxOBB.Center.Z));
        //Logger.Info("AABB" + JsonSerializer.Serialize(newSelectionBox.Center.X) + " " + JsonSerializer.Serialize(newSelectionBox.Center.Y) + " " + JsonSerializer.Serialize(newSelectionBox.Center.Z));
        
        foreach (var submesh in mesh.SubMeshes)
        {
            Matrix meshRotationMatrix = Matrix.RotationQuaternion(meshRotation);
            submesh.BoundingBox.Translate(meshPosition);
            submesh.BoundingBox.Scale(meshScale);
            submesh.BoundingBox.Transform(meshRotationMatrix);
            
            BoundingBox newSubmeshBoundingBox = submesh.BoundingBox.GetBoundingBox();
            
            ContainmentType contained = selectionBoxAabb.Contains(newSubmeshBoundingBox);
            //Logger.Info($"AABB (Center: {newSubmeshBoundingBox.Center.X} {newSubmeshBoundingBox.Center.Y} {newSubmeshBoundingBox.Center.Z}) is inside {contained} selection.");
            if (contained != ContainmentType.Disjoint)
            {
                return true;
            }
        }
        return false;
    }
}