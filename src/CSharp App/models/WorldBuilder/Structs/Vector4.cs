using System;

namespace VolumetricSelection2077.models.WorldBuilder.Structs;

public struct Vector4
{
    public float x;
    public float y;
    public float z;
    public float w;

    public Vector4(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }
    
    public static implicit operator SharpDX.Vector4(Vector4 value) => new (value.x, value.y, value.z, value.w);
    
    public static implicit operator Vector4(SharpDX.Vector4 value) => new (value.X, value.Y, value.Z, value.W);
    
    public override bool Equals(object? obj)
    {
        if (obj is Vector4 other)
        {
            return x == other.x && y == other.y && z == other.z && w == other.w;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(x, y, z, w);
    }
}