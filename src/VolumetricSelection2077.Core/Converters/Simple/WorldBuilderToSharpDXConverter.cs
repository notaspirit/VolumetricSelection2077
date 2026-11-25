using System;
using SharpDX;
using VolumetricSelection2077.Core.Models.WorldBuilder.Structs;

namespace VolumetricSelection2077.Core.Converters.Simple;

public static class WorldBuilderToSharpDXConverter
{
    public static Quaternion ToSDXQ(this EulerAngles eulerAngles)
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