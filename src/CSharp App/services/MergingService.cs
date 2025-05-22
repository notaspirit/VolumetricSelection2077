using System.Collections.Generic;
using System.Linq;
using Force.DeepCloner;
using VolumetricSelection2077.Models;

namespace VolumetricSelection2077.Services
{
    public class MergingService
    {
        private GameFileService _gameFileService;
        
        public MergingService()
        {
            _gameFileService = GameFileService.Instance;
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

        public void ResolveNullProxyRefs(AxlSector sector)
        {
            var nullProxyRefs = sector.NodeDeletions?.Where(x => x.ProxyRef == null || x.ProxyRef == 0).ToList();
            if (nullProxyRefs?.Count == 0 || nullProxyRefs == null)
                return;

            var abbrsector = _gameFileService.GetSector(sector.Path);;
            foreach (var nullProxyRefNode in nullProxyRefs)
            {
                nullProxyRefNode.ProxyRef = abbrsector?.Nodes[abbrsector.NodeData[nullProxyRefNode.Index].NodeIndex].ProxyRef;
            }
        }
        
        public void RecalculateNbNodesUnderProxy(Dictionary<string, AxlSector> sectors)
        {
            foreach (var sector in sectors.Values)
            {
                ResolveNullProxyRefs(sector);
                
                if (sector.NodeMutations == null || sector.NodeMutations.Count == 0)
                    continue;
                for (var i = sector.NodeMutations.Count - 1; i >= 0; i--)
                {
                    var nodeMutation = sector.NodeMutations[i];
                    if (nodeMutation is not AxlProxyNodeMutationMutation proxyNode)
                    {
                        if (nodeMutation.Type.ToLower().Contains("proxy"))
                        {
                            nodeMutation = new AxlProxyNodeMutationMutation()
                            {
                                Index = nodeMutation.Index,
                                Type = nodeMutation.Type,
                                ProxyRef = nodeMutation.ProxyRef,
                                NbNodesUnderProxyDiff = 0,
                            };
                            proxyNode = (AxlProxyNodeMutationMutation)nodeMutation;
                        }
                        else
                            continue;
                    }
    
                    if (proxyNode.ProxyRef == null || proxyNode.ProxyRef == 0)
                    {
                        sector.NodeMutations.RemoveAt(i);
                        continue;
                    }
                    
                    var nodesReferencingThisProxy = sectors.Values
                        .Where(s => s.NodeDeletions != null)
                        .SelectMany(s => s.NodeDeletions)
                        .Where(n => n is AxlNodeBase nodeBase && nodeBase.ProxyRef == proxyNode.ProxyRef)
                        .ToList();

                    var nbNodesChange = 0;
                    foreach (var node in nodesReferencingThisProxy)
                    {
                        switch (node)
                        {
                            case AxlCollisionNodeDeletion col:
                                nbNodesChange -= col.ActorDeletions.Count;
                                break;
                            case AxlInstancedNodeDeletion inst:
                                nbNodesChange -= inst.InstanceDeletions.Count;
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
        }

        public static SectorMergeChangesCount CalculateDifference(AxlModificationFile merged, AxlModificationFile original)
        {
            return new SectorMergeChangesCount
            {
                newSectors = merged.Streaming.Sectors.Count - original.Streaming.Sectors.Count,
                newNodes = merged.Streaming.Sectors.Sum(s => s.NodeDeletions?.Count ?? 0) +
                           merged.Streaming.Sectors.Sum(s => s.NodeMutations?.Count ?? 0) -
                           original.Streaming.Sectors.Sum(s => s.NodeDeletions?.Count ?? 0) -
                           original.Streaming.Sectors.Sum(s => s.NodeMutations?.Count ?? 0),
                newActors = merged.Streaming.Sectors.Where(s => s.NodeDeletions != null)
                                .SelectMany(s => s.NodeDeletions)
                                .Where(n => n is AxlCollisionNodeDeletion)
                                .Sum(n => (n as AxlCollisionNodeDeletion).ActorDeletions.Count) -
                            original.Streaming.Sectors.Where(s => s.NodeDeletions != null)
                                .SelectMany(s => s.NodeDeletions)
                                .Where(n => n is AxlCollisionNodeDeletion)
                                .Sum(n => (n as AxlCollisionNodeDeletion).ActorDeletions.Count),
                newInstances = merged.Streaming.Sectors.Where(s => s.NodeDeletions != null)
                                   .SelectMany(s => s.NodeDeletions)
                                   .Where(n => n is AxlInstancedNodeDeletion)
                                   .Sum(n => (n as AxlInstancedNodeDeletion).InstanceDeletions.Count) -
                               original.Streaming.Sectors.Where(s => s.NodeDeletions != null)
                                   .SelectMany(s => s.NodeDeletions)
                                   .Where(n => n is AxlInstancedNodeDeletion)
                                   .Sum(n => (n as AxlInstancedNodeDeletion).InstanceDeletions.Count)
            };
        }
    }
}
