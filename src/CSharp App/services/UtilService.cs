using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using VolumetricSelection2077.Models;
using WolvenKit.Interfaces.Extensions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VolumetricSelection2077.Services
{
    public class UtilService
    {
        public static string BuildORRegex(List<string> patterns)
        {
            return string.Join("|", patterns);
        }
        public static string EscapeSlashes(string input)
        {
            return input.Replace("\\", "\\\\");
        }
        
        /// <summary>
        /// Formats TimeSpan to H Hour M minute S.MS Second with the larger ones only being added if it is not 0
        /// </summary>
        /// <param name="elapsed"></param>
        /// <returns></returns>
        public static string FormatElapsedTime(TimeSpan elapsed)
        {
            var parts = new List<string>();
        
            if (elapsed.Hours > 0)
            {
                parts.Add($"{elapsed.Hours} hour{(elapsed.Hours == 1 ? "" : "s")}");
            }
            if (elapsed.Minutes > 0)
            {
                parts.Add($"{elapsed.Minutes} minute{(elapsed.Minutes == 1 ? "" : "s")}");
            }
            if (elapsed.Seconds > 0 || parts.Count == 0)
            {
                parts.Add($"{elapsed.Seconds}.{elapsed.Milliseconds:D3} seconds");
            }
        
            return string.Join(", ", parts);
        }
        
        /// <summary>
        /// Formats elapsed seconds to MM:SS
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string FormatElapsedTimeMMSS(TimeSpan time)
        {
            return $"{time.Minutes:D2}:{time.Seconds:D2}";
        }


        public static AxlModificationFile? TryParseAxlRemovalFile(String input)
        {
            try
            {
                return JsonConvert.DeserializeObject<AxlModificationFile>(input);
            }
            catch (JsonException) { }
            
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                return deserializer.Deserialize<AxlModificationFile>(input);
            }
            catch (YamlDotNet.Core.YamlException) { }
            
            return null;
        }

        public static string SanitizeFilePath(string input)
        {
            char[] additionalInvalidChars = { '?', '*', '"', '<', '>', '|', '\\', '/' };
            var invalidCharacters = Path.GetInvalidPathChars().Concat(additionalInvalidChars).Distinct().ToArray();
            
            List<string> cleanOutput = [];
            
            var parts = input.Split(Path.DirectorySeparatorChar);
            foreach (var part in parts)
            {
                var tempPart = part.Trim();
                tempPart = new string(tempPart.Select(c => invalidCharacters.Contains(c) ? '_' : c).ToArray());
                if (!string.IsNullOrEmpty(tempPart))
                {
                    cleanOutput.Add(tempPart);
                }
                
            }
            return string.Join(Path.DirectorySeparatorChar, cleanOutput);
        }
        /// <summary>
        /// Checks if the given directory contains any files
        /// </summary>
        /// <param name="path"></param>
        /// <returns>true if no files were found</returns>
        /// <exception cref="ArgumentException">given filepath is invalid</exception>
        public static bool IsDirectoryEmpty(string path)
        {
            if (ValidationService.ValidatePath(path) != ValidationService.PathValidationResult.ValidDirectory)
                throw new ArgumentException($"Path is invalid.");
            if (Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories).Any())
                return false;
            return true;
        }

        public static (List<AxlSector>, SectorMergeChangesCount) MergeAxlSectors(List<AxlSector> listA, List<AxlSector> listB)
        {
            var changesCount = new SectorMergeChangesCount();

            Dictionary<string, AxlSector> mergedDict = listA.ToDictionary(x => x.Path);
            foreach (var sectorB in listB)
            {
                if (mergedDict.TryGetValue(sectorB.Path, out var existingSector))
                {
                    if ((existingSector.NodeDeletions != null || sectorB.NodeDeletions != null) && sectorB.NodeDeletions != null)
                    {
                        Dictionary<int, AxlNodeDeletion> mergedNodes = existingSector.NodeDeletions?.ToDictionary(x => x.Index) ?? [];
                        foreach (var nodeB in sectorB.NodeDeletions)
                        {
                            if (mergedNodes.TryGetValue(nodeB.Index, out var existingNode))
                            {
                                switch (nodeB)
                                {
                                    case AxlCollisionNodeDeletion nodeBCollision:
                                        if (existingNode is not AxlCollisionNodeDeletion existingCollisionNode)
                                        {
                                            Logger.Warning($"Node deletion type mismatch for {nodeB.Index} in {sectorB.Path}");
                                            continue;
                                        }
                                        if (nodeBCollision.ExpectedActors != existingCollisionNode.ExpectedActors)
                                            Logger.Warning($"Node deletion expected actor count mismatch for {nodeB.Index} in {sectorB.Path}, ListA: {existingCollisionNode.ExpectedActors}, ListB: {nodeBCollision.ExpectedActors}, using ListB.");

                                        HashSet<int> actorSet = [.. nodeBCollision.ActorDeletions ?? []];
                                        actorSet.UnionWith(existingCollisionNode.ActorDeletions ?? []);
                                        existingCollisionNode.ActorDeletions = actorSet.ToList();
                                        changesCount.newActors += actorSet.Count - existingCollisionNode.ActorDeletions.Count;
                                        break;
                                    case AxlInstancedNodeDeletion nodeBInstanced:
                                        if (existingNode is not AxlInstancedNodeDeletion existingInstancedNode)
                                        {
                                            Logger.Warning($"Node deletion type mismatch for {nodeB.Index} in {sectorB.Path}");
                                            continue;
                                        }
                                        if (nodeBInstanced.ExpectedInstances != existingInstancedNode.ExpectedInstances)
                                            Logger.Warning($"Node deletion expected instance count mismatch for {nodeB.Index} in {sectorB.Path}, ListA: {existingInstancedNode.ExpectedInstances}, ListB: {nodeBInstanced.ExpectedInstances}, using ListB.");
                                        HashSet<int> instanceSet = [.. nodeBInstanced.InstanceDeletions ?? []];
                                        instanceSet.UnionWith(existingInstancedNode.InstanceDeletions ?? []);
                                        existingInstancedNode.InstanceDeletions = instanceSet.ToList();
                                        changesCount.newInstances += instanceSet.Count - existingInstancedNode.InstanceDeletions.Count;
                                        break;
                                    default:
                                        Logger.Warning($"Node merging for {nodeB.GetType()} is not implemented.");
                                        break;
                                }
                            }
                            else
                            {
                                mergedNodes.Add(nodeB.Index, nodeB);
                                changesCount.newNodes++;
                                switch (nodeB)
                                {
                                    case AxlCollisionNodeDeletion:
                                        changesCount.newActors++;
                                        break;
                                    case AxlInstancedNodeDeletion:
                                        changesCount.newInstances++;
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    mergedDict.Add(sectorB.Path, sectorB);
                    changesCount.newSectors++;
                    changesCount.newNodes += sectorB.NodeMutations?.Count ?? 0 + sectorB.NodeDeletions?.Count ?? 0;

                    if (sectorB.NodeDeletions == null)
                        continue;

                    foreach (var listBNode in sectorB.NodeDeletions)
                    {
                        switch (listBNode)
                        {
                            case AxlCollisionNodeDeletion:
                                changesCount.newActors++;
                                break;
                            case AxlInstancedNodeDeletion:
                                changesCount.newInstances++;
                                break;
                        }
                    }
                }
            }

            var proxyRefCount = new Dictionary<ulong, int>();

            foreach (var sector in mergedDict.Values)
            {
                if (sector.NodeDeletions == null || sector.NodeDeletions.Count == 0)
                    continue;
                foreach (var node in sector.NodeDeletions)
                {
                    if (node.ProxyRef == null)
                        continue;
                    var proxyNodeChange = 0;
                    switch (node)
                    {
                        case AxlCollisionNodeDeletion nodeCollision:
                            proxyNodeChange = nodeCollision.ActorDeletions.Count;
                            break;
                        case AxlInstancedNodeDeletion nodeInstanced:
                            proxyNodeChange = nodeInstanced.InstanceDeletions.Count;
                            break;
                        default:
                            proxyNodeChange = 1;
                            break;
                    }
                    if (proxyRefCount.ContainsKey(node.ProxyRef.Value))
                        proxyRefCount[node.ProxyRef.Value] += proxyNodeChange;

                    else
                        proxyRefCount[node.ProxyRef.Value] = proxyNodeChange;
                }
            }

            AddMutationsToDictionary(listA);
            AddMutationsToDictionary(listB);
            changesCount.newNodes += mergedDict.Values.Sum(x => x.NodeMutations?.Count ?? 0) - listA.Sum(x => x.NodeMutations?.Count ?? 0) - listB.Sum(x => x.NodeMutations?.Count ?? 0);
            return (mergedDict.Values.ToList(), changesCount);

            void AddMutationsToDictionary(List<AxlSector> sectorList)
            {
                foreach (var sector in sectorList)
                {
                    var newMutationsList = new List<AxlNodeMutation>();

                    var mutations = sector.NodeMutations;
                    if (mutations == null || mutations.Count == 0)
                        continue;
                    foreach (var mutation in mutations)
                    {
                        if (mutation is not AxlProxyNodeMutationMutation proxyMutation)
                        {
                            Logger.Warning($"Merging of node mutation for {mutation.GetType()} is not implemented.");
                            continue;
                        }

                        if (proxyMutation.ProxyRef == null)
                        {
                            Logger.Warning($"Node mutation proxy ref is null for {proxyMutation.Index} in {sector.Path}");
                            continue;
                        }

                        if (proxyRefCount.TryGetValue((ulong)proxyMutation.ProxyRef, out var count))
                        {
                            proxyMutation.NbNodesUnderProxyDiff = count;
                            newMutationsList.Add(proxyMutation);
                        }
                        else
                        {
                            Logger.Debug($"Proxy ref {proxyMutation.ProxyRef} has no changes in {sector.Path}");
                        }
                    }
                    if (mergedDict.TryGetValue(sector.Path, out var existingSector))
                    {
                        existingSector.NodeMutations = newMutationsList;
                    }
                    else
                    {
                        mergedDict.Add(sector.Path, new AxlSector
                        {
                            Path = sector.Path,
                            NodeMutations = newMutationsList,
                            ExpectedNodes = sector.ExpectedNodes
                        });
                    }
                }
            }
        }
    }
}