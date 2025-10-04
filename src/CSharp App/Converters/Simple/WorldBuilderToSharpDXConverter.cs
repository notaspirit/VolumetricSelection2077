using System;
using SharpDX;
using VolumetricSelection2077.models.WorldBuilder.Structs;

namespace VolumetricSelection2077.Converters.Simple;

public class WorldBuilderToSharpDXConverter
{
    public static Quaternion Quaternion(EulerAngles eulerAngles)
    {
        double halfPitch = eulerAngles.roll * Math.PI / 180.0 * 0.5;
        double halfYaw = eulerAngles.yaw * Math.PI / 180.0 * 0.5;
        double halfRoll = eulerAngles.pitch * Math.PI / 180.0 * 0.5;
    
        float sinPitch = (float)Math.Sin(halfPitch);
        float cosPitch = (float)Math.Cos(halfPitch);
        float sinYaw = (float)Math.Sin(halfYaw);
        float cosYaw = (float)Math.Cos(halfYaw);
        float sinRoll = (float)Math.Sin(halfRoll);
        float cosRoll = (float)Math.Cos(halfRoll);
    
        float x = sinPitch * cosYaw * cosRoll - cosPitch * sinYaw * sinRoll;
        float y = cosPitch * sinYaw * cosRoll + sinPitch * cosYaw * sinRoll;
        float z = cosPitch * cosYaw * sinRoll - sinPitch * sinYaw * cosRoll;
        float w = cosPitch * cosYaw * cosRoll + sinPitch * sinYaw * sinRoll;

        return new Quaternion(x, y, z, w);
    }
}