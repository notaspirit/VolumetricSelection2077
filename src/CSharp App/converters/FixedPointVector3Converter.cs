
using System;
using Newtonsoft.Json.Linq;
using Vector3 = SharpDX.Vector3;

namespace VolumetricSelection2077.Converters;
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
    
    public static Vector3 PosBitsToVec3(JToken? jsonObject)
    {
        if (jsonObject == null)
        {
            return new Vector3();
        }
        int bitsX = jsonObject?["x"]?["Bits"]?.Value<int>() ?? 0;
        int bitsY = jsonObject?["y"]?["Bits"]?.Value<int>() ?? 0;
        int bitsZ = jsonObject?["z"]?["Bits"]?.Value<int>() ?? 0;
        
        float x = ToFloat(bitsX);
        float y = ToFloat(bitsY);
        float z = ToFloat(bitsZ);
        
        return new Vector3(x, y, z);
    }

}