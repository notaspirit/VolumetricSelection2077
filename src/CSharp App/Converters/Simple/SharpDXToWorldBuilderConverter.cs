using System;
using MathNet.Spatial.Euclidean;
using EulerAngles = VolumetricSelection2077.models.WorldBuilder.Structs.EulerAngles;

namespace VolumetricSelection2077.Converters.Simple;

public class SharpDXToWorldBuilderConverter
{
    public static EulerAngles EulerAngles(SharpDX.Quaternion q)
    {
        var quat = new Quaternion(q.W, q.X, q.Y, q.Z);
        var euler = ToEulerAngles(quat);
        
        return new EulerAngles(pitch: (float)euler.x, roll: (float)euler.y, yaw: (float)euler.z);
    }

    private static (double x, double y, double z) ToEulerAngles(Quaternion q)
    {
        var m = GetMatrix(q);

        double x, y, z;

        x = Math.Asin(Clamp(m[2, 1], -1, 1));
        if (Math.Abs(m[2, 1]) < 0.99999)
        {
            z = Math.Atan2(-m[0, 1], m[1, 1]);
            y = Math.Atan2(-m[2, 0], m[2, 2]);
        }
        else
        {
            z = Math.Atan2(m[1, 0], m[0, 0]);
            y = 0;
        }

        return (RadiansToDegrees(x), RadiansToDegrees(y), RadiansToDegrees(z));
    }

    private static double Clamp(double val, double min, double max)
        => Math.Max(min, Math.Min(max, val));

    private static double RadiansToDegrees(double radians)
        => radians * (180.0 / Math.PI);

    private static double[,] GetMatrix(Quaternion q)
    {
        double w = q.Real;
        double x = q.ImagX;
        double y = q.ImagY;
        double z = q.ImagZ;

        double[,] m = new double[3,3];

        m[0, 0] = 1 - 2 * (y * y + z * z);
        m[0, 1] = 2 * (x * y - z * w);
        m[0, 2] = 2 * (x * z + y * w);

        m[1, 0] = 2 * (x * y + z * w);
        m[1, 1] = 1 - 2 * (x * x + z * z);
        m[1, 2] = 2 * (y * z - x * w);

        m[2, 0] = 2 * (x * z - y * w);
        m[2, 1] = 2 * (y * z + x * w);
        m[2, 2] = 1 - 2 * (x * x + y * y);
        return m;
    }
}