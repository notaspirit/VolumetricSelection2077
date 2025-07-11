using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Newtonsoft.Json;
using VolumetricSelection2077.Converters;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Models.WorldBuilder.Favorites;
using VolumetricSelection2077.Resources;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VolumetricSelection2077.Services;

public class PostProcessingService
{
    private readonly Progress _progress;
    private readonly SettingsService _settingsService;
    private readonly AxlRemovalToWorldBuilder _removalToWorldBuilder;
    private const long MaxJsonSize = 100 * 1024 * 1024;
    private readonly JsonSerializerSettings options;
    
    
    public PostProcessingService()
    {
        _progress = Progress.Instance;
        _settingsService = SettingsService.Instance;
        _removalToWorldBuilder = new AxlRemovalToWorldBuilder();
        options = new JsonSerializerSettings
        {
            Converters =
                { new WorldBuilderElementJsonConverter(), new WorldBuilderSpawnableJsonConverter() },
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };
    }
    
    /// <summary>
    /// Entry method to be called from ProcessingService
    /// </summary>
    public void Run(AxlRemovalSector?[] rawSectors)
    {
        _progress.AddTarget(2, Progress.ProgressSections.Finalization);

        var nullCheckedSectors = rawSectors.Where(s => s != null).ToList();
        if (nullCheckedSectors.Count == 0)
        {
            Logger.Warning("No sectors intersect, no output file generated!");
            return;
        }
        
        var removalFile = new AxlRemovalFile
        {
            Streaming = new AxlRemovalStreaming
            {
                Sectors = nullCheckedSectors
            }
        };
        
        switch (_settingsService.SaveFileFormat)
        {
            case SaveFileFormat.Enum.ArchiveXLJson:
            case SaveFileFormat.Enum.ArchiveXLYaml:
                SaveAsRemoval(removalFile);
                break;
            case SaveFileFormat.Enum.WorldBuilder:
                SaveAsPrefab(removalFile);
                break;
        }
    }

    /// <summary>
    /// Saves the AxlRemovalFile as an xl file in either JSON or YAML depending on the state of the SettingsService
    /// </summary>
    /// <param name="axlRemovalFile"></param>
    private void SaveAsRemoval(AxlRemovalFile axlRemovalFile)
    {
        var nodeCount = axlRemovalFile.Streaming.Sectors.Select(s => s?.NodeDeletions.Count).Sum();
        var actorCount = axlRemovalFile.Streaming.Sectors.SelectMany(s => s?.NodeDeletions ?? new()).Sum(n => n.ActorDeletions?.Count);
        Logger.Success($"Found {nodeCount} node deletions and {actorCount} actor deletions across {axlRemovalFile.Streaming.Sectors.Count} sectors");
        
        var outputFilePath = Path.Join(_settingsService.SaveToArchiveMods ? _settingsService.GameDirectory : _settingsService.OutputDirectory, _settingsService.OutputFilename) + ".xl";
        var (outputContent, mergeChanges) = SerializeAxlRemovalFile(axlRemovalFile, outputFilePath);
        
        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                
        if (!File.Exists(outputFilePath))
        {
            File.WriteAllText(outputFilePath, outputContent);
            Logger.Info($"Created file {outputFilePath}");
            return;
        }

        switch (_settingsService.SaveMode)
        {
            case SaveFileMode.Enum.Extend:
            {
                File.WriteAllText(outputFilePath, outputContent);
                var newSectorS = mergeChanges?.newSectors != 1 ? "s" : "";
                var newNodesS = mergeChanges?.newNodes != 1 ? "s" : "";
                var newActorsS = mergeChanges?.newActors != 1 ? "s" : "";
                Logger.Info($"Extended file {outputFilePath} with {mergeChanges.newSectors} new sector{newSectorS}, {mergeChanges.newNodes} new node{newNodesS}, {mergeChanges.newActors} new actor{newActorsS}.");
                return;
            }
            case SaveFileMode.Enum.Overwrite:
                File.WriteAllText(outputFilePath, outputContent);
                Logger.Info($"Overwrote file {outputFilePath}");
                return;
            case SaveFileMode.Enum.New:
                var newOutputFilePath = GetOutputFilename(outputFilePath);
                File.WriteAllText(newOutputFilePath, outputContent);
                Logger.Info($"Created file {newOutputFilePath}");
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private (string, MergeChanges) SerializeAxlRemovalFile(AxlRemovalFile axlRemovalFile, string outputFilePath)
    {
        var mergeChanges = new MergeChanges();
        
        if (_settingsService.SaveMode == SaveFileMode.Enum.Extend && File.Exists(outputFilePath))
        {
            string fileContent = File.ReadAllText(outputFilePath);
            var existingRemovals = UtilService.TryParseAxlRemovalFile(fileContent);
            if (existingRemovals == null)
                throw new FileLoadException($"Failed to find or parse existing Removal File at {outputFilePath}!"); 
            
            (axlRemovalFile, mergeChanges) = MergeSectors(existingRemovals, axlRemovalFile);
        }
        
        string outputContent;

        switch (_settingsService.SaveFileFormat)
        {
            case  SaveFileFormat.Enum.ArchiveXLJson:
                var jsonOptions = new JsonSerializerSettings 
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                    
                };
                outputContent = JsonConvert.SerializeObject(axlRemovalFile, jsonOptions);
                break;
            case  SaveFileFormat.Enum.ArchiveXLYaml:
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                    .Build();

                outputContent = serializer.Serialize(axlRemovalFile);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        return (outputContent, mergeChanges);
    }
    
    /// <summary>
    /// Merge Two AxlRemovalFiles
    /// </summary>
    /// <param name="removals1"></param>
    /// <param name="removals2"></param>
    /// <returns></returns>
    private static (AxlRemovalFile, MergeChanges) MergeSectors(AxlRemovalFile removals1, AxlRemovalFile removals2)
    {
        var changeCount = new MergeChanges();
        var newSectors = removals2.Streaming.Sectors;
        var oldSectors = removals1.Streaming.Sectors;
        
        Dictionary<string, AxlRemovalSector> mergedDict = oldSectors.ToDictionary(x => x.Path);

        foreach (var newSector in newSectors)
        {
            if (mergedDict.TryGetValue(newSector.Path, out AxlRemovalSector existingSector))
            {
                Dictionary<int, AxlRemovalNodeDeletion> mergedNodes =
                    existingSector.NodeDeletions.ToDictionary(x => x.Index);
                foreach (var newNode in newSector.NodeDeletions)
                {
                    if (mergedNodes.TryGetValue(newNode.Index, out AxlRemovalNodeDeletion existingNode))
                    {
                        if (newNode.ActorDeletions != null || 
                            newNode.ActorDeletions?.Count > 0 ||
                            existingNode.ActorDeletions?.Count != null ||
                            existingNode.ActorDeletions?.Count > 0)
                        {
                            existingNode.ExpectedActors =  newNode.ExpectedActors ?? existingNode.ExpectedActors;
                            HashSet<int> actorSet = new HashSet<int>(newNode.ActorDeletions ?? new List<int>());
                            actorSet.UnionWith(existingNode.ActorDeletions ?? new List<int>());
                            existingNode.ActorDeletions = actorSet.ToList();
                            changeCount.newActors += actorSet.Count - existingNode.ActorDeletions.Count;
                        }
                    }
                    else
                    {
                        mergedNodes[newNode.Index] = newNode;
                        changeCount.newNodes++;
                        changeCount.newActors += newNode.ActorDeletions?.Count ?? 0;
                    }
                }
                existingSector.NodeDeletions = mergedNodes.Values.ToList();
            }
            else
            {
                mergedDict[newSector.Path] = newSector;
                changeCount.newSectors++;
                changeCount.newNodes += newSector.NodeDeletions?.Count ?? 0;
                foreach (var newNode in newSector.NodeDeletions)
                    changeCount.newActors += newNode.ActorDeletions?.Count ?? 0;
            }
        }
        var mergedSectors = mergedDict.Values.ToList();
        var mergedRemovalFile = new AxlRemovalFile
        {
            Streaming = new AxlRemovalStreaming
            {
                Sectors = mergedSectors,
            }
        };
        return (mergedRemovalFile, changeCount);
    }
    
    /// <summary>
    /// Saves the AxlRemovalFile as a world builder prefab
    /// </summary>
    /// <param name="axlRemovalFile"></param>
    private void SaveAsPrefab(AxlRemovalFile axlRemovalFile)
    {
        var favoritesPath = Path.Join(_settingsService.GameDirectory, "bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "entSpawner", "data", "favorite", "GeneratedByVS2077.json");
        var logMessage = "";
        
        
        var favRoot = new FavoritesRoot
        {
            Name = "GeneratedByVS2077",
            Favorites = new()
        };

        var convertedData = _removalToWorldBuilder.Convert(axlRemovalFile, _settingsService.OutputFilename);
        Logger.Success($"Found {convertedData.Children.OfType<PositionableGroup>().Sum(g => g.Children.Count)} WorldBuilder elements");
        if (!File.Exists(favoritesPath))
        {
            favRoot.Favorites.Add(new Favorite
            {
                Name =  _settingsService.OutputFilename,
                Data = (Positionable)convertedData
            });
            logMessage = $"Created prefab {_settingsService.OutputFilename}";
        }
        else
        {
            var existingFavorites = JsonConvert.DeserializeObject<FavoritesRoot>(File.ReadAllText(favoritesPath), new JsonSerializerSettings
            {
                Converters = { new WorldBuilderElementJsonConverter(), new WorldBuilderSpawnableJsonConverter() },
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            });
            
            switch (_settingsService.SaveMode)
            {
                case SaveFileMode.Enum.Overwrite:
                    var existingOverwritePrefab = existingFavorites?.Favorites.FirstOrDefault(f => f.Name == _settingsService.OutputFilename);
                    if (existingOverwritePrefab != null)
                    {
                        existingOverwritePrefab.Data =
                            (Positionable)convertedData;
                        logMessage = $"Overwrote prefab {_settingsService.OutputFilename}";
                    }
                    else
                        goto newPrefab;
                    break;
                case SaveFileMode.Enum.Extend:
                    var existingExtendPrefab = existingFavorites?.Favorites.FirstOrDefault(f => f.Name == _settingsService.OutputFilename);
                    if (existingExtendPrefab != null)
                    {
                        var existingElementsCount = existingExtendPrefab.Data.Children.OfType<PositionableGroup>().Sum(g => g.Children.Count);
                        existingExtendPrefab.Data = WorldBuilderMergingService.Merge(existingExtendPrefab, new Favorite
                        {
                            Name = _settingsService.OutputFilename,
                            Data = (Positionable)convertedData
                        }).Data;
                        var newElementsCount = existingExtendPrefab.Data.Children.OfType<PositionableGroup>().Sum(g => g.Children.Count);
                        logMessage = $"Extended prefab {_settingsService.OutputFilename} with {newElementsCount - existingElementsCount} new elements";
                    }
                    else
                        goto newPrefab;
                    break;
                case SaveFileMode.Enum.New:
                    newPrefab:
                    var existingNewPrefab = existingFavorites?.Favorites.FirstOrDefault(f => f.Name == _settingsService.OutputFilename);
                    if (existingNewPrefab == null)
                    {
                        existingFavorites?.Favorites.Add(new Favorite
                        {
                            Name = _settingsService.OutputFilename,
                            Data = (Positionable)convertedData
                        });
                        logMessage = $"Created prefab {_settingsService.OutputFilename}";
                    }
                    else
                    {
                        var newCount = existingFavorites?.Favorites.Count(f => f.Name.StartsWith(_settingsService.OutputFilename));
                        var newOutputFilename = $"{_settingsService.OutputFilename}+{newCount}";
                        existingFavorites?.Favorites.Add(new Favorite
                        {
                            Name = newOutputFilename,
                            Data = (Positionable)convertedData
                        });
                        logMessage = $"Created prefab {newOutputFilename}";
                    }
                    break;
            }
            
            if (existingFavorites != null)
                favRoot.Favorites.AddRange(existingFavorites.Favorites);
        }

        var serialized = JsonConvert.SerializeObject(favRoot, options); 
        File.WriteAllText(favoritesPath, serialized);
        Logger.Info(logMessage);
    }
    
    private static string GetOutputFilename(string outputFilename)
    {
        if (ValidationService.ValidatePath(outputFilename) != ValidationService.PathValidationResult.ValidFile)
            throw new ArgumentException("Invalid output filename!");
        
        if (!File.Exists(outputFilename))
            return outputFilename;
        
        int totalCount = 1;
        string outputFilePathWithoutExtension = outputFilename.Split('.').First();
        foreach (var file in Directory.GetFiles(Path.GetDirectoryName(outputFilename), "*.*",
                     SearchOption.AllDirectories))
        {
            if (!file.StartsWith(outputFilePathWithoutExtension)) continue;
            if (!Int32.TryParse(file.Split("+").Last().Split(".").First(), out int count))
                continue;
            if (count >= totalCount) 
                totalCount = count + 1;
        }
        return $"{outputFilePathWithoutExtension.Split("+").First()}+{totalCount}.{outputFilename.Split('.').Last()}";
    }

}