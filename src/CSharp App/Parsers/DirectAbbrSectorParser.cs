using System;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;
using WolvenKit.RED4.Archive.CR2W;
using WolvenKit.RED4.Types;

namespace VolumetricSelection2077.Parsers;

public class DirectAbbrSectorParser
{
    public static AbbrSector? Parse(CR2WFile input)
    {
        if (input.RootChunk is not worldStreamingSector)
        {
            throw new Exception("Input file is not a world streaming sector");
        }
        var sector = input.RootChunk as worldStreamingSector;

        foreach (var node in sector.Nodes)
        {
            var debugName = node.Chunk?.DebugName;
            var type = node.Chunk?.GetType().Name;
            
            Logger.Info($"{debugName ?? "none"}");
            Logger.Info($"{type ?? "none"}");
        }

        return null;
    }
}