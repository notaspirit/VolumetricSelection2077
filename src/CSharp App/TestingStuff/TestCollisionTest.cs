using System.Collections.Generic;
using SharpDX;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Parsers;
using VolumetricSelection2077.Services;
using WolvenKit.RED4.Types;
using Quaternion = SharpDX.Quaternion;
using Vector3 = SharpDX.Vector3;

namespace VolumetricSelection2077.TestingStuff;

public class TestCollisionTest
{
    public static BoundingBox buildBB(Vector3 size)
    {
        Vector3 halfExtend = size / 2f; 
        return new BoundingBox(-halfExtend, halfExtend);
    }
    
    public static void RunMeshTest(GameFileService gfs)
    {
        // Vector3 testTranslate = new(10f, -0.24f, -84.2145f);
        Vector3 testTranslate = new(0, 0, 0);
        string meshPath = @"ep1\environment\architecture\pacifica\combat_zone\stadium\bar\bar_a_fridge_a.mesh";
        var (glbSuccess, glbError, glbOut) = gfs.GetGameFileAsGlb(meshPath);
        if (!glbSuccess || glbError != "" || glbOut == null)
        {
            Logger.Warning("Failed to get glb " + glbError);
            return;
        }
        AbbrMesh? mesh = AbbrMeshParser.ParseFromGlb(glbOut);

        List<KeyValuePair<BoundingBox, bool>> bbList = new();
        bbList.Add(new(buildBB(new Vector3(5.19f, 5.19f, 5.19f)), false));
        bbList.Add(new(buildBB(new Vector3(5.703f, 7.787f, 1)), false));
        bbList.Add(new(buildBB(new Vector3(8.99335f, 8.64937f, 3.02371f)), false));
        bbList.Add(new(buildBB(new Vector3(10.9324f, 10.9324f, 10.9324f)), false));
        bbList.Add(new(buildBB(new Vector3(5.74776f, 1, 1)), true));
        bbList.Add(new(buildBB(new Vector3(1.86691f, 1.86691f, 4.07539f)), false));
        bbList.Add(new(buildBB(new Vector3(3.89276f, 24.3775f, 3.89276f)), true));
        bbList.Add(new(buildBB(new Vector3(12.687f, 12.687f, 12.687f)), false));
        bbList.Add(new(buildBB(new Vector3(54.9711f, 54.9202f, 28.2249f) ), false));
        bbList.Add(new(buildBB(new Vector3(12.2468f, 12.2468f, 12.2468f)), false));
        
        List<AbbrSectorTransform> transforms = new();
        transforms.Add(new()
        {
            Position = testTranslate, // + new Vector3(-9.79527f, -5.79494f, 0f),
            Rotation = new Quaternion(0.144247f, -0.067669f, 0.207461f, 0.965181f),
            Scale = new Vector3(1, 1, 1)
        });

        for (int i = 0; i < bbList.Count; i++)
        {
            OrientedBoundingBox obb = new OrientedBoundingBox(bbList[i].Key);
            
            switch (i)
            {
                case 0:
                    obb.Transform(Matrix.RotationQuaternion(new Quaternion(-0.10529f, 0.663105f, 0.188025f, 0.716834f)));
                    obb.Translate(new Vector3(0, -9.88927f, 7.93876f) + testTranslate);
                    break;
                case 1:
                    obb.Transform(Matrix.RotationQuaternion(new Quaternion(0.720243f, 0.118428f, -0.126998f, 0.671637f)));
                    obb.Translate(new Vector3(10.1838f, 10.3861f, -0.018033f) + testTranslate);
                    break;
                case 2:
                    obb.Transform(Matrix.RotationQuaternion(new Quaternion(-0.967725f, -0.320061f, -0.094806f, 0.903823f)));
                    obb.Translate(new Vector3(9.08527f, 11.346f, 5.82674f) + testTranslate);
                    break;
                case 3:
                    obb.Transform(Matrix.RotationQuaternion(new Quaternion(0.044571f, 0.490569f, 0.385128f, 0.780405f)));
                    obb.Translate(new Vector3(-16.7235f, 0, 0) + testTranslate);
                    break;
                case 4:
                    obb.Transform(Matrix.RotationQuaternion(new Quaternion(0.38688f, 0.617763f, -0.205679f, 0.652985f)));
                    obb.Translate(new Vector3(7.64558f, 10.8131f, 1.62222f) + testTranslate);
                    break;
                case 5:
                    obb.Transform(Matrix.RotationQuaternion(new Quaternion(-0.120826f, 0.461408f, 0.525472f, 0.704544f)));
                    obb.Translate(new Vector3(10.7074f, 12.2909f, 1.21902f) + testTranslate);
                    break;
                case 6:
                    obb.Transform(Matrix.RotationQuaternion(new Quaternion(-0.031916f, 0.263099f, -0.392559f, 0.880714f)));
                    obb.Translate(new Vector3(9.43542f, 14.9061f, 4.45551f) + testTranslate);
                    break;
                case 7:
                    obb.Transform(Matrix.RotationQuaternion(new Quaternion(0.281054f, 0.376155f, 0.059402f, 0.880902f)));
                    obb.Translate(new Vector3(29.8415f, 0, 0) + testTranslate);
                    break;
                case 8:
                    obb.Transform(Matrix.RotationQuaternion(new Quaternion(0.595517f, -0.141961f, -0.786476f, 0.081622f)));
                    obb.Translate(new Vector3(0, -37.9764f, -26.4391f) + testTranslate);
                    break;
                case 9:
                    obb.Transform(Matrix.RotationQuaternion(new Quaternion(-0.039326f, 0.503681f, 0.300183f, 0.809104f)));
                    obb.Translate(new Vector3(0, 0, 0) + testTranslate);
                    break;
            }
            
            
            bool checkResult = CollisionCheckService.IsMeshInsideBox(mesh, obb, obb.GetBoundingBox(), transforms);
            if (checkResult != bbList[i].Value)
            {
                Logger.Warning($"Got Unexpected Result {checkResult} for index {i}!");
            }
            else
            {
                Logger.Success($"Got expected Result {checkResult} for index {i}!");
            }
        }
    }
}