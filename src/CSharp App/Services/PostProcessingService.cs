using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using VolumetricSelection2077.Converters;
using VolumetricSelection2077.Json;
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
    private readonly JsonSerializerSettings _jsonOptions;
    
    
    public PostProcessingService()
    {
        _progress = Progress.Instance;
        _settingsService = SettingsService.Instance;
        _removalToWorldBuilder = new AxlRemovalToWorldBuilder();
        _jsonOptions = new JsonSerializerSettings
        {
            Converters =
                { new WorldBuilderElementJsonConverter(),
                    new WorldBuilderSpawnableJsonConverter(),
                    new WorldBuilderElementListConverter(),
                    new ColorToColorArray() },
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
        
        _progress.AddCurrent(1, Progress.ProgressSections.Finalization);
        
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
        
        _progress.AddCurrent(1, Progress.ProgressSections.Finalization);
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
        
        string outputFilePath;
        if (_settingsService.SaveToArchiveMods)
            outputFilePath = Path.Join(_settingsService.GameDirectory, "archive", "pc", "mod", _settingsService.OutputFilename) + ".xl";
        else
            outputFilePath = Path.Join(_settingsService.OutputDirectory, _settingsService.OutputFilename) + ".xl";
        
        var (outputContent, mergeChanges) = SerializeAxlRemovalFile(axlRemovalFile, outputFilePath);
        
        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
        
        if (!File.Exists(outputFilePath))
        {
            if (_settingsService.SaveMode == SaveFileMode.Enum.Subtract)
            {
                Logger.Error($"No Existing File to remove from found at {outputFilePath}");
                return;
            }
            
            File.WriteAllText(outputFilePath, outputContent);
            Logger.Info($"Created file {outputFilePath}");
            WriteBackupFile(outputFilePath, outputContent);
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
                WriteBackupFile(outputFilePath, outputContent);
                return;
            }
            case SaveFileMode.Enum.Overwrite:
                File.WriteAllText(outputFilePath, outputContent);
                Logger.Info($"Overwrote file {outputFilePath}");
                WriteBackupFile(outputFilePath, outputContent);
                return;
            case SaveFileMode.Enum.New:
                var newOutputFilePath = GetOutputFilename(outputFilePath);
                File.WriteAllText(newOutputFilePath, outputContent);
                Logger.Info($"Created file {newOutputFilePath}");
                WriteBackupFile(newOutputFilePath, outputContent);
                return;
            case SaveFileMode.Enum.Subtract:
                File.WriteAllText(outputFilePath, outputContent);
                var remSectorS = mergeChanges?.newSectors != 1 ? "s" : "";
                var remNodesS = mergeChanges?.newNodes != 1 ? "s" : "";
                var remActorsS = mergeChanges?.newActors != 1 ? "s" : "";
                Logger.Info($"Removed {mergeChanges.newSectors} sector{remSectorS}, {mergeChanges.newNodes} node{remNodesS}, {mergeChanges.newActors} actor{remActorsS} from file {outputFilePath}.");
                WriteBackupFile(outputFilePath, outputContent);
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private (string, MergeChanges) SerializeAxlRemovalFile(AxlRemovalFile axlRemovalFile, string outputFilePath)
    {
        var mergeChanges = new MergeChanges();

        if (File.Exists(outputFilePath))
        {
            string fileContent = File.ReadAllText(outputFilePath);
            var existingRemovals = UtilService.TryParseAxlRemovalFile(fileContent);
            if (existingRemovals == null)
                throw new FileLoadException($"Failed to find or parse existing Removal File at {outputFilePath}!"); 

            if (_settingsService.SaveMode == SaveFileMode.Enum.Extend)
            {
                (axlRemovalFile, mergeChanges) = MergeSectors(existingRemovals, axlRemovalFile);
            }
            else if (_settingsService.SaveMode == SaveFileMode.Enum.Subtract)
            {
                (axlRemovalFile, mergeChanges) = SubtractRemovals(existingRemovals, axlRemovalFile);
            }
        }
        
        string outputContent;

        switch (_settingsService.SaveFileFormat)
        {
            case  SaveFileFormat.Enum.ArchiveXLJson:
                outputContent = JsonConvert.SerializeObject(axlRemovalFile, _jsonOptions);
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

    private static (AxlRemovalFile, MergeChanges) SubtractRemovals(AxlRemovalFile baseFile, AxlRemovalFile subtraction)
    {
        var mc = new MergeChanges();
        foreach (var sector in subtraction.Streaming.Sectors)
        {
            var baseSector = baseFile.Streaming.Sectors.FirstOrDefault(s => s.Path == sector.Path);
            if (baseSector == null)
                continue;
            
            foreach (var node in sector.NodeDeletions)
            {
                var baseNode = baseSector.NodeDeletions.FirstOrDefault(n => n.Index == node.Index);
                if (baseNode == null)
                    continue;
                
                if (node.ActorDeletions != null && baseNode.ActorDeletions != null)
                {
                    var baseActorSet = new HashSet<int>(baseNode.ActorDeletions);
                    var subActorSet = new HashSet<int>(node.ActorDeletions);
                    var diff = baseActorSet.Except(subActorSet).ToList();
                    baseNode.ActorDeletions = diff;
                    mc.newActors += diff.Count;
                    
                    if (baseNode.ActorDeletions.Count == 0)
                    {
                        baseSector.NodeDeletions.Remove(baseNode);
                        mc.newNodes++;
                    }
                }
                else
                {
                    baseSector.NodeDeletions.Remove(baseNode);
                    mc.newNodes++;
                }
            }

            if (baseSector.NodeDeletions.Count == 0)
            {
                baseFile.Streaming.Sectors.Remove(baseSector);
                mc.newSectors++;
            }
        }
        
        return (baseFile, mc);
    }
    
    /// <summary>
    /// Saves the AxlRemovalFile as a world builder prefab
    /// </summary>
    /// <param name="axlRemovalFile"></param>
    private void SaveAsPrefab(AxlRemovalFile axlRemovalFile)
    {
        var favoritesPath = _settingsService.SaveToArchiveMods ?
            Path.Join(_settingsService.GameDirectory, "bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "entSpawner", "data", "favorite", "GeneratedByVS2077.json") :
            Path.Join(_settingsService.OutputDirectory, "GeneratedByVS2077.json");
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
            logMessage = $"Created prefab {_settingsService.OutputFilename} at {favoritesPath}";
        }
        else
        {
            var existingFavorites = JsonConvert.DeserializeObject<FavoritesRoot>(File.ReadAllText(favoritesPath), _jsonOptions);
            
            switch (_settingsService.SaveMode)
            {
                case SaveFileMode.Enum.Overwrite:
                    var existingOverwritePrefab = existingFavorites?.Favorites.FirstOrDefault(f => f.Name == _settingsService.OutputFilename);
                    if (existingOverwritePrefab != null)
                    {
                        existingOverwritePrefab.Data =
                            (Positionable)convertedData;
                        logMessage = $"Overwrote prefab {_settingsService.OutputFilename} at {favoritesPath}";
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
                        logMessage = $"Extended prefab {_settingsService.OutputFilename} with {newElementsCount - existingElementsCount} new elements at {favoritesPath}";
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
                        logMessage = $"Created prefab {_settingsService.OutputFilename} at {favoritesPath}";
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
                        logMessage = $"Created prefab {newOutputFilename} at {favoritesPath}";
                    }
                    break;
            }
            
            if (existingFavorites != null)
                favRoot.Favorites.AddRange(existingFavorites.Favorites);
        }

        var serialized = JsonConvert.SerializeObject(favRoot, _jsonOptions); 
        File.WriteAllText(favoritesPath, serialized);
        Logger.Info(logMessage);
        WriteBackupFile(favoritesPath, serialized);
    }
    
    private static string GetOutputFilename(string outputFilename)
    {
        if (ValidationService.ValidatePath(outputFilename) != ValidationService.PathValidationResult.Valid)
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

    private void WriteBackupFile(string originalOutputFilePath, string content)
    {
        var dirName = $"{Path.GetFileNameWithoutExtension(originalOutputFilePath)}-{_settingsService.SaveMode}-{_settingsService.SaveFileFormat}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
        var dirPath = Path.Join(_settingsService.BackupDirectory, dirName);
        Directory.CreateDirectory(dirPath);
        File.WriteAllText(Path.Join(dirPath, Path.GetFileName(originalOutputFilePath)), content);

        string selectionFilePath;
        var relativePath = Path.Join("bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "VolumetricSelection2077",
            "data", "selection.json");
        if (!string.IsNullOrEmpty(_settingsService.CustomSelectionFilePath))
            selectionFilePath = Path.Join(_settingsService.CustomSelectionFilePath, relativePath);
        else 
            selectionFilePath = Path.Join(_settingsService.GameDirectory, relativePath);

        File.Copy(selectionFilePath, Path.Join(dirPath, Path.GetFileName(selectionFilePath)));
        File.WriteAllText(Path.Join(dirPath, "settings.json"), JsonConvert.SerializeObject(_settingsService, Formatting.Indented));
        var latestLogFile = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "Logs")).GetFiles("*.txt").OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
        if (latestLogFile != null)
        {
            File.Copy(latestLogFile.FullName, Path.Join(dirPath, Path.GetFileName(latestLogFile.FullName)));
        }
            
        var dirInfo = new DirectoryInfo(_settingsService.BackupDirectory);
        if (dirInfo.GetDirectories().Length <= _settingsService.MaxBackupFiles)
            return;
        
        var dirsToDelete = dirInfo.GetDirectories().OrderByDescending(d => d.LastWriteTime).Skip(_settingsService.MaxBackupFiles);
        foreach (var dir in dirsToDelete)
            dir.Delete(true);
    }
}