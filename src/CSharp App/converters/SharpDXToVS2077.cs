using System;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.models.WorldBuilder.Structs;
using Quaternion = SharpDX.Quaternion;

namespace VolumetricSelection2077.Converters;

public class SharpDXToVS2077
{
    public static EulerAngles EulerAngles(Quaternion q)
    { 
        EulerAngles angles = new EulerAngles();
    
        double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        angles.pitch = (float)(Math.Atan2(sinr_cosp, cosr_cosp) * 180.0 / Math.PI);
    
        double sinp = 2 * (q.W * q.Y - q.Z * q.X);
        if (Math.Abs(sinp) >= 1)
            angles.roll = (float)(Math.CopySign(Math.PI / 2, sinp) * 180.0 / Math.PI);
        else
            angles.roll = (float)(Math.Asin(sinp) * 180.0 / Math.PI);
    
        double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        angles.yaw = (float)(Math.Atan2(siny_cosp, cosy_cosp) * 180.0 / Math.PI);
    
        return angles;
    }
}