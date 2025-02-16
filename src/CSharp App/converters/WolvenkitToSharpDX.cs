using SharpDX;

namespace VolumetricSelection2077.Converters;

public class WolvenkitToSharpDX
{
    public static Vector3 Vector3(WolvenKit.RED4.Types.Vector3 vector)
    {
        return new Vector3(vector.X, vector.Y, vector.Z);
    }
    
    public static Vector3 Vector3(WolvenKit.RED4.Types.Vector4 vector)
    {
        return new Vector3(vector.X, vector.Y, vector.Z);
    }
    public static Quaternion Quaternion(WolvenKit.RED4.Types.Quaternion quaternion)
    {
        return new Quaternion(quaternion.I, quaternion.J, quaternion.K, quaternion.R);
    }
}