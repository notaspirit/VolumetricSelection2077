using System;
using VolumetricSelection2077.Converters.Simple;
using WolvenKit.RED4.Types;

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
    
    public static implicit operator SharpDX.Quaternion(EulerAngles value) => WorldBuilderToSharpDXConverter.Quaternion(value);
    
    public static implicit operator EulerAngles(SharpDX.Quaternion value) => SharpDXToWorldBuilderConverter.EulerAngles(value);
    
    public static implicit operator WolvenKit.RED4.Types.Quaternion(EulerAngles value)
    {
        var sharpdx = WorldBuilderToSharpDXConverter.Quaternion(value);
        return new Quaternion
        {
            I = sharpdx.X,
            J = sharpdx.Y,
            K = sharpdx.Z,
            R = sharpdx.W,
        };
    }

    public static implicit operator EulerAngles(WolvenKit.RED4.Types.Quaternion value)
    {
        var sharpdx = new SharpDX.Quaternion(value.I, value.J, value.K, value.R);
        return SharpDXToWorldBuilderConverter.EulerAngles(sharpdx);
    }

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