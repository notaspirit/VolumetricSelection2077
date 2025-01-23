using System;
using SharpDX;
using VolumetricSelection2077.Models;
using System.Collections.Generic;
using System.Text.Json;
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
                submesh.BoundingBox.Transform(meshRotationMatrix);
                submesh.BoundingBox.Translate(transform.Position);
                submesh.BoundingBox.Scale(transform.Scale);

                BoundingBox newSubmeshBoundingBox = submesh.BoundingBox.GetBoundingBox();

                ContainmentType contained = selectionBoxAabb.Contains(newSubmeshBoundingBox);
                //Logger.Info($"AABB (Center: {newSubmeshBoundingBox.Center.X} {newSubmeshBoundingBox.Center.Y} {newSubmeshBoundingBox.Center.Z}) is inside {contained} selection.");
                if (contained != ContainmentType.Disjoint)
                {
                    return true;
                }
            }
        }
        return false;
    }
}