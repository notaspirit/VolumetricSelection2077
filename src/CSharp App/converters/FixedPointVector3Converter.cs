
using System;
using Newtonsoft.Json.Linq;
using Vector3 = SharpDX.Vector3;

namespace VolumetricSelection2077.Converters;
public class FixedPointVector3Converter
{
    // Assuming 32-bit fixed-point with 16.16 format
    private const int FractionalBits = 16;

    private static float ConvertFixedPoint(int value)
    {
        int integerPart = value >> FractionalBits;
        int fractionalPart = value & ((1 << FractionalBits) - 1);
        
        float fractionalDecimal = (float)(fractionalPart / Math.Pow(2, FractionalBits));

        return integerPart + fractionalDecimal;
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
        
        float x = ConvertFixedPoint(bitsX);
        float y = ConvertFixedPoint(bitsY);
        float z = ConvertFixedPoint(bitsZ);
        
        return new Vector3(x, y, z);
    }

}