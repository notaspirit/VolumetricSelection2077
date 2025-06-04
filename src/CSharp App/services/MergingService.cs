using System;
using System.Collections.Generic;
using System.Linq;
using Force.DeepCloner;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Resources;

namespace VolumetricSelection2077.Services
{
    public class MergingService
    {
        private GameFileService _gameFileService;
        private SettingsService _settings;
        private List<ulong?> _deletedQuestRefHashes = new ();
        
        public MergingService()
        {
            _gameFileService = GameFileService.Instance;
            _settings = SettingsService.Instance;
        }
        
        public AxlModificationFile MergeAxlFiles(AxlModificationFile axl1, AxlModificationFile axl2)
        {
            var result = new AxlModificationFile
            {
                Streaming = new AxlStreaming
                {
                    Sectors = MergeSectors(axl1.Streaming.Sectors, axl2.Streaming.Sectors)
                }
            };
            return result;
        }

        public List<AxlSector> MergeSectors(List<AxlSector> sectors1, List<AxlSector> sectors2)
        {
            var mergedSectors = new Dictionary<string, AxlSector>();
            foreach (var sector in sectors1.Concat(sectors2))
            {
                if (!mergedSectors.TryGetValue(sector.Path, out var existingSector))
                {
                    mergedSectors[sector.Path] = sector.DeepClone();
                }
                else
                {
                    MergeSectorNodes(existingSector, sector);
                }
            }

            RecalculateNbNodesUnderProxy(mergedSectors);

            return mergedSectors.Values.ToList();
        }

        public static void MergeSectorNodes(AxlSector target, AxlSector source)
        {
            target.NodeDeletions ??= new();
            source.NodeDeletions ??= new();

            target.NodeMutations ??= new();
            source.NodeMutations ??= new();

            MergeNodeDeletions(target.NodeDeletions, source.NodeDeletions);
            MergeNodeMutations(target.NodeMutations, source.NodeMutations);
        }

        public static void MergeNodeDeletions(List<AxlNodeDeletion> target, List<AxlNodeDeletion> source)
        {
            foreach (var deletion in source)
            {
                var indexInTarget = target.FirstOrDefault(x => x.Index == deletion.Index);
                if (indexInTarget == null)
                {
                    target.Add(deletion);
                }
                else if (deletion is AxlCollisionNodeDeletion col &&
                         indexInTarget is AxlCollisionNodeDeletion tgt)
                {
                    if (col.ExpectedActors != tgt.ExpectedActors)
                    {
                        Logger.Debug($"Expected actors do not match for index {col.Index}. Skipping merge.");
                        continue;
                    }

                    tgt.ActorDeletions = tgt.ActorDeletions.Union(col.ActorDeletions).Distinct().ToList();
                }
                else if (deletion is AxlInstancedNodeDeletion inst &&
                         indexInTarget is AxlInstancedNodeDeletion tgtInst)
                {
                    if (inst.ExpectedInstances != tgtInst.ExpectedInstances)
                    {
                        Logger.Debug($"Expected actors do not match for index {inst.Index}. Skipping merge.");
                        continue;
                    }

                    tgtInst.InstanceDeletions = tgtInst.InstanceDeletions.Union(inst.InstanceDeletions).Distinct().ToList();
                }
            }
        }

        public static void MergeNodeMutations(List<AxlNodeMutation> target, List<AxlNodeMutation> source)
        {
            foreach (var mutation in source)
            {
                var indexInTarget = target.FirstOrDefault(x => x.Index == mutation.Index);
                if (indexInTarget == null)
                {
                    target.Add(mutation);
                }
            }
        }

        public void ResolveMissingNodeProperties(AxlSector sector)
        {
            var nullProxyRefs = sector.NodeDeletions?.Where(x => x.ProxyRef is null).ToList();
            
            var missingExpectedNodes = sector.NodeMutations?.Where(x => x is AxlProxyNodeMutationMutation
            {
                ExpectedNodesUnderProxy: null
            }).Select(x => (AxlProxyNodeMutationMutation)x).ToList();
            
            var missingQuestRefProxies = sector.NodeMutations?.Where(x => x is AxlProxyNodeMutationMutation
            {
                QuestRef: null
            }).Select(x => (AxlProxyNodeMutationMutation)x).ToList();
            
            if ((nullProxyRefs?.Count == 0 || nullProxyRefs == null) && (missingExpectedNodes?.Count == 0 || missingExpectedNodes == null) && (missingQuestRefProxies?.Count == 0 ||  missingQuestRefProxies == null))
                return;

            var abbrsector = _gameFileService.GetSector(sector.Path);
            if (abbrsector == null)
                return;
            
            if (nullProxyRefs != null)
            {
                foreach (var nullProxyRefNode in nullProxyRefs)
                {
                    nullProxyRefNode.ProxyRef = abbrsector.NodeData[nullProxyRefNode.Index].ProxyRef;
                }
            }

            if (missingExpectedNodes != null)
            {
                foreach (var missingExpectedNode in missingExpectedNodes)
                {
                    missingExpectedNode.ExpectedNodesUnderProxy = abbrsector?.Nodes[abbrsector.NodeData[missingExpectedNode.Index].NodeIndex].ExpectedNodesUnderProxy;
                }
            }

            if (missingQuestRefProxies != null)
            {
                foreach (var missingQuestRefNode in missingQuestRefProxies)
                {
                    missingQuestRefNode.QuestRef = abbrsector?.NodeData[missingQuestRefNode.Index].QuestRef;
                }
            }
        }
        
        public void RecalculateNbNodesUnderProxy(Dictionary<string, AxlSector> sectors)
        {
            foreach (var sector in sectors.Values)
            { 
                ResolveMissingNodeProperties(sector);
            }
            
            
            foreach (var sector in sectors.Values)
            {
                
                if (sector.NodeMutations == null || sector.NodeMutations.Count == 0)
                    continue;
                for (var i = sector.NodeMutations.Count - 1; i >= 0; i--)
                {
                    var nodeMutation = sector.NodeMutations[i];
                    if (nodeMutation is not AxlProxyNodeMutationMutation proxyNode)
                    {
                        if (nodeMutation.Type.Contains("proxy", StringComparison.CurrentCultureIgnoreCase))
                            Logger.Warning($"Node {nodeMutation.Index}, {sector.Path} references a proxy node, but is not a proxy node mutation. Skipping...");
                        continue;
                    }
    
                    if (proxyNode.QuestRef is null or 0 || proxyNode.ExpectedNodesUnderProxy == 0)
                    {
                        sector.NodeMutations.RemoveAt(i);
                        continue;
                    }

                    var nodesReferencingThisProxy = sectors.Values
                        .Where(s => s.NodeDeletions != null)
                        .SelectMany(s => s.NodeDeletions)
                        .Where(n => n.ProxyRef == proxyNode.QuestRef)
                        .ToList();
                    
                    var nbNodesChange = 0;
                    var nbInstancesChange = 0;
                    foreach (var node in nodesReferencingThisProxy)
                    {
                        switch (node)
                        {
                            // actor and instanced nodes just need to not be counted at all for the proxy
                            // however for the removing logic it is still needed
                            case AxlCollisionNodeDeletion collisionNode:
                                nbInstancesChange -= collisionNode.ActorDeletions.Count;
                                break;
                            case AxlInstancedNodeDeletion  instancedNode:
                                nbInstancesChange -= instancedNode.InstanceDeletions.Count;
                                break;
                            default:
                                nbNodesChange--;
                                break;
                        }
                    }
                    
                    if (nbNodesChange == 0)
                    {
                        sector.NodeMutations.RemoveAt(i);
                        continue;
                    }

                    if ((nbNodesChange + nbInstancesChange) * -1 == proxyNode.ExpectedNodesUnderProxy && _settings.ResolveProxies == ProxyResolvingMode.Enum.ResolveAndDeleteUnreferenced)
                    {
                        _deletedQuestRefHashes.Add((ulong)proxyNode.QuestRef);
                        sector.NodeDeletions ??= new();
                        sector.NodeDeletions.Add(new AxlNodeDeletion
                        {
                            Index = proxyNode.Index,
                            ProxyRef = proxyNode.ProxyRef,
                            DebugName = proxyNode.DebugName,
                            Type = proxyNode.Type
                        });
                        sector.NodeMutations.RemoveAt(i);
                        continue;
                    }
                    
                    proxyNode.NbNodesUnderProxyDiff = nbNodesChange;
                }
            }
            
            foreach (var sector in sectors.Values.ToList())
            {
                if ((sector.NodeMutations == null || sector.NodeMutations.Count == 0) &&
                    (sector.NodeDeletions == null || sector.NodeDeletions.Count == 0))
                {
                    sectors.Remove(sector.Path);
                }
            }

            var allNodeRemovalProxyRefs = sectors.Values.Where(x => x.NodeDeletions is not null)
                .SelectMany(x => x.NodeDeletions).Where(x => !IsInstancedNode(x)).Where(x => x.ProxyRef is not 0).Select(x => x.ProxyRef).Distinct();
            var allProxyMutationRefs = sectors.Values.Where(x => x.NodeMutations is not null).SelectMany(x => x.NodeMutations).Select(x =>
                (x as AxlProxyNodeMutationMutation).QuestRef).Distinct().ToList();
            allProxyMutationRefs.AddRange(_deletedQuestRefHashes);
            _deletedQuestRefHashes.Clear();
            var diff = allNodeRemovalProxyRefs.Except(allProxyMutationRefs).ToList();
            
            if (diff.Count > 0)
                Logger.Warning($"Found {diff.Count} unresolved proxy references! {string.Join(", ", diff)}");
        }

        private static bool IsInstancedNode(AxlNodeDeletion node)
        {
            return node.Type is nameof(NodeTypeProcessingOptions.Enum.worldInstancedDestructibleMeshNode) or nameof(NodeTypeProcessingOptions.Enum.worldInstancedMeshNode) or nameof(NodeTypeProcessingOptions.Enum.worldCollisionNode);
        }
        
        public static SectorMergeChangesCount CalculateDifference(AxlModificationFile merged, AxlModificationFile original)
        {
            int CountNodeDeletions(List<AxlSector> sectors) =>
                sectors.Sum(s => s.NodeDeletions?.Count ?? 0);

            int CountNodeMutations(List<AxlSector> sectors) =>
                sectors.Sum(s => s.NodeMutations?.Count ?? 0);

            int CountCollisionActors(List<AxlSector> sectors) =>
                sectors.SelectMany(s => s.NodeDeletions ?? Enumerable.Empty<AxlNodeDeletion>())
                    .OfType<AxlCollisionNodeDeletion>()
                    .Sum(d => d.ActorDeletions?.Count ?? 0);

            int CountInstanceDeletions(List<AxlSector> sectors) =>
                sectors.SelectMany(s => s.NodeDeletions ?? Enumerable.Empty<AxlNodeDeletion>())
                    .OfType<AxlInstancedNodeDeletion>()
                    .Sum(d => d.InstanceDeletions?.Count ?? 0);
            
            Logger.Debug($"Existing Actors: {CountCollisionActors(original.Streaming.Sectors)}, Existing Instances: {CountInstanceDeletions(original.Streaming.Sectors)}");
            Logger.Debug($"Merged Actors: {CountCollisionActors(merged.Streaming.Sectors)}, Merged Instances: {CountInstanceDeletions(merged.Streaming.Sectors)}");
            return new SectorMergeChangesCount
            {
                newSectors = merged.Streaming.Sectors.Count - original.Streaming.Sectors.Count,
                newNodes = (CountNodeDeletions(merged.Streaming.Sectors) + CountNodeMutations(merged.Streaming.Sectors)) -
                           (CountNodeDeletions(original.Streaming.Sectors) + CountNodeMutations(original.Streaming.Sectors)),
                newActors = CountCollisionActors(merged.Streaming.Sectors) - CountCollisionActors(original.Streaming.Sectors),
                newInstances = CountInstanceDeletions(merged.Streaming.Sectors) - CountInstanceDeletions(original.Streaming.Sectors)
            };
        }
    }
}
