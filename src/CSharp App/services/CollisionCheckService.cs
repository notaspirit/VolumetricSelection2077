using System;
using SharpDX;
using VolumetricSelection2077.Models;
using System.Collections.Generic;
using System.Text.Json;
using SharpGLTF.IO;

namespace VolumetricSelection2077.Services;

public class CollisionCheckService
{
    public static OrientedBoundingBox BuildAABB(List<Vector3> points)
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

        return new OrientedBoundingBox(min, max);
    }

    public static bool isMeshInsideBox(AbbrMesh mesh, SelectionBox box, Vector3 meshPosition, Vector3 meshScale, Quaternion meshRotation)
    {
        Matrix selectionBoxMatrix = Matrix.RotationYawPitchRoll(box.Rotation.X, box.Rotation.Y, box.Rotation.Z);
        OrientedBoundingBox selectionBoxOBB = new OrientedBoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        selectionBoxOBB.Transform(selectionBoxMatrix);
        selectionBoxOBB.Translate(box.Origin);
        selectionBoxOBB.Scale(box.Scale);
        //Logger.Info("OBB" + JsonSerializer.Serialize(selectionBoxOBB.Center.X) + " " + JsonSerializer.Serialize(selectionBoxOBB.Center.Y) + " " + JsonSerializer.Serialize(selectionBoxOBB.Center.Z));
        BoundingBox newSelectionBox = selectionBoxOBB.GetBoundingBox();
        //Logger.Info("AABB" + JsonSerializer.Serialize(newSelectionBox.Center.X) + " " + JsonSerializer.Serialize(newSelectionBox.Center.Y) + " " + JsonSerializer.Serialize(newSelectionBox.Center.Z));
        
        foreach (var submesh in mesh.SubMeshes)
        {
            Matrix meshRotationMatrix = Matrix.RotationQuaternion(meshRotation);
            submesh.BoundingBox.Translate(meshPosition);
            submesh.BoundingBox.Scale(meshScale);
            submesh.BoundingBox.Transform(meshRotationMatrix);
            
            BoundingBox newSubmeshBoundingBox = submesh.BoundingBox.GetBoundingBox();
            
            ContainmentType contained = newSelectionBox.Contains(newSubmeshBoundingBox);
            //Logger.Info($"AABB (Center: {newSubmeshBoundingBox.Center.X} {newSubmeshBoundingBox.Center.Y} {newSubmeshBoundingBox.Center.Z}) is inside {contained} selection.");
            if (contained != ContainmentType.Disjoint)
            {
                return true;
            }
        }
        return false;
    }
}