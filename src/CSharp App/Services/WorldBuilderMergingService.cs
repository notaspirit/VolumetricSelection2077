using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using VolumetricSelection2077.Json.Helpers;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Models.WorldBuilder.Favorites;
using VolumetricSelection2077.Models.WorldBuilder.Spawn;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Entity;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Light;
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
        
        HashFavorite(ref mergedRaw, ReJsonSerialize(favoriteA));
        HashFavorite(ref mergedRaw, ReJsonSerialize(favoriteB));
        
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
            Data = result
        };
    }

    public static Favorite Subtract(Favorite baseFavorite, Favorite subtractionFavorite)
    {
        var baseElements = new List<WorldBuilderMergingStruct>();
        var subtractionElements = new List<WorldBuilderMergingStruct>();
        
        HashFavorite(ref baseElements, ReJsonSerialize(baseFavorite));
        HashFavorite(ref subtractionElements, ReJsonSerialize(subtractionFavorite));
        
        var uniqueElements = baseElements.Except(subtractionElements).ToList();
        
        var result = new PositionableGroup
        {
            Name = baseFavorite.Name,
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
            Name = baseFavorite.Name,
            Data = result
        };
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="favorite"></param>
    /// <returns></returns>
    /// <remarks>Reserialization is necessary to avoid quirks like -0.0 being turned into 0.0 only in one set despite being different on the bit level</remarks>
    private static Favorite ReJsonSerialize(Favorite favorite)
    {
        return JsonConvert.DeserializeObject<Favorite>(JsonConvert.SerializeObject(favorite, JsonSerializerPresets.WorldBuilder), JsonSerializerPresets.WorldBuilder)!;
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
            case Effect:
                break;
            case Particle particle:
                WriteParticle(particle, bw);
                break;
            case Light light:
                WriteLight(light, bw);
                break;
            case Entity entity:
                WriteEntity(entity, bw);
                break;
        }
    }

    private static void WriteLight(Light light, BinaryWriter bw)
    {
        WriteColor(light.Color, bw);
        bw.Write(light.Intensity);
        bw.Write(light.InnerAngle);
        bw.Write(light.OuterAngle);
        bw.Write(light.Radius);
        bw.Write(light.CapsuleLength);
        bw.Write(light.AutoHideDistance);
        bw.Write(light.FlickerStrength);
        bw.Write(light.FlickerPeriod);
        bw.Write(light.FlickerOffset);
        bw.Write(light.LightType.ToString());
        bw.Write(light.LocalShadows);
        bw.Write(light.Temperature);
        bw.Write(light.ScaleVolFog);
        bw.Write(light.UseInParticles);
        bw.Write(light.UseInTransparents);
        bw.Write(light.EV);
        bw.Write(light.ShadowFadeDistance);
        bw.Write(light.ShadowFadeRange);
        bw.Write(light.ContactShadows.ToString());
        bw.Write(light.SpotCapsule);
        bw.Write(light.Softness);
        bw.Write(light.Attenuation.ToString());
        bw.Write(light.ClampAttenuation);
        bw.Write(light.SceneSpecularScale);
        bw.Write(light.SceneDiffuse);
        bw.Write(light.RoughnessBias);
        bw.Write(light.SourceRadius);
        bw.Write(light.SourceRadius);
        bw.Write(light.Directional);
    }
        
    private static void WriteEntity(Entity entity, BinaryWriter bw)
    {
        foreach (var app in entity.Appearances)
            bw.Write(app);
        bw.Write(entity.AppearanceIndex);
        bw.Write(JsonConvert.SerializeObject(entity.InstanceDataChanges));
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

    private static void WriteColor(float[] color, BinaryWriter bw)
    {
        bw.Write(color[0]);
        bw.Write(color[1]);
        bw.Write(color[2]);
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