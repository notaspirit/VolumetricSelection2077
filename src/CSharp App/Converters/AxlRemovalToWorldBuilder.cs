using System.Collections.Generic;
using System.Linq;
using DynamicData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Entity;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Light;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Mesh;
using VolumetricSelection2077.models.WorldBuilder.Spawn.Visual;
using VolumetricSelection2077.Services;
using WolvenKit.RED4.Archive.Buffer;
using WolvenKit.RED4.Archive.CR2W;
using WolvenKit.RED4.CR2W.JSON;
using WolvenKit.RED4.Types;
using Activator = System.Activator;
using Vector4 = SharpDX.Vector4;

namespace VolumetricSelection2077.Converters;

public class AxlRemovalToWorldBuilder
{
    private GameFileService _gfs;
    private List<string> _warnedTypes;
    private Dictionary<string, List<string>> _embeddedResourcePaths;
    
    public AxlRemovalToWorldBuilder()
    {
        _gfs = GameFileService.Instance;
        _warnedTypes = new List<string>();
        _embeddedResourcePaths = new Dictionary<string, List<string>>();
    }

    public Element Convert(AxlRemovalFile axlFile, string rootName)
    {
        _warnedTypes.Clear();
        _embeddedResourcePaths.Clear();
        
        var root = new PositionableGroup
        {
            Name = rootName
        };

        foreach (var sector in axlFile.Streaming.Sectors)
        {
            if (sector.NodeDeletions.Count <= 0)
                continue;

            var worldSectorCR2W = _gfs?.ArchiveManager?.GetCR2WFile(sector.Path);
            if (worldSectorCR2W is not { RootChunk: worldStreamingSector wse })
            {
                Logger.Warning($"Failed to get sector {sector.Path}, no nodes from that sector will be added.");
                continue;
            }

            AbbrSector? worldSectorAbbr = null;
            
            foreach (var node in sector.NodeDeletions)
            {
                var convertedNode = ConvertNode(node, worldSectorCR2W, ref worldSectorAbbr, sector.Path);
                if (convertedNode.Count == 0)
                    continue;
                
                if (root.Children.OfType<PositionableGroup>().All(pg => pg.Name != node.Type))
                    root.Children.Add(new PositionableGroup
                    {
                        Name = node.Type
                    });
                var pg = root.Children.OfType<PositionableGroup>().First(pg => pg.Name == node.Type);
                pg.Children.AddRange(convertedNode);
            }
        }


        var embeddedFilesCount = _embeddedResourcePaths.Values.Sum(v => v.Count);
        if (embeddedFilesCount > 0)
        {
            var formattedPaths = "";
            foreach (var (key, value) in _embeddedResourcePaths)
            {
                formattedPaths += $"{key}\n";
                foreach (var path in value)
                {
                    formattedPaths += $"  {path}\n";
                }
                formattedPaths += "\n";
            }
            
            Logger.Warning($"Found {embeddedFilesCount} embedded resource paths. These will need to be manually extracted from their respective sectors and added to the project otherwise they will simply not show up in game.");
            Logger.Warning("The following paths were found:\n" +
                           $"{formattedPaths}");
        }
        
        _warnedTypes.Clear();
        _embeddedResourcePaths.Clear();
        return root;
    }
    private List<Element> ConvertNode(AxlRemovalNodeDeletion remNode, CR2WFile sectorCR2W, ref AbbrSector? worldSectorAbbr, string sectorPath)
    {
        var sector = sectorCR2W.RootChunk as worldStreamingSector;
        
        var nodeData = sector.NodeData.Data as CArray<worldNodeData>;
        
        var nodeDataEntry = nodeData[remNode.Index];
        var node = sector.Nodes[nodeDataEntry.NodeIndex].Chunk;

        var spawnableElements = new List<Element>();
        
        switch (node)
        {
            case worldStaticLightNode lightNode:
                var spawnableLight = new SpawnableElement
                {
                    Name = GetSpawnableName(lightNode),
                    Spawnable = new Light
                    {
                        Color = new float[] { lightNode.Color.Red / 255f, lightNode.Color.Green / 255f, lightNode.Color.Blue / 255f },
                        Intensity = lightNode.Intensity,
                        InnerAngle = lightNode.InnerAngle,
                        OuterAngle = lightNode.OuterAngle,
                        Radius = lightNode.Radius,
                        CapsuleLength = lightNode.CapsuleLength,
                        AutoHideDistance = lightNode.AutoHideDistance,
                        FlickerStrength = lightNode.Flicker.FlickerStrength,
                        FlickerPeriod = lightNode.Flicker.FlickerPeriod,
                        FlickerOffset = lightNode.Flicker.PositionOffset,
                        LightType = lightNode.Type,
                        LocalShadows = lightNode.EnableLocalShadows,
                        Temperature = lightNode.Temperature,
                        ScaleVolFog = lightNode.ScaleVolFog,
                        UseInParticles = lightNode.UseInParticles,
                        UseInTransparents = lightNode.UseInTransparents,
                        EV = lightNode.EV,
                        ShadowFadeDistance = lightNode.ShadowFadeDistance,
                        ShadowFadeRange = lightNode.ShadowFadeRange,
                        ContactShadows = lightNode.ContactShadows,
                        SpotCapsule = lightNode.SpotCapsule,
                        Softness = lightNode.Softness,
                        Attenuation = lightNode.Attenuation,
                        ClampAttenuation = lightNode.ClampAttenuation,
                        SceneSpecularScale = lightNode.SceneSpecularScale,
                        SceneDiffuse = lightNode.SceneDiffuse,
                        RoughnessBias = lightNode.RoughnessBias,
                        SourceRadius = lightNode.SourceRadius,
                        Directional = lightNode.Directional,
                    }
                };
                PopulateSpawnable(ref spawnableLight, nodeDataEntry);
                spawnableElements.Add(spawnableLight);
                break;
            case worldEntityNode entityNode:
                if ((string?)entityNode.EntityTemplate.DepotPath is null)
                    return spawnableElements;

                HandleEmbeddedResourceWarning(entityNode.EntityTemplate.DepotPath, sectorPath, ref sectorCR2W);
                
                var rawEntTemplate = _gfs.ArchiveManager?.GetCR2WFile(entityNode.EntityTemplate.DepotPath);
                if (rawEntTemplate?.RootChunk is not entEntityTemplate entTemplate)
                    return spawnableElements;

                var entAppearances = entTemplate.Appearances.Select(a => (string)a.Name).ToArray();
                var entAppearance = entityNode.AppearanceName == "default"
                    ? entTemplate.DefaultAppearance
                    : entityNode.AppearanceName;

                var spawnableEntity = new SpawnableElement
                {
                    Name = GetSpawnableName(entityNode),
                    Spawnable = new Entity
                    {
                        Appearance = entAppearance,
                        Appearances = entAppearances,
                        AppearanceIndex = entAppearances.ToList().IndexOf(entAppearance),
                        ResourcePath = entityNode.EntityTemplate.DepotPath,
                        InstanceDataChanges = GetInstanceDataChanges(entityNode)
                    }
                };
                
                PopulateSpawnable(ref spawnableEntity, nodeDataEntry);
                spawnableElements.Add(spawnableEntity);
                break;
            case worldFoliageNode foliageNode:
                if ((string?)foliageNode.Mesh.DepotPath is null)
                    return spawnableElements;

                HandleEmbeddedResourceWarning(foliageNode.Mesh.DepotPath, sectorPath, ref sectorCR2W);
                
                worldSectorAbbr ??= _gfs.GetSector(sectorPath);
                if (worldSectorAbbr == null)
                    return spawnableElements;
                var abbrFoliageNodeNodeDataEntry = worldSectorAbbr.NodeData[remNode.Index];
                foreach (var transform in abbrFoliageNodeNodeDataEntry.Transforms)
                {
                    if (remNode.ActorDeletions != null && remNode.ActorDeletions.Count != 0)
                        if (remNode.ActorDeletions.All(x => x != abbrFoliageNodeNodeDataEntry.Transforms.IndexOf(transform)))
                            continue;
                    
                    
                    spawnableElements.Add(new SpawnableElement
                    {
                        Name = GetSpawnableName(foliageNode),
                        Spawnable = new Mesh
                        {
                            Position = new Vector4(transform.Position, 1),
                            EulerRotation = transform.Rotation,
                            Scale = transform.Scale,
                            
                            PrimaryRange = nodeDataEntry.MaxStreamingDistance,
                            SecondaryRange = nodeDataEntry.UkFloat1,
                            Uk10 = nodeDataEntry.Uk10,
                            Uk11 = nodeDataEntry.Uk11,
                    
                            ResourcePath = foliageNode.Mesh.DepotPath,
                            Appearance = foliageNode.MeshAppearance,
                        }
                    });
                }
                break;
            case worldInstancedDestructibleMeshNode instancedDestructibleMeshNode:
                if ((string?)instancedDestructibleMeshNode.Mesh.DepotPath is null)
                    return spawnableElements;

                HandleEmbeddedResourceWarning(instancedDestructibleMeshNode.Mesh.DepotPath, sectorPath, ref sectorCR2W);
                
                worldSectorAbbr ??= _gfs.GetSector(sectorPath);
                if (worldSectorAbbr == null)
                    return spawnableElements;
                var abbrInstancedDestructibleMeshNodeDataEntry = worldSectorAbbr.NodeData[remNode.Index];
                foreach (var transform in abbrInstancedDestructibleMeshNodeDataEntry.Transforms)
                {
                    if (remNode.ActorDeletions != null && remNode.ActorDeletions.Count != 0)
                        if (remNode.ActorDeletions.All(x => x != abbrInstancedDestructibleMeshNodeDataEntry.Transforms.IndexOf(transform)))
                            continue;
                    
                    
                    spawnableElements.Add(new SpawnableElement
                    {
                        Name = GetSpawnableName(instancedDestructibleMeshNode),
                        Spawnable = new DynamicMesh
                        {
                            Position = new Vector4(transform.Position, 1),
                            EulerRotation = transform.Rotation,
                            Scale = transform.Scale,
                    
                            CastLocalShadows = instancedDestructibleMeshNode.CastLocalShadows,
                            CastShadows = instancedDestructibleMeshNode.CastShadows,
                    
                            PrimaryRange = nodeDataEntry.MaxStreamingDistance,
                            SecondaryRange = nodeDataEntry.UkFloat1,
                            Uk10 = nodeDataEntry.Uk10,
                            Uk11 = nodeDataEntry.Uk11,
                    
                            ResourcePath = instancedDestructibleMeshNode.Mesh.DepotPath,
                            Appearance = instancedDestructibleMeshNode.MeshAppearance,
                        }
                    });
                }
                break;
            case worldPhysicalDestructionNode destructionNode:
                if ((string?)destructionNode.Mesh.DepotPath is null)
                    return spawnableElements;
                
                HandleEmbeddedResourceWarning(destructionNode.Mesh.DepotPath, sectorPath, ref sectorCR2W);
                
                var spawnabledestructionMesh = new SpawnableElement
                {
                    Name = GetSpawnableName(destructionNode),
                    Spawnable = new DynamicMesh
                    {
                        ResourcePath = destructionNode.Mesh.DepotPath,
                        Appearance = destructionNode.MeshAppearance,
                        Scale = nodeDataEntry.Scale,
                    }
                };
                PopulateSpawnable(ref spawnabledestructionMesh, nodeDataEntry);
                spawnableElements.Add(spawnabledestructionMesh);
                break;
            case worldDynamicMeshNode dynamicMeshNode:
                if ((string?)dynamicMeshNode.Mesh.DepotPath is null)
                    return spawnableElements;
                
                HandleEmbeddedResourceWarning(dynamicMeshNode.Mesh.DepotPath, sectorPath, ref sectorCR2W);
                
                var spawnabledynamicMesh = new SpawnableElement
                {
                    Name = GetSpawnableName(dynamicMeshNode),
                    Spawnable = new DynamicMesh
                    {
                        StartAsleep = dynamicMeshNode.StartAsleep,
                    }
                };
                PopulateBaseMesh(ref spawnabledynamicMesh, dynamicMeshNode, nodeDataEntry);
                spawnableElements.Add(spawnabledynamicMesh);
                break;
            case worldEffectNode effectNode:
                if ((string?)effectNode.Effect.DepotPath is null)
                    return spawnableElements;

                HandleEmbeddedResourceWarning(effectNode.Effect.DepotPath, sectorPath, ref sectorCR2W);
                
                var spawnableEffect = new SpawnableElement
                {
                    Name = GetSpawnableName(effectNode),
                    Spawnable = new Effect
                    {
                        ResourcePath = effectNode.Effect.DepotPath
                    }
                };
                PopulateSpawnable(ref spawnableEffect, nodeDataEntry);
                spawnableElements.Add(spawnableEffect);
                break;
            case worldStaticParticleNode particleNode:
                if ((string?)particleNode.ParticleSystem.DepotPath is null)
                    return spawnableElements;

                HandleEmbeddedResourceWarning(particleNode.ParticleSystem.DepotPath, sectorPath, ref sectorCR2W);
                
                var spawnableParticle = new SpawnableElement
                {
                    Name = GetSpawnableName(particleNode),
                    Spawnable = new Particle
                    {
                        ResourcePath = particleNode.ParticleSystem.DepotPath,
                        EmissionRate = particleNode.EmissionRate
                    }
                };
                PopulateSpawnable(ref spawnableParticle, nodeDataEntry);
                spawnableElements.Add(spawnableParticle);
                break;
            case worldBendedMeshNode bendedMeshNode:
                if ((string?)bendedMeshNode.Mesh.DepotPath is null)
                    return spawnableElements;

                HandleEmbeddedResourceWarning(bendedMeshNode.Mesh.DepotPath, sectorPath, ref sectorCR2W);
                
                var spawnableBendedMesh = new SpawnableElement
                {
                    Name = GetSpawnableName(bendedMeshNode),
                    Spawnable = new Mesh
                    {
                        Scale = nodeDataEntry.Scale,

                        CastLocalShadows = bendedMeshNode.CastLocalShadows,
                        CastShadows = bendedMeshNode.CastShadows,

                        ResourcePath = bendedMeshNode.Mesh.DepotPath,
                        Appearance = bendedMeshNode.MeshAppearance,
                    }
                };
                PopulateSpawnable(ref spawnableBendedMesh, nodeDataEntry);
                spawnableElements.Add(spawnableBendedMesh);
                break;
            case worldStaticDecalNode decalNode:
                if ((string?)decalNode.Material.DepotPath is null)
                    return spawnableElements;

                HandleEmbeddedResourceWarning(decalNode.Material.DepotPath, sectorPath, ref sectorCR2W);
                
                var spawnableDecalNode = new SpawnableElement
                {
                    Name = GetSpawnableName(decalNode),
                    Spawnable = new Decal
                    {
                        ResourcePath = decalNode.Material.DepotPath,
                        Alpha = decalNode.Alpha,
                        AutoHideDistance = decalNode.AutoHideDistance,
                        HorizontalFlip = decalNode.HorizontalFlip,
                        VerticalFlip = decalNode.VerticalFlip,
                        Scale = nodeDataEntry.Scale
                    }
                };
                PopulateSpawnable(ref spawnableDecalNode, nodeDataEntry);
                spawnableElements.Add(spawnableDecalNode);
                break;
            case worldTerrainMeshNode terrainMeshNode:
                if ((string?)terrainMeshNode.MeshRef.DepotPath is null)
                    return spawnableElements;
                
                HandleEmbeddedResourceWarning(terrainMeshNode.MeshRef.DepotPath, sectorPath, ref sectorCR2W);
                
                var spawnableTerrainMeshNode = new SpawnableElement
                {
                    Name = GetSpawnableName(terrainMeshNode),
                    Spawnable = new Mesh
                    {
                        Scale = nodeDataEntry.Scale,
                        ResourcePath = terrainMeshNode.MeshRef.DepotPath
                    }
                };
                
                PopulateSpawnable(ref spawnableTerrainMeshNode, nodeDataEntry);
                spawnableElements.Add(spawnableTerrainMeshNode);
                break;
            case worldWaterPatchNode waterPatchNode:
                if ((string?)waterPatchNode.Mesh.DepotPath is null)
                    return spawnableElements;
                
                HandleEmbeddedResourceWarning(waterPatchNode.Mesh.DepotPath, sectorPath, ref sectorCR2W);
                
                var spawnableWaterPatchNode = new SpawnableElement
                {
                    Name = GetSpawnableName(waterPatchNode),
                    Spawnable = new WaterPatch
                    {
                        Depth = waterPatchNode.Depth
                    }
                };
                PopulateBaseMesh(ref spawnableWaterPatchNode, waterPatchNode, nodeDataEntry);
                spawnableElements.Add(spawnableWaterPatchNode);
                break;
            case worldClothMeshNode clothMeshNode:
                if ((string?)clothMeshNode.Mesh.DepotPath is null)
                    return spawnableElements;
                
                HandleEmbeddedResourceWarning(clothMeshNode.Mesh.DepotPath, sectorPath, ref sectorCR2W);
                
                var spawnableClothMeshNode = new SpawnableElement
                {
                    Name = GetSpawnableName(clothMeshNode),
                    Spawnable = new ClothMesh
                    {
                        AffectedByWind = clothMeshNode.AffectedByWind
                    }
                };
            
                PopulateBaseMesh(ref spawnableClothMeshNode, clothMeshNode, nodeDataEntry);
                spawnableElements.Add(spawnableClothMeshNode);
                break;
            case worldRotatingMeshNode rotatingMeshNode:
                if ((string?)rotatingMeshNode.Mesh.DepotPath is null)
                    return spawnableElements;

                HandleEmbeddedResourceWarning(rotatingMeshNode.Mesh.DepotPath, sectorPath, ref sectorCR2W);
                
                var spawnableRotatingMeshNode = new SpawnableElement
                {
                    Name = GetSpawnableName(rotatingMeshNode),
                    Spawnable = new RotatingMesh
                    {
                        Duration = rotatingMeshNode.FullRotationTime,
                        Axis = rotatingMeshNode.RotationAxis,
                        Reverse = rotatingMeshNode.ReverseDirection
                    }
                };
                
                PopulateBaseMesh(ref spawnableRotatingMeshNode, rotatingMeshNode, nodeDataEntry);
                spawnableElements.Add(spawnableRotatingMeshNode);
                break;
            case worldInstancedMeshNode instancedMeshNode:
                if ((string?)instancedMeshNode.Mesh.DepotPath is null)
                    return spawnableElements;

                HandleEmbeddedResourceWarning(instancedMeshNode.Mesh.DepotPath, sectorPath, ref sectorCR2W);
                
                worldSectorAbbr ??= _gfs.GetSector(sectorPath);
                if (worldSectorAbbr == null)
                    return spawnableElements;
                var abbrInstancedMeshNodeDataEntry = worldSectorAbbr.NodeData[remNode.Index];
                foreach (var transform in abbrInstancedMeshNodeDataEntry.Transforms)
                {
                    if (remNode.ActorDeletions != null && remNode.ActorDeletions.Count != 0)
                        if (remNode.ActorDeletions.All(x => x != abbrInstancedMeshNodeDataEntry.Transforms.IndexOf(transform)))
                            continue;
                    
                    spawnableElements.Add(new SpawnableElement
                    {
                        Name = GetSpawnableName(instancedMeshNode),
                        Spawnable = new Mesh
                        {
                            Position = new Vector4(transform.Position, 1),
                            EulerRotation = transform.Rotation,
                            Scale = transform.Scale,
                    
                            CastLocalShadows = instancedMeshNode.CastLocalShadows,
                            CastShadows = instancedMeshNode.CastShadows,
                    
                            PrimaryRange = nodeDataEntry.MaxStreamingDistance,
                            SecondaryRange = nodeDataEntry.UkFloat1,
                            Uk10 = nodeDataEntry.Uk10,
                            Uk11 = nodeDataEntry.Uk11,
                    
                            ResourcePath = instancedMeshNode.Mesh.DepotPath,
                            Appearance = instancedMeshNode.MeshAppearance,
                        }
                    });
                }
                break;
            case worldMeshNode meshNode:
                if ((string?)meshNode.Mesh.DepotPath is null)
                    return spawnableElements;

                HandleEmbeddedResourceWarning(meshNode.Mesh.DepotPath, sectorPath, ref sectorCR2W);
                
                var spawnableMeshNode = new SpawnableElement
                {
                    Name = GetSpawnableName(meshNode),
                    Spawnable = new Mesh()
                };
                
                PopulateBaseMesh(ref spawnableMeshNode, meshNode, nodeDataEntry);
                
                spawnableElements.Add(spawnableMeshNode);
                break;
            default:
                var nodeTypeString = node?.GetType().ToString() ?? "";
                if (!_warnedTypes.Contains(nodeTypeString))
                {
                    Logger.Warning($"{nodeTypeString} is not a supported node type. Skipping...");
                    _warnedTypes.Add(nodeTypeString);
                }
                break;
        }
        return spawnableElements;
    }

    private static Dictionary<string, JObject> GetInstanceDataChanges(worldEntityNode entityNode)
    {
        var outDict = new Dictionary<string, JObject>();
        if (entityNode.InstanceData?.Chunk?.Buffer.Data is not RedPackage instanceData)
            return outDict;

        foreach (var kvp in instanceData.ChunkDictionary)
            outDict.Add(kvp.Value.ToString(), CleanInstanceDataChanges(kvp.Key));
        
        return outDict;
    }

    private static JObject CleanInstanceDataChanges(IRedType sparseValue)
    {
        var type = sparseValue.GetType();
        var defaultInstance = Activator.CreateInstance(type);
        var defaultInstanceJObject = JsonConvert.DeserializeObject<JObject>(RedJsonSerializer.Serialize(defaultInstance));

        var redSerializedJObject = JsonConvert.DeserializeObject<JObject>(RedJsonSerializer.Serialize(sparseValue));
        var outSerialized = new JObject();

        foreach (var prop in defaultInstanceJObject.Properties())
        {
            var matchingProp = redSerializedJObject.Properties().FirstOrDefault(x => x.Name == prop.Name);
            if (matchingProp == null)
                continue;
            
            if (!JToken.DeepEquals(prop.Value, matchingProp.Value))
                outSerialized.Add(prop.Name, matchingProp.Value);
        }
        return outSerialized;
    }
    
    
    private static void PopulateBaseMesh(ref SpawnableElement se, worldMeshNode meshNode, worldNodeData nodeDataEntry)
    {
        var mesh = (Mesh)se.Spawnable;
        mesh.Scale = nodeDataEntry.Scale;

        mesh.CastLocalShadows = meshNode.CastLocalShadows;
        mesh.CastRayTracedLocalShadows = meshNode.CastRayTracedLocalShadows;
        mesh.CastRayTracedGlobalShadows = meshNode.CastRayTracedGlobalShadows;
        mesh.CastShadows = meshNode.CastShadows;

        mesh.WindImpulseEnabled = meshNode.WindImpulseEnabled;
        
        se.Spawnable.ResourcePath = meshNode.Mesh.DepotPath;
        se.Spawnable.Appearance = meshNode.MeshAppearance;
        
        PopulateSpawnable(ref se,nodeDataEntry);
    }
    
    private static void PopulateSpawnable(ref SpawnableElement se, worldNodeData nodeDataEntry)
    {
        se.Spawnable.Position = nodeDataEntry.Position;
        se.Spawnable.EulerRotation = nodeDataEntry.Orientation;

        se.Spawnable.PrimaryRange = nodeDataEntry.MaxStreamingDistance;
        se.Spawnable.SecondaryRange = nodeDataEntry.UkFloat1;
        se.Spawnable.Uk10 = nodeDataEntry.Uk10;
        se.Spawnable.Uk11 = nodeDataEntry.Uk11;
    }

    private static string GetSpawnableName(worldNode node)
    {
        var name = node switch
        {
            worldFoliageNode foliageNode => $"{foliageNode.DebugName} {foliageNode.Mesh.DepotPath.GetString()}",
            worldInstancedDestructibleMeshNode instancedDestructibleMeshNode => $"{instancedDestructibleMeshNode.DebugName} {instancedDestructibleMeshNode.Mesh.DepotPath.GetString()}",
            worldPhysicalDestructionNode destructionNode => $"{destructionNode.DebugName} {destructionNode.Mesh.DepotPath.GetString()}",
            worldDynamicMeshNode dynamicMeshNode => $"{dynamicMeshNode.DebugName} {dynamicMeshNode.Mesh.DepotPath.GetString()}",
            worldEffectNode effectNode => $"{effectNode.DebugName} {effectNode.Effect.DepotPath.GetString()}",
            worldStaticParticleNode particleNode => $"{particleNode.DebugName} {particleNode.ParticleSystem.DepotPath.GetString()}",
            worldBendedMeshNode bendedMeshNode => $"{bendedMeshNode.DebugName} {bendedMeshNode.Mesh.DepotPath.GetString()}",
            worldStaticDecalNode decalNode => $"{decalNode.DebugName} {decalNode.Material.DepotPath.GetString()}",
            worldTerrainMeshNode terrainMeshNode => $"{terrainMeshNode.DebugName} {terrainMeshNode.MeshRef.DepotPath.GetString()}",
            worldWaterPatchNode waterPatchNode => $"{waterPatchNode.DebugName} {waterPatchNode.Mesh.DepotPath.GetString()}",
            worldEntityNode entityNode => $"{entityNode.DebugName} {entityNode.EntityTemplate.DepotPath.GetString()}",
            worldInstancedMeshNode instancedMeshNode => $"{instancedMeshNode.DebugName} {instancedMeshNode.Mesh.DepotPath.GetString()}",
            worldMeshNode meshNode => $"{meshNode.DebugName} {meshNode.Mesh.DepotPath.GetString()}",
            _ => node.DebugName.ToString()
        };
        
        return string.IsNullOrEmpty(name) ? "Generated by VS2077" : name;
    }

    private void HandleEmbeddedResourceWarning(string resourcePath, string sectorPath, ref CR2WFile sector)
    {
        if (sector.EmbeddedFiles.All(f => f.FileName != resourcePath))
            return;

        if (_embeddedResourcePaths.TryGetValue(sectorPath, out var embeddedResourcePaths))
        {
            if (embeddedResourcePaths.Contains(resourcePath))
                return;
            
            embeddedResourcePaths.Add(resourcePath);
        }
        else
        {
            _embeddedResourcePaths.Add(sectorPath, new List<string> { resourcePath });
        }
    }
}