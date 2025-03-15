using System;
using SharpDX;
using VolumetricSelection2077.Services;
using System.Collections.Generic;

namespace VolumetricSelection2077.TestingStuff;

public class TestAdvMeshCol
{
    static List<KeyValuePair<OrientedBoundingBox, bool>> CreateBoxesWithStatus()
    {
        var boxesWithStatus = new List<KeyValuePair<OrientedBoundingBox, bool>>();

        // 1. Box at triangle center, should intersect
        {
            Vector3 position = new Vector3(0, 0, 0);
            Vector3 scale = new Vector3(0.5f, 0.5f, 0.5f);
            Quaternion rotation = Quaternion.RotationYawPitchRoll(0, 0, 0);

            Matrix transformMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                     Matrix.RotationQuaternion(rotation) *
                                     Matrix.Translation(position);

            OrientedBoundingBox box = new OrientedBoundingBox(-(scale / 2), (scale / 2));
            box.Transform(transformMatrix);

            boxesWithStatus.Add(new KeyValuePair<OrientedBoundingBox, bool>(box, false));
        }

        // 2. Box outside triangle, should not intersect
        {
            Vector3 position = new Vector3(3, 0, 0);
            Vector3 scale = new Vector3(0.5f, 0.5f, 0.5f);
            Quaternion rotation = Quaternion.RotationYawPitchRoll(0, 0, 0);

            Matrix transformMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                     Matrix.RotationQuaternion(rotation) *
                                     Matrix.Translation(position);

            OrientedBoundingBox box = new OrientedBoundingBox(-(scale / 2), (scale / 2));
            box.Transform(transformMatrix);

            boxesWithStatus.Add(new KeyValuePair<OrientedBoundingBox, bool>(box, false));
        }

        // 3. Box inside triangle, rotated 45°, should intersect
        {
            Vector3 position = new Vector3(1, 0.577f, 0);
            Vector3 scale = new Vector3(0.4f, 0.4f, 0.4f);
            Quaternion rotation = Quaternion.RotationYawPitchRoll(0, 0, MathUtil.PiOverFour);

            Matrix transformMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                     Matrix.RotationQuaternion(rotation) *
                                     Matrix.Translation(position);

            OrientedBoundingBox box = new OrientedBoundingBox(-(scale / 2), (scale / 2));
            box.Transform(transformMatrix);

            boxesWithStatus.Add(new KeyValuePair<OrientedBoundingBox, bool>(box, true));
        }

        // 4. Box below triangle, should not intersect
        {
            Vector3 position = new Vector3(1, -1, 0);
            Vector3 scale = new Vector3(0.3f, 0.3f, 0.3f);
            Quaternion rotation = Quaternion.RotationYawPitchRoll(0, 0, MathUtil.Pi / 6);

            Matrix transformMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                     Matrix.RotationQuaternion(rotation) *
                                     Matrix.Translation(position);

            OrientedBoundingBox box = new OrientedBoundingBox(-(scale / 2), (scale / 2));
            box.Transform(transformMatrix);

            boxesWithStatus.Add(new KeyValuePair<OrientedBoundingBox, bool>(box, false));
        }

        // 5. Box above triangle, should not intersect
        {
            Vector3 position = new Vector3(1, 0.577f, 1);
            Vector3 scale = new Vector3(0.3f, 0.3f, 0.3f);
            Quaternion rotation = Quaternion.RotationYawPitchRoll(0, MathUtil.PiOverFour, 0);

            Matrix transformMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                     Matrix.RotationQuaternion(rotation) *
                                     Matrix.Translation(position);

            OrientedBoundingBox box = new OrientedBoundingBox(-(scale / 2), (scale / 2));
            box.Transform(transformMatrix);

            boxesWithStatus.Add(new KeyValuePair<OrientedBoundingBox, bool>(box, false));
        }

        // 6. Box crossing through triangle along X-axis, should intersect
        {
            Vector3 position = new Vector3(1, 0.577f, 0);
            Vector3 scale = new Vector3(2, 0.1f, 0.1f);
            Quaternion rotation = Quaternion.RotationYawPitchRoll(0, 0, 0);

            Matrix transformMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                     Matrix.RotationQuaternion(rotation) *
                                     Matrix.Translation(position);

            OrientedBoundingBox box = new OrientedBoundingBox(-(scale / 2), (scale / 2));
            box.Transform(transformMatrix);

            boxesWithStatus.Add(new KeyValuePair<OrientedBoundingBox, bool>(box, true));
        }

        // 7. Box crossing through triangle along Y-axis, should intersect
        {
            Vector3 position = new Vector3(1, 0.577f, 0);
            Vector3 scale = new Vector3(0.1f, 2, 0.1f);
            Quaternion rotation = Quaternion.RotationYawPitchRoll(0, 0, 0);

            Matrix transformMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                     Matrix.RotationQuaternion(rotation) *
                                     Matrix.Translation(position);

            OrientedBoundingBox box = new OrientedBoundingBox(-(scale / 2), (scale / 2));
            box.Transform(transformMatrix);

            boxesWithStatus.Add(new KeyValuePair<OrientedBoundingBox, bool>(box, true));
        }

        // 8. Box left of triangle, should not intersect
        {
            Vector3 position = new Vector3(-1, 0.577f, 0);
            Vector3 scale = new Vector3(0.5f, 0.5f, 0.5f);
            Quaternion rotation = Quaternion.RotationYawPitchRoll(0, 0, MathUtil.Pi / 3);

            Matrix transformMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                     Matrix.RotationQuaternion(rotation) *
                                     Matrix.Translation(position);

            OrientedBoundingBox box = new OrientedBoundingBox(-(scale / 2), (scale / 2));
            box.Transform(transformMatrix);

            boxesWithStatus.Add(new KeyValuePair<OrientedBoundingBox, bool>(box, false));
        }

        // 9. Box top right of triangle, should not intersect
        {
            Vector3 position = new Vector3(2, 2, 0);
            Vector3 scale = new Vector3(0.5f, 0.5f, 0.5f);
            Quaternion rotation = Quaternion.RotationYawPitchRoll(0, 0, MathUtil.PiOverTwo);

            Matrix transformMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                     Matrix.RotationQuaternion(rotation) *
                                     Matrix.Translation(position);

            OrientedBoundingBox box = new OrientedBoundingBox(-(scale / 2), (scale / 2));
            box.Transform(transformMatrix);

            boxesWithStatus.Add(new KeyValuePair<OrientedBoundingBox, bool>(box, false));
        }

        // 10. Box inside triangle left side, should intersect
        {
            Vector3 position = new Vector3(0.5f, 0.3f, 0);
            Vector3 scale = new Vector3(0.3f, 0.3f, 0.3f);
            Quaternion rotation = Quaternion.RotationYawPitchRoll(0, 0, MathUtil.Pi / 5);

            Matrix transformMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                     Matrix.RotationQuaternion(rotation) *
                                     Matrix.Translation(position);

            OrientedBoundingBox box = new OrientedBoundingBox(-(scale / 2), (scale / 2));
            box.Transform(transformMatrix);

            boxesWithStatus.Add(new KeyValuePair<OrientedBoundingBox, bool>(box, false));
        }

        // 11. Box inside triangle right side, should intersect
        {
            Vector3 position = new Vector3(1.5f, 0.3f, 0);
            Vector3 scale = new Vector3(0.3f, 0.3f, 0.3f);
            Quaternion rotation = Quaternion.RotationYawPitchRoll(0, 0, MathUtil.Pi / 5);

            Matrix transformMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                     Matrix.RotationQuaternion(rotation) *
                                     Matrix.Translation(position);

            OrientedBoundingBox box = new OrientedBoundingBox(-(scale / 2), (scale / 2));
            box.Transform(transformMatrix);

            boxesWithStatus.Add(new KeyValuePair<OrientedBoundingBox, bool>(box, true));
        }

        // 12. Box inside triangle top, should intersect
        {
            Vector3 position = new Vector3(0.8f, 1.2f, 0);
            Vector3 scale = new Vector3(0.3f, 0.3f, 0.3f);
            Quaternion rotation = Quaternion.RotationYawPitchRoll(0, 0, 0);

            Matrix transformMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                     Matrix.RotationQuaternion(rotation) *
                                     Matrix.Translation(position);

            OrientedBoundingBox box = new OrientedBoundingBox(-(scale / 2), (scale / 2));
            box.Transform(transformMatrix);

            boxesWithStatus.Add(new KeyValuePair<OrientedBoundingBox, bool>(box, true));
        }

        // 13. Box at edge of triangle, should intersect
        {
            Vector3 position = new Vector3(2, 1, 0);
            Vector3 scale = new Vector3(0.3f, 0.3f, 0.3f);
            Quaternion rotation = Quaternion.RotationYawPitchRoll(0, 0, MathUtil.Pi / 3);

            Matrix transformMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                     Matrix.RotationQuaternion(rotation) *
                                     Matrix.Translation(position);

            OrientedBoundingBox box = new OrientedBoundingBox(-(scale / 2), (scale / 2));
            box.Transform(transformMatrix);

            boxesWithStatus.Add(new KeyValuePair<OrientedBoundingBox, bool>(box, false));
        }

        // 14. Box above triangle vertex, should intersect
        {
            Vector3 position = new Vector3(0, 0, 0.5f);
            Vector3 scale = new Vector3(0.3f, 0.3f, 0.3f);
            Quaternion rotation = Quaternion.RotationYawPitchRoll(MathUtil.Pi / 6, MathUtil.Pi / 6, 0);

            Matrix transformMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                     Matrix.RotationQuaternion(rotation) *
                                     Matrix.Translation(position);

            OrientedBoundingBox box = new OrientedBoundingBox(-(scale / 2), (scale / 2));
            box.Transform(transformMatrix);

            boxesWithStatus.Add(new KeyValuePair<OrientedBoundingBox, bool>(box, true));
        }

        // 15. Box far from triangle, should not intersect
        {
            Vector3 position = new Vector3(3, 3, 0);
            Vector3 scale = new Vector3(1, 1, 1);
            Quaternion rotation = Quaternion.RotationYawPitchRoll(0, 0, 0);

            Matrix transformMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                     Matrix.RotationQuaternion(rotation) *
                                     Matrix.Translation(position);

            OrientedBoundingBox box = new OrientedBoundingBox(-(scale / 2), (scale / 2));
            box.Transform(transformMatrix);

            boxesWithStatus.Add(new KeyValuePair<OrientedBoundingBox, bool>(box, false));
        }

        // 16. Flat box above triangle, should intersect
        {
            Vector3 position = new Vector3(1, 0.577f, 0.5f);
            Vector3 scale = new Vector3(1, 1, 0.1f);
            Quaternion rotation = Quaternion.RotationYawPitchRoll(0, 0, 0);

            Matrix transformMatrix = Matrix.Scaling(new Vector3(1,1,1)) *
                                     Matrix.RotationQuaternion(rotation) *
                                     Matrix.Translation(position);

            OrientedBoundingBox box = new OrientedBoundingBox(-(scale / 2), (scale / 2));
            box.Transform(transformMatrix);

            boxesWithStatus.Add(new KeyValuePair<OrientedBoundingBox, bool>(box, false));
        }
        return boxesWithStatus;
    }

    public static void Run()
    {
        Logger.Info("Testing Box vs Triangle Intersection");
        var triangle = new Vector3[] { new Vector3(0, 0, 0.5f), new Vector3(2, 0, 0), new Vector3(1, 1.732f, 0) };
        var dataSetDict = CreateBoxesWithStatus();
        int i = 0;
        foreach (var box in dataSetDict)
        {
            var result = CollisionCheckService.CheckIntersectionBoxTri(triangle, box.Key);
            if (result != box.Value)
                Logger.Error($"Got unexpected result for {i}");
            else
                Logger.Success($"Got expected result for {i}");
            i++;
        }
    }
}