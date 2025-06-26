using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using NAudio.CoreAudioApi;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Models.WorldBuilder.Favorites;
using VolumetricSelection2077.Models.WorldBuilder.Spawn;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Mesh;
using VolumetricSelection2077.models.WorldBuilder.Spawn.Visual;
using VolumetricSelection2077.models.WorldBuilder.Structs;
using XXHash3NET;

namespace VolumetricSelection2077.Services;

public static class WorldBuilderMergingService
{
    public static Favorite Merge(Favorite favoriteA, Favorite favoriteB)
    {
        var mergedRaw = new List<WorldBuilderMergingStruct>();
        
        HashFavorite(ref mergedRaw, favoriteA);
        HashFavorite(ref mergedRaw, favoriteB);
        
        var uniqueElements = mergedRaw.Distinct();
        
        var result = new PositionableGroup
        {
            Name = favoriteA.Name,
            Children = new()
        };

        foreach (var element in uniqueElements)
        {
            if (result.Children.All(x => x is PositionableGroup pg && pg.Name != element.ParentName))
                result.Children.Add(new PositionableGroup
                {
                    Name = element.ParentName,
                    Children = new()
                });
            var parent = result.Children.OfType<PositionableGroup>().First(x => x.Name == element.ParentName);
            parent.Children.Add(element.SpawnableElement);
        }
        
        return new Favorite
        {
            Name = favoriteA.Name,
            Data = result,
        };
    }

    private static void HashFavorite(ref List<WorldBuilderMergingStruct> results, Favorite favorite)
    {
        foreach (var element in favorite.Data.Children)
        {
            if (element is not PositionableGroup pg) continue;
            HashElements(ref results, pg);
        }
    }
    
    private static void HashElements(ref List<WorldBuilderMergingStruct> results, PositionableGroup group)
    {
        foreach (var element in group.Children)
        {
            if (element is not SpawnableElement sp) continue;
            results.Add(new()
            {
                Hash = HashElement(sp),
                SpawnableElement = sp,
                ParentName = group.Name
            });
        }
    }
    
    private static ulong HashElement(SpawnableElement element)
    {
        var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);

        WriteSpawnableElement(element, bw);
        
        return XXHash64.Compute(ms.GetBuffer());
    }
    
    private static void WriteSpawnableElement(SpawnableElement element, BinaryWriter bw)
    {
        bw.Write(element.Name);
        bw.Write(element.ModulePath);
        WriteVector4(element.Position, bw);
        WriteSpawnable(element.Spawnable, bw);
    }

        private static void WriteSpawnable(Spawnable spawnable, BinaryWriter bw)
    {
        WriteSpawnableBase(spawnable, bw);
        switch (spawnable)
        {
            case ClothMesh clothMesh:
                WriteMesh(clothMesh, bw);
                WriteClothMesh(clothMesh, bw);
                break;
            case RotatingMesh rotatingMesh:
                WriteMesh(rotatingMesh, bw);
                WriteRotatingMesh(rotatingMesh, bw);
                break;
            case DynamicMesh dynamicMesh:
                WriteMesh(dynamicMesh, bw);
                WriteDynamicMesh(dynamicMesh, bw);
                break;
            case WaterPatch waterPatch:
                WriteMesh(waterPatch, bw);
                WriteWaterPatch(waterPatch, bw);
                break;
            case Mesh mesh:
                WriteMesh(mesh, bw);
                break;
            case Decal decal:
                WriteDecal(decal, bw);
                break;
            case Effect effect:
                break;
            case Particle particle:
                WriteParticle(particle, bw);
                break;
        }
    }

    private static void WriteParticle(Particle particle, BinaryWriter bw)
    {
        bw.Write(particle.EmissionRate);
    }
    
    private static void WriteDecal(Decal decal, BinaryWriter bw)
    {
        bw.Write(decal.Alpha);
        bw.Write(decal.HorizontalFlip);
        bw.Write(decal.VerticalFlip);
        bw.Write(decal.AutoHideDistance);
        WriteVector3(decal.Scale, bw);
    }
    
    private static void WriteWaterPatch(WaterPatch waterPatch, BinaryWriter bw)
    {
        bw.Write(waterPatch.Depth);
    }
    private static void WriteDynamicMesh(DynamicMesh dynamicMesh, BinaryWriter bw)
    {
        bw.Write(dynamicMesh.StartAsleep);
    }
    
    private static void WriteMesh(Mesh mesh, BinaryWriter bw)
    {
        WriteVector3(mesh.Scale, bw);
        bw.Write(mesh.WindImpulseEnabled);
        bw.Write(mesh.CastLocalShadows.ToString());
        bw.Write(mesh.CastRayTracedGlobalShadows.ToString());
        bw.Write(mesh.CastRayTracedLocalShadows.ToString());
        bw.Write(mesh.CastShadows.ToString());
    }
    
    private static void WriteRotatingMesh(RotatingMesh rotatingMesh, BinaryWriter bw)
    {
        bw.Write(rotatingMesh.Duration);
        bw.Write(rotatingMesh.Axis.ToString());
        bw.Write(rotatingMesh.Reverse);
    }
    
    private static void WriteClothMesh(ClothMesh clothMesh, BinaryWriter bw)
    {
        bw.Write(clothMesh.AffectedByWind);
    }
    
    private static void WriteSpawnableBase(Spawnable spawnable, BinaryWriter bw)
    {
        bw.Write(spawnable.ModulePath);
        bw.Write(spawnable.DataType);
        bw.Write(spawnable.NodeType);
        bw.Write(spawnable.ResourcePath);
        bw.Write(spawnable.Appearance);
        WriteVector4(spawnable.Position, bw);
        WriteEuler(spawnable.EulerRotation, bw);
        bw.Write(spawnable.PrimaryRange);
        bw.Write(spawnable.SecondaryRange);
        bw.Write(spawnable.Uk10);
        bw.Write(spawnable.Uk11);
    }
    
    private static void WriteEuler(EulerAngles ea, BinaryWriter bw)
    {
        bw.Write(ea.pitch);
        bw.Write(ea.yaw);
        bw.Write(ea.roll);
    }
    
    private static void WriteVector4(Vector4 v, BinaryWriter bw)
    {
        bw.Write(v.w);
        bw.Write(v.x);
        bw.Write(v.y);
        bw.Write(v.z);
    }
    
    private static void WriteVector3(Vector3 v, BinaryWriter bw)
    {
        bw.Write(v.x);
        bw.Write(v.y);
        bw.Write(v.z);
    }
}