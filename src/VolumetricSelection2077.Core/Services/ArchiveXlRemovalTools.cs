using Newtonsoft.Json;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Json.Helpers;
using VolumetricSelection2077.Core.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VolumetricSelection2077.Core.Services;

public static class ArchiveXlRemovalTools
{
    public static Result<AxlRemovalFile> ProcessRemoval(AxlRemovalFile fileA, FilePostProcessingMode mode, AxlRemovalFile? fileB = null)
    {
        var result = new Result<AxlRemovalFile>();

        switch (mode)
        {
            case FilePostProcessingMode.Extend:
                if (fileB == null)
                {
                    result.Status = ResultStatus.Error;
                    result.AddMessage("Extend processing mode was selected but no file to extend with was provided");
                    return result;
                }

                var (extendedRem, extendedChangeDesc) = MergeRemovals(fileA, fileB);
                result.Data = extendedRem;
                result.AddMessage(extendedChangeDesc.HasChanges ? $"Extended file with {extendedChangeDesc.ToAdditionString()}" : "No new changes");
                return result;
            case FilePostProcessingMode.Subtract:
                if (fileB == null)
                {
                    result.Status = ResultStatus.Error;
                    result.AddMessage("Subtract processing mode was selected but no file to subtract from was provided");
                    return result;
                }

                var (subtractedRem, subtractedChangeDesc) = SubtractRemovals(fileA, fileB);
                result.Data = subtractedRem;
                result.AddMessage(subtractedChangeDesc.HasChanges ? $"Subtracted file with {subtractedChangeDesc.ToSubtractionString()}" : "No changes");
                return result;
            case FilePostProcessingMode.Overwrite:
            case FilePostProcessingMode.New:
                result.Data = fileA;
                return result;
            default:
                result.Status = ResultStatus.Error;
                result.AddMessage("Invalid file processing mode");
                return result;
        }
    }
    
    public static Result<string> SaveRemoval(AxlRemovalFile removalFile, FilePostProcessingMode mode, AxlRemovalFormat format, string path)
    {
        var result = new Result<string>();

        Result<AxlRemovalFile> processRes;
        if (mode is FilePostProcessingMode.Subtract or FilePostProcessingMode.Extend)
        {
            if (!File.Exists(path))
            {
                result.Status = ResultStatus.Error;
                result.AddMessage("File does not exist");
                return result;
            }
            var existingRemoval = Utils.TryParseAxlRemovalFile(File.ReadAllText(path));
            if (existingRemoval is null)
            {
                result.Status = ResultStatus.Error;
                result.AddMessage("Failed to parse existing removal file");
                return result;
            }
            processRes = ProcessRemoval(existingRemoval, mode, removalFile);
        }
        else
        {
            processRes = ProcessRemoval(removalFile, mode);
        }
        
        switch (processRes.Status)
        {
            case ResultStatus.Error:
                result.Status = ResultStatus.Error;
                result.AddMessage($"Failed to process removal with error {processRes.Message}");
                return result;
            case ResultStatus.Warning:
                result.Status = ResultStatus.Warning;
                result.AddMessage($"Processed removal with warning {processRes.Message}");
                break;
            case ResultStatus.Ok:
            default:
                break;
        }
        
        var serializedRes = SerializeRemoval(removalFile, format);
        switch (serializedRes.Status)
        {
            case ResultStatus.Error:
                result.Status = ResultStatus.Error;
                result.AddMessage($"Failed to serialize removal to {format} with error {serializedRes.Message}");
                break;
            case ResultStatus.Warning:
                result.Status = ResultStatus.Warning;
                result.AddMessage($"Serialized removal to {format} with warning {serializedRes.Message}");
                break;
            case ResultStatus.Ok:
            default:
                break;
        }
                
        var filePath = mode == FilePostProcessingMode.New ? GetNewFilename(path) : path;
        result.Data = filePath;
        File.WriteAllText(filePath, serializedRes.Data);
                
        return result;
    }

    public static Result<string> SerializeRemoval(AxlRemovalFile removalFile, AxlRemovalFormat format)
    {
        var result = new Result<string>();
        
        switch (format)
        {
            case AxlRemovalFormat.Json:
                try
                {
                    result.Data = JsonConvert.SerializeObject(removalFile, JsonSerializerPresets.WorldBuilder);
                }
                catch(Exception ex)
                {
                    result.Status = ResultStatus.Error;
                    result.AddMessage(ex.Message);
                }
                return result;
            case AxlRemovalFormat.Yaml:
                try
                {
                    var serializer = new SerializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                        .Build();

                    result.Data = serializer.Serialize(removalFile);
                }
                catch(Exception ex)
                {
                    result.Status = ResultStatus.Error;
                    result.AddMessage(ex.Message);
                }
                return result;
            default:
                result.Status = ResultStatus.Error;
                result.AddMessage("Unsupported AxlRemovalFormat");
                return result;
        }
    }
    
    private static string GetNewFilename(string outputFilename)
    {
        if (!File.Exists(outputFilename))
            return outputFilename;
        
        var totalCount = 1;
        var outputFilePathWithoutExtension = outputFilename.Split('.').First();
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
    
    /// <summary>
    /// Merge Two AxlRemovalFiles
    /// </summary>
    /// <param name="removals1"></param>
    /// <param name="removals2"></param>
    /// <returns></returns>
    public static (AxlRemovalFile, MergeChanges) MergeRemovals(AxlRemovalFile removals1, AxlRemovalFile removals2)
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
                            existingNode.ExpectedActors =  newNode.ExpectedActors;
                            HashSet<int> actorSet = new HashSet<int>(newNode.ActorDeletions ?? new List<int>());
                            actorSet.UnionWith(existingNode.ActorDeletions ?? new List<int>());
                            existingNode.ActorDeletions = actorSet.ToList();
                            changeCount.NewActors += actorSet.Count - existingNode.ActorDeletions.Count;
                        }
                    }
                    else
                    {
                        mergedNodes[newNode.Index] = newNode;
                        changeCount.NewNodes++;
                        changeCount.NewActors += newNode.ActorDeletions?.Count ?? 0;
                    }
                }
                existingSector.NodeDeletions = mergedNodes.Values.ToList();
            }
            else
            {
                mergedDict[newSector.Path] = newSector;
                changeCount.NewSectors++;
                changeCount.NewNodes += newSector.NodeDeletions?.Count ?? 0;
                foreach (var newNode in newSector.NodeDeletions)
                    changeCount.NewActors += newNode.ActorDeletions?.Count ?? 0;
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
    public static (AxlRemovalFile, MergeChanges) SubtractRemovals(AxlRemovalFile baseFile, AxlRemovalFile subtraction)
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
                    mc.NewActors += diff.Count;
                    
                    if (baseNode.ActorDeletions.Count == 0)
                    {
                        baseSector.NodeDeletions.Remove(baseNode);
                        mc.NewNodes++;
                    }
                }
                else
                {
                    baseSector.NodeDeletions.Remove(baseNode);
                    mc.NewNodes++;
                }
            }

            if (baseSector.NodeDeletions.Count == 0)
            {
                baseFile.Streaming.Sectors.Remove(baseSector);
                mc.NewSectors++;
            }
        }
        
        return (baseFile, mc);
    }
}