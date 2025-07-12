using System;
using MathNet.Spatial.Euclidean;
using EulerAngles = VolumetricSelection2077.models.WorldBuilder.Structs.EulerAngles;

namespace VolumetricSelection2077.Converters;

public class SharpDXToWorldBuilder
{
    public static EulerAngles EulerAngles(SharpDX.Quaternion q)
    {
        var quat = new Quaternion(q.W, q.X, q.Y, q.Z);
        var euler = ToEulerAngles(quat, RotationOrder.ZXY);
        
        return new EulerAngles(pitch: (float)euler.x, roll: (float)euler.y, yaw: (float)euler.z);
    }
    
    public enum RotationOrder { XYZ, XZY, YXZ, YZX, ZXY, ZYX }

    public static (double x, double y, double z) ToEulerAngles(Quaternion q, RotationOrder order)
    {
        var m = GetMatrix(q);

        double x, y, z;

        switch (order)
        {
            case RotationOrder.XYZ:
                y = Math.Asin(Clamp(m[0, 2], -1, 1));
                if (Math.Abs(m[0, 2]) < 0.99999)
                {
                    x = Math.Atan2(-m[1, 2], m[2, 2]);
                    z = Math.Atan2(-m[0, 1], m[0, 0]);
                }
                else
                {
                    x = Math.Atan2(m[2, 1], m[1, 1]);
                    z = 0;
                }
                break;

            case RotationOrder.XZY:
                z = Math.Asin(-Clamp(m[0, 1], -1, 1));
                if (Math.Abs(m[0, 1]) < 0.99999)
                {
                    x = Math.Atan2(m[2, 1], m[1, 1]);
                    y = Math.Atan2(m[0, 2], m[0, 0]);
                }
                else
                {
                    x = Math.Atan2(-m[1, 2], m[1, 1]);
                    y = 0;
                }
                break;

            case RotationOrder.YXZ:
                x = Math.Asin(-Clamp(m[1, 2], -1, 1));
                if (Math.Abs(m[1, 2]) < 0.99999)
                {
                    y = Math.Atan2(m[0, 2], m[2, 2]);
                    z = Math.Atan2(m[1, 0], m[1, 1]);
                }
                else
                {
                    y = Math.Atan2(-m[2, 0], m[0, 0]);
                    z = 0;
                }
                break;

            case RotationOrder.YZX:
                z = Math.Asin(Clamp(m[1, 0], -1, 1));
                if (Math.Abs(m[1, 0]) < 0.99999)
                {
                    y = Math.Atan2(-m[2, 0], m[0, 0]);
                    x = Math.Atan2(-m[1, 2], m[1, 1]);
                }
                else
                {
                    y = Math.Atan2(m[0, 2], m[2, 2]);
                    x = 0;
                }
                break;

            case RotationOrder.ZXY:
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
                break;

            case RotationOrder.ZYX:
                y = Math.Asin(-Clamp(m[2, 0], -1, 1));
                if (Math.Abs(m[2, 0]) < 0.99999)
                {
                    z = Math.Atan2(m[1, 0], m[0, 0]);
                    x = Math.Atan2(m[2, 1], m[2, 2]);
                }
                else
                {
                    z = Math.Atan2(-m[0, 1], m[1, 1]);
                    x = 0;
                }
                break;

            default:
                throw new NotImplementedException("Rotation order not supported.");
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