using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Documents;
using SharpDX;
using VolumetricSelection2077.Converters;
using VolumetricSelection2077.Services;
using WolvenKit.RED4.Types;
using WolvenKit.RED4.Types.Pools;
using Vector3 = WolvenKit.RED4.Types.Vector3;

namespace VolumetricSelection2077.TestingStuff;

public class GetAvgBBScaleStreamingBlockSector
{
    public static void Run(GameFileService gfs)
    {
        int testSectorsCount = 1000;
        
        
        Logger.Info("Getting basegame streamingblock...");
        string baseGameStreamingBlock = @"base\worlds\03_night_city\_compiled\default\blocks\all.streamingblock";
        var streamingblock = gfs.GetCr2WFile(baseGameStreamingBlock);
        if (streamingblock == null)
        {
            Logger.Error("Failed to load streaming block");
            return;
        }

        Logger.Info("Reading streaming block...");
        var root = streamingblock.RootChunk as worldStreamingBlock;
        Dictionary<string, BoundingBox> bbsFromBlock = new();
        
        for (int i = 0; i < testSectorsCount; i++)
        {
            var descriptor = GetRandomSectorDescriptor();
            Logger.Info(descriptor.Data.DepotPath);
            bbsFromBlock.Add(descriptor.Data.DepotPath, 
                new BoundingBox()
            {
                Minimum = WolvenkitToSharpDX.Vector3(descriptor.StreamingBox.Min),
                Maximum = WolvenkitToSharpDX.Vector3(descriptor.StreamingBox.Max)
            });
        }
        
        Dictionary<string, BoundingBox> bbsFromSectorManual = new();

        Logger.Info("Calculating rough AABB dirctly...");
        foreach (var entry in bbsFromBlock)
        {
            var sector = gfs.GetSector(entry.Key);
            SharpDX.Vector3 min = new(int.MaxValue, int.MaxValue, int.MaxValue);
            SharpDX.Vector3 max = new(int.MinValue, int.MinValue, int.MinValue);
            foreach (var nodeDataEntry in sector.NodeData)
            {
                foreach (var transform in nodeDataEntry.Transforms)
                {
                    if (transform.Position == new SharpDX.Vector3(0f, 0f, 0f) &&
                        transform.Rotation == new SharpDX.Quaternion(0f, 0f, 0f, 1f) &&
                        transform.Scale == new SharpDX.Vector3(1f, 1f, 1f))
                    {
                        Logger.Warning("ALARM! ALARM!");
                        continue;
                    }
                    
                    if (transform.Position.X < min.X) min.X = transform.Position.X;
                    if (transform.Position.Y < min.Y) min.Y = transform.Position.Y;
                    if (transform.Position.Z < min.Z) min.Z = transform.Position.Z;
                    
                    if (transform.Position.X > max.X) max.X = transform.Position.X;
                    if (transform.Position.Y > max.Y) max.Y = transform.Position.Y;
                    if (transform.Position.Z > max.Z) max.Z = transform.Position.Z;
                }
            }
            bbsFromSectorManual.Add(entry.Key, new BoundingBox()
            {
                Minimum = min,
                Maximum = max
            });
        }
        
        List<SharpDX.Vector3> scalediffs = new();
        Logger.Info("Comparing AABBs...");
        foreach (var blockEntry in bbsFromBlock)
        {
            BoundingBox manualBox;
            if (bbsFromSectorManual[blockEntry.Key].Size == new SharpDX.Vector3(0, 0, 0))
            {
                manualBox = bbsFromBlock[blockEntry.Key];
            }
            else
            {
                manualBox = bbsFromSectorManual[blockEntry.Key];   
            }
            Logger.Info($"\n" +
                        $"AABB Size from sectorBlock: {blockEntry.Value.Size}\n" +
                        $"AABB Size from Manual: {manualBox.Size}\n" +
                        $"Difference Absolute: {blockEntry.Value.Size - manualBox.Size}\n" +
                        $"Difference Percent: {(blockEntry.Value.Size/manualBox.Size)-1}");
            scalediffs.Add((blockEntry.Value.Size/manualBox.Size)-1);
        }

        float summedDiffX = 0f;
        float summedDiffY = 0f;
        float summedDiffZ = 0f;
        foreach (var scalediff in scalediffs)
        {
            summedDiffX += scalediff.X;
            summedDiffY += scalediff.Y;
            summedDiffZ += scalediff.Z;
        }
        
        float avgDiffX = summedDiffX / testSectorsCount;
        float avgDiffY = summedDiffY / testSectorsCount;
        float avgDiffZ = summedDiffZ / testSectorsCount;
        
        Logger.Info($"\n" +
                    $"Average Difference in Percent:\n" +
                    $"X: {avgDiffX}\n" +
                    $"Y: {avgDiffY}\n" +
                    $"Z: {avgDiffZ}");
        return;
        
        worldStreamingSectorDescriptor? GetRandomSectorDescriptor()
        {
            Random random = new Random();
            int randomIndex = random.Next(0, root.Descriptors.Count);
            var descriptor = root.Descriptors[randomIndex];
            if ((/*$"{descriptor.Category}" != "Interior" && */
                $"{descriptor.Category}" != "Exterior") || bbsFromBlock.ContainsKey(descriptor.Data.DepotPath))
            {
                return GetRandomSectorDescriptor();
            }
            return descriptor;
        }
    }
}