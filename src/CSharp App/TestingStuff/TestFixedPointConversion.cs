using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using VolumetricSelection2077.Converters;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class TestFixedPointConversion
{
    public static void Run()
    {
        Logger.Info("Running TestFixedPointConversion Test...");
        Dictionary<int, float> testValues = new();
        
        testValues.Add(-155316192, -1184.96851f);
        testValues.Add(-49971412, -381.251617f);
        testValues.Add(1636308, 12.4840393f);
        
        testValues.Add(-153644352, -1172.21338f);
        testValues.Add(-53292268, -406.587738f);
        testValues.Add(1473187, 11.2395248f);
        
        testValues.Add(-152541328, -1163.79797f);
        testValues.Add(-52228096, -398.46875f);
        testValues.Add(1491032, 11.3756714f);

        foreach (var bits in testValues)
        {
            float prococessedValue = FixedPointVector3Converter.ToFloat(bits.Key);
            if (prococessedValue != bits.Value)
            {
                Logger.Error($"{bits.Key}: {bits.Value} resulted in unexpected: {prococessedValue}");
            }
            else
            {
                Logger.Success($"{bits.Key}: {bits.Value} resulted in expected: {prococessedValue}");
            }
        }
    }
}