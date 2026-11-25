using MessagePack;
using SharpDX;

namespace VolumetricSelection2077.Core.Models;

[MessagePackObject]
public struct VSSectorInfo
{
    [Key(0)] 
    public BoundingBox BoundingBox { get; set; }
}