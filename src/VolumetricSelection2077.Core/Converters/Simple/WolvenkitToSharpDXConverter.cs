using SharpDX;

namespace VolumetricSelection2077.Core.Converters.Simple;

public static class WolvenkitToSharpDXConverter
{
    public static Vector3 ToSDXV3(this WolvenKit.RED4.Types.Vector3 vector)
    {
        return new Vector3(vector.X, vector.Y, vector.Z);
    }
    
    public static Vector3 ToSDXV3(this WolvenKit.RED4.Types.Vector4 vector)
    {
        return new Vector3(vector.X, vector.Y, vector.Z);
    }
    
    public static Vector4 ToSDXV4(this WolvenKit.RED4.Types.Vector4 vector)
    {
        return new Vector4(vector.X, vector.Y, vector.Z,  vector.W);
    }
    public static Quaternion ToSDXQ(this WolvenKit.RED4.Types.Quaternion quaternion)
    {
        return new Quaternion(quaternion.I, quaternion.J, quaternion.K, quaternion.R);
    }

    public static BoundingBox ToSDXAABB(this WolvenKit.RED4.Types.Box box)
    {
        return new BoundingBox()
        {
            Minimum = new Vector3(box.Min.X, box.Min.Y, box.Min.Z),
            Maximum = new Vector3(box.Max.X, box.Max.Y, box.Max.Z),
        };
    }
}