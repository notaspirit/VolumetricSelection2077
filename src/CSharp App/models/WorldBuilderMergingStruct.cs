using System.Collections.Generic;
using VolumetricSelection2077.Models.WorldBuilder.Editor;

namespace VolumetricSelection2077.Models;

public struct WorldBuilderMergingStruct
{
    public SpawnableElement SpawnableElement { get; set; }
    public ulong Hash { get; set; }
    public string ParentName { get; set; }
    
    
    public bool Equals(WorldBuilderMergingStruct other)
    {
        return this.Hash == other.Hash;
    }

    public override bool Equals(object obj)
    {
        return obj is WorldBuilderMergingStruct other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Hash.GetHashCode();
    }

    public static bool operator ==(WorldBuilderMergingStruct left, WorldBuilderMergingStruct right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WorldBuilderMergingStruct left, WorldBuilderMergingStruct right)
    {
        return !(left == right);
    }
}