using MessagePack;
using SharpDX;

namespace VolumetricSelection2077.Core.Models;

[MessagePackObject]
public class SerializedSectorBoundingBoxes
{
    [Key(0)]
    public string GameVersion { get; set; }
    [Key(1)]
    public uint DatabaseVersion { get; set; }
    [Key(2)]
    public List<KeyValuePair<string, BoundingBox>> SectorBoundingBoxes { get; set; }
}