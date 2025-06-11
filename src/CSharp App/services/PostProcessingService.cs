using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    
    public PostProcessingService()
    {
        _progress = Progress.Instance;
        _settingsService = SettingsService.Instance;
        _removalToWorldBuilder = new AxlRemovalToWorldBuilder();
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
        
        var nodeCount = nullCheckedSectors.Select(s => s?.NodeDeletions.Count).Sum();
        var actorCount = nullCheckedSectors.SelectMany(s => s?.NodeDeletions ?? new()).Sum(n => n.ActorDeletions?.Count);
        Logger.Success($"Found {nodeCount} node deletions and {actorCount} actor deletions across {nullCheckedSectors.Count} sectors.");

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
                SaveAsWorldBuilderPrefab(removalFile);
                break;
        }
    }

    /// <summary>
    /// Saves the AxlRemovalFile as an xl file in either JSON or YAML depending on the state of the SettingsService
    /// </summary>
    /// <param name="axlRemovalFile"></param>
    private void SaveAsRemoval(AxlRemovalFile axlRemovalFile)
    {
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
                int totalCount = 1;
                string outputFilePathWithoutExtension = outputFilePath.Split('.').First();
                foreach (var file in Directory.GetFiles(Path.GetDirectoryName(outputFilePath), "*.*",
                             SearchOption.AllDirectories))
                {
                    if (!file.StartsWith(outputFilePathWithoutExtension)) continue;
                    if (!Int32.TryParse(file.Split("+").Last().Split(".").First(), out int count))
                        continue;
                    if (count >= totalCount) 
                        totalCount = count + 1;
                }
                string newOutputFilePath = $"{outputFilePathWithoutExtension.Split("+").First()}+{totalCount}.xl";
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
    /// Saves the AxlRemovalFile as a World Builder prefab.
    /// </summary>
    /// <param name="axlRemovalFile"></param>
    private void SaveAsWorldBuilderPrefab(AxlRemovalFile axlRemovalFile)
    {
        var favoritesPath = Path.Join(_settingsService.GameDirectory, "bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "entSpawner", "data", "favorite");
        var favoritesFileRootName =  Path.Join(favoritesPath, "GeneratedByVS2077");
        
        var vs2077FavoriteFiles = Directory.GetFiles(favoritesPath, "*.*").Where(f => f.StartsWith(favoritesFileRootName)).ToList();
        
        Dictionary<(string, int), FavoritesRoot> vs2077Favorites = new Dictionary<(string, int), FavoritesRoot>();
        
        if (vs2077FavoriteFiles.Count == 0)
        {
            vs2077Favorites.Add((favoritesFileRootName + ".json", 0), new FavoritesRoot());
        }
        else
        {
            foreach (var favoriteFile in vs2077FavoriteFiles)
            {
                var deserialized = JsonConvert.DeserializeObject<FavoritesRoot>(File.ReadAllText(favoriteFile));
                if (deserialized == null)
                {
                    Logger.Warning($"Failed to read {favoriteFile}!");
                    continue;
                }

                Int32.TryParse(favoriteFile.Split("+").Last().Split(".").First(), out int count);
                vs2077Favorites.Add((favoriteFile, count), deserialized);
            }
        }

        var convertedElement = _removalToWorldBuilder.Convert(axlRemovalFile, _settingsService.OutputFilename);
        
        if (vs2077Favorites.Select(d => d.Value)
            .SelectMany(f => f.Favorites)
            .All(f => f.Name != _settingsService.OutputFilename.Replace(@"\", @"\\")))
        {
            var newestFilePath = vs2077Favorites.Select(d => d.Key).OrderByDescending(k => k.Item2).First();
            
            FavoritesRoot favRoot;
            if (!File.Exists(newestFilePath.Item1))
            {
                favRoot = new FavoritesRoot
                {
                    Favorites = new()
                    {
                        new Favorite
                        {
                            Data = (Positionable)convertedElement,
                            Name = _settingsService.OutputFilename
                        }
                    }
                };
            }
            else
            {
                var newestFileInfo = new FileInfo(newestFilePath.Item1);
                if (newestFileInfo.Length >= MaxJsonSize)
                {
                    newestFilePath.Item1 = favoritesPath + $"+{newestFilePath.Item2 + 1}.json";
                    favRoot = new FavoritesRoot
                    {
                        Favorites = new()
                        {
                            new Favorite
                            {
                                Data = (Positionable)convertedElement,
                                Name = _settingsService.OutputFilename
                            }
                        }
                    };
                }
                else
                {
                    favRoot = JsonConvert.DeserializeObject<FavoritesRoot>(File.ReadAllText(newestFilePath.Item1));
                    favRoot.Favorites.Add(new Favorite
                    {
                        Data = (Positionable)convertedElement,
                        Name = _settingsService.OutputFilename
                    });
                }
            }

            var serialized = JsonConvert.SerializeObject(favRoot);
            File.WriteAllText(newestFilePath.Item1, serialized);
            Logger.Info($"Created Prefab at {newestFilePath.Item1} with the name {_settingsService.OutputFilename}");
            return;
        }
        
        switch (_settingsService.SaveMode)
        {
            case SaveFileMode.Enum.Overwrite:
                var match = vs2077Favorites
                    .SelectMany(kvp => kvp.Value.Favorites
                        .Where(f => f.Name == _settingsService.OutputFilename.Replace(@"\", @"\\"))
                        .Select(f => new { Key = kvp.Key, Favorite = f }))
                    .FirstOrDefault()!;
                
                var key = match.Key;
                var existingPrefab = match.Favorite;
                
                existingPrefab.Data =  (Positionable)convertedElement;
                var serialized = JsonConvert.SerializeObject(vs2077Favorites[key]);
                File.WriteAllText(key.Item1, serialized);
                Logger.Info($"Overwrote Prefab {_settingsService.OutputFilename} in {key.Item1}");
                return;
            case SaveFileMode.Enum.Extend:
                Logger.Warning("Extending Prefabs is not yet implemented, creating a new one instead!");
                goto CreateNew;
            case SaveFileMode.Enum.New:
                CreateNew:
                var matches = vs2077Favorites
                    .SelectMany(kvp => kvp.Value.Favorites
                        .Where(f => f.Name == _settingsService.OutputFilename.Replace(@"\", @"\\"))
                        .Select(f => new { Key = kvp.Key, Favorite = f }))
                    .ToList();

                var totalCount = 1;
                foreach (var newMatch in matches)
                {
                    if (!Int32.TryParse(newMatch.Key.Item1.Split("+").Last().Split(".").First(), out int count))
                        continue;
                    if (count >= totalCount)
                        totalCount = count + 1;
                }

                var newOutputName = _settingsService.OutputFilename + $"{(totalCount > 1 ? "+" + totalCount : "")}";
                
                var newestFilePath = vs2077Favorites.Select(d => d.Key).OrderByDescending(k => k.Item2).First();

                FavoritesRoot favRoot;
                if (!File.Exists(newestFilePath.Item1))
                {
                    favRoot = new FavoritesRoot
                    {
                        Favorites = new()
                        {
                            new Favorite
                            {
                                Data = (Positionable)convertedElement,
                                Name = newOutputName
                            }
                        }
                    };
                }
                else
                {
                    var newestFileInfo = new FileInfo(newestFilePath.Item1);
                    if (newestFileInfo.Length >= MaxJsonSize)
                    {
                        newestFilePath.Item1 = favoritesPath + $"+{newestFilePath.Item2 + 1}.json";
                        favRoot = new FavoritesRoot
                        {
                            Favorites = new()
                            {
                                new Favorite
                                {
                                    Data = (Positionable)convertedElement,
                                    Name = newOutputName
                                }
                            }
                        };
                    }
                    else
                    {
                        favRoot = JsonConvert.DeserializeObject<FavoritesRoot>(File.ReadAllText(newestFilePath.Item1));
                        favRoot.Favorites.Add(new Favorite
                        {
                            Data = (Positionable)convertedElement,
                            Name = newOutputName
                        });
                    }
                }
                
                var newSerialized = JsonConvert.SerializeObject(favRoot);
                File.WriteAllText(newestFilePath.Item1, newSerialized);
                Logger.Info($"Created Prefab at {newestFilePath.Item1} with the name {newOutputName}");
                break;
        }
    }
}