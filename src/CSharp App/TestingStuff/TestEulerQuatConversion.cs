using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class TestEulerQuatConversion
{
    private class Element
    {
        public float yaw;
        public float pitch;
        public float roll;
        public Quaternion quaternion;

        public Element(float yaw, float pitch, float roll, Quaternion quaternion)
        {
            this.yaw = yaw;
            this.pitch = pitch;
            this.roll = roll;
            this.quaternion = quaternion;
        }
    }

    private static List<Element> elements = [
        new Element(0, 0, 0, new Quaternion(real: 1, imagX: 0, imagY: 0, imagZ: 0)),
        new Element(0, 0, 90, new Quaternion(real: 0.70710676908493, imagX: 0, imagY: 0.70710676908493, imagZ: 0)),
        new Element(0, 90, 0, new Quaternion(real: 0.70710676908493, imagX: 0.70710676908493, imagY: 0, imagZ: 0)),
        new Element(90, 0, 0, new Quaternion(real: 0.70710676908493, imagX: 0, imagY: 0, imagZ: 0.70710676908493)),
        new Element(45, 45, 45, new Quaternion(real: 0.73253774642944, imagX: 0.19134171307087, imagY: 0.46193981170654, imagZ: 0.46193981170654)),
        new Element(0, 0, 180, new Quaternion(real: -8.7422790784331e-08, imagX: -0, imagY: 1, imagZ: 0)),
        new Element(0, 180, 0, new Quaternion(real: -8.7422776573476e-08, imagX: 1, imagY: 0, imagZ: 0)),
        new Element(180, 0, 0, new Quaternion(real: -8.7422776573476e-08, imagX: -0, imagY: 0, imagZ: 1)),
        new Element(0, 90, 90, new Quaternion(real: 0.5, imagX: 0.5, imagY: 0.5, imagZ: 0.5)),
        new Element(90, 90, 0, new Quaternion(real: 0.5, imagX: 0.5, imagY: 0.5, imagZ: 0.5)),
        new Element(0, 0, -90, new Quaternion(real: 0.70710676908493, imagX: 0, imagY: -0.70710676908493, imagZ: 0)),
        new Element(0, -90, 0, new Quaternion(real: 0.70710676908493, imagX: -0.70710676908493, imagY: 0, imagZ: 0)),
        new Element(-90, 0, 0, new Quaternion(real: 0.70710676908493, imagX: 0, imagY: 0, imagZ: -0.70710676908493)),
        new Element(90, 45, 270, new Quaternion(real: -0.65328150987625, imagX: -0.65328145027161, imagY: 0.27059799432755, imagZ: -0.27059811353683)),
        new Element(90, 60, 30, new Quaternion(real: 0.5, imagX: 0.18301270902157, imagY: 0.5, imagZ: 0.68301272392273)),
        new Element(60, 240, 120, new Quaternion(real: -0.59150642156601, imagX: 0.59150642156601, imagY: -0.15849375724792, imagZ: 0.52451890707016)),
        new Element(359, 359, 359, new Quaternion(real: -0.99988651275635, imagX: 0.008802124299109, imagY: 0.0086498213931918, imagZ: 0.0086498213931918)),
        new Element(180, 180, 0, new Quaternion(real: 7.6427427076918e-15, imagX: -8.7422776573476e-08, imagY: 1, imagZ: -8.7422776573476e-08)),
        new Element(0, 180, 180, new Quaternion(real: 7.6427427076918e-15, imagX: -8.7422790784331e-08, imagY: -8.7422783678903e-08, imagZ: 1)),
        new Element(180, 0, 180, new Quaternion(real: 7.6427427076918e-15, imagX: -1, imagY: -8.7422783678903e-08, imagZ: -8.7422790784331e-08))
    ];

    private const float TOLERANCE = 0.1f;
    
    public static void Run()
    {
        Dictionary<RotationOrder, int> rotOrderFreqency =  new Dictionary<RotationOrder, int>();
         
        foreach (var element in elements)
        {

            Logger.Debug($"Comparing element at {elements.IndexOf(element)}");
            Logger.Info($"Original Euler is pitch: {element.pitch}, yaw:  {element.yaw}, roll: {element.roll}");

            foreach (RotationOrder order in Enum.GetValues(typeof(RotationOrder)))
            {
                var convertedEuler = ToEulerAngles(element.quaternion, order);
                Logger.Info($"Converted {order}: converted euler: {convertedEuler}");
                if (Math.Abs(convertedEuler.x - element.yaw) < TOLERANCE &&
                    Math.Abs(convertedEuler.y - element.pitch) < TOLERANCE &&
                    Math.Abs(convertedEuler.z - element.roll) < TOLERANCE)
                {
                    Logger.Info("Found match!");
                    try
                    {
                        rotOrderFreqency[order]++;
                    }
                    catch (Exception)
                    {
                        rotOrderFreqency[order] = 1;
                    }

                }
            }
        }

        var stringFreq = rotOrderFreqency.Aggregate("", (current, kvp) => current + $"{kvp.Key}: {kvp.Value}\n");
        Logger.Info($"Rotation order frequencies are \n {stringFreq}");
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