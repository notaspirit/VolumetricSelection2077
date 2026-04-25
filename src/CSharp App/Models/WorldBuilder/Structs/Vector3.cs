using System;
using System.Globalization;

namespace VolumetricSelection2077.models.WorldBuilder.Structs;

public class Vector3
{
    public float x;
    public float y;
    public float z;

    public Vector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    
    public Vector3() : this(0, 0, 0) { }
    
    public static implicit operator SharpDX.Vector3(Vector3 value) => new (value.x, value.y, value.z);
    
    public static implicit operator Vector3(SharpDX.Vector3 value) => new (value.X, value.Y, value.Z);
    
    public static implicit operator WolvenKit.RED4.Types.Vector3(Vector3 value) => new ()
    {
        X = value.x,
        Y = value.y,
        Z = value.z,
    };
    
    public static implicit operator Vector3(WolvenKit.RED4.Types.Vector3 value) => new (value.X, value.Y, value.Z);
    
    public override bool Equals(object? obj)
    {
        if (obj is Vector3 other)
        {
            return x == other.x && y == other.y && z == other.z;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(x, y, z);
    }

    public override string ToString() =>
        string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1} Z:{2}", [
            x,
            y,
            z
        ]);
}