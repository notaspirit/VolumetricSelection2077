using System.Collections.Generic;
using MessagePack;
using SharpDX;

namespace VolumetricSelection2077.Models;

[MessagePackObject]
public class SectorBBDump
{
    [Key(0)]
    public string GameVersion { get; set; }
    [Key(1)]
    public string VS2077Version { get; set; }
    [Key(2)]
    public List<KeyValuePair<string, BoundingBox>> Sectors { get; set; }
}