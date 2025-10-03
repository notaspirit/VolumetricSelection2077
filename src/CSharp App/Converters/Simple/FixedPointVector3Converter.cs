using WolvenKit.RED4.Types;
using Vector3 = SharpDX.Vector3;

namespace VolumetricSelection2077.Converters.Simple;
public class FixedPointVector3Converter
{
    private const float Fixed_1 = 131072.0f;  // 131072.0f (2^17)
    
    public static float ToFloat(int fixedValue)
    {
        return fixedValue / Fixed_1;
    }
    
    public static int FromFloat(float floatValue)
    {
        return (int)(floatValue * Fixed_1);
    }
    public static Vector3 PosBitsToVec3(WorldPosition worldPos)
    {
        int bitsX = worldPos.X.Bits;
        int bitsY = worldPos.Y.Bits;
        int bitsZ = worldPos.Z.Bits;
        
        float x = ToFloat(bitsX);
        float y = ToFloat(bitsY);
        float z = ToFloat(bitsZ);
        
        return new Vector3(x, y, z);
    }
    

}