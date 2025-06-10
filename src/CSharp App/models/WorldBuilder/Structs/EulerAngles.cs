using System;
using VolumetricSelection2077.Converters;

namespace VolumetricSelection2077.models.WorldBuilder.Structs;

public struct EulerAngles
{
    public float yaw  { get; set; }
    public float pitch { get; set; }
    public float roll { get; set; }

    public EulerAngles(float  yaw, float pitch, float roll)
    {
        this.yaw   = yaw;
        this.pitch = pitch;
        this.roll  = roll;
    }
    
    public static implicit operator SharpDX.Quaternion(EulerAngles value) => VS2077ToSharpDX.Quaternion(value);
    
    public static implicit operator EulerAngles(SharpDX.Quaternion value) => SharpDXToVS2077.EulerAngles(value);
    
    public override bool Equals(object? obj)
    {
        if (obj is EulerAngles other)
        {
            return yaw == other.yaw && pitch == other.pitch && roll == other.roll;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(yaw, pitch, roll);
    }
}