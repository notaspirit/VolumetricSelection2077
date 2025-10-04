using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Json.Helpers;
using VolumetricSelection2077.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VolumetricSelection2077.Services;

public partial class PostProcessingService
{
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
        switch (_settingsService.SaveFileLocation)
        {
            case SaveFileLocation.GameDirectory:
                outputFilePath = Path.Join(_settingsService.GameDirectory, "archive", "pc", "mod", _settingsService.OutputFilename) + ".xl";
                break;
            case SaveFileLocation.OutputDirectory:
                outputFilePath = Path.Join(_settingsService.OutputDirectory, _settingsService.OutputFilename) + ".xl";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        var (outputContent, mergeChanges) = SerializeAxlRemovalFile(axlRemovalFile, outputFilePath);
        
        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
        
        if (!File.Exists(outputFilePath))
        {
            if (_settingsService.SaveMode == SaveFileMode.Subtract)
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
            case SaveFileMode.Extend:
            {
                File.WriteAllText(outputFilePath, outputContent);
                var newSectorS = mergeChanges?.newSectors != 1 ? "s" : "";
                var newNodesS = mergeChanges?.newNodes != 1 ? "s" : "";
                var newActorsS = mergeChanges?.newActors != 1 ? "s" : "";
                Logger.Info($"Extended file {outputFilePath} with {mergeChanges.newSectors} new sector{newSectorS}, {mergeChanges.newNodes} new node{newNodesS}, {mergeChanges.newActors} new actor{newActorsS}.");
                WriteBackupFile(outputFilePath, outputContent);
                return;
            }
            case SaveFileMode.Overwrite:
                File.WriteAllText(outputFilePath, outputContent);
                Logger.Info($"Overwrote file {outputFilePath}");
                WriteBackupFile(outputFilePath, outputContent);
                return;
            case SaveFileMode.New:
                var newOutputFilePath = GetOutputFilename(outputFilePath);
                File.WriteAllText(newOutputFilePath, outputContent);
                Logger.Info($"Created file {newOutputFilePath}");
                WriteBackupFile(newOutputFilePath, outputContent);
                return;
            case SaveFileMode.Subtract:
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
            AxlRemovalFile? existingRemovals = null;

            if (_settingsService.SaveMode is SaveFileMode.Extend or SaveFileMode.Subtract)
            {
                var fileContent = File.ReadAllText(outputFilePath);
                existingRemovals = UtilService.TryParseAxlRemovalFile(fileContent);
                if (existingRemovals == null)
                    throw new FileLoadException($"Failed to find or parse existing Removal File at {outputFilePath}!"); 
            }

            (axlRemovalFile, mergeChanges) = _settingsService.SaveMode switch
            {
                SaveFileMode.Extend => MergeSectors(existingRemovals!, axlRemovalFile),
                SaveFileMode.Subtract => SubtractRemovals(existingRemovals!, axlRemovalFile),
                _ => (axlRemovalFile, mergeChanges)
            };
        }
        
        string outputContent;

        switch (_settingsService.SaveFileFormat)
        {
            case  SaveFileFormat.ArchiveXLJson:
                outputContent = JsonConvert.SerializeObject(axlRemovalFile, JsonSerializerPresets.WorldBuilder);
                break;
            case  SaveFileFormat.ArchiveXLYaml:
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
    /// Subtracts the AxlRemovalFile from the baseFile
    /// </summary>
    /// <param name="baseFile"></param>
    /// <param name="subtraction"></param>
    /// <returns></returns>
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
}