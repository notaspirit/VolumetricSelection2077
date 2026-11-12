using SharpDX;
using VolumetricSelection2077.Core.Converters.Simple;
using EulerAngles = VolumetricSelection2077.Core.Models.WorldBuilder.Structs.EulerAngles;
using WBox = WolvenKit.RED4.Types.Box;
using WQuaternion = WolvenKit.RED4.Types.Quaternion;
using WVector3 = WolvenKit.RED4.Types.Vector3;
using WVector4 = WolvenKit.RED4.Types.Vector4;

namespace VolumetricSelection2077.Tests.Converters;

public class SimpleConverterTests
{
    [Theory]
    [InlineData(0, 0f)]
    [InlineData(131072, 1f)]
    [InlineData(-131072, -1f)]
    [InlineData(65536, 0.5f)]
    public void FixedPointVector3Converter_ToFloat_ConvertsFixedPoint(int fixedValue, float expected)
    {
        Assert.Equal(expected, FixedPointVector3Converter.ToFloat(fixedValue));
    }

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(1f, 131072)]
    [InlineData(-1f, -131072)]
    [InlineData(0.5f, 65536)]
    public void FixedPointVector3Converter_FromFloat_ConvertsToFixedPoint(float value, int expected)
    {
        Assert.Equal(expected, FixedPointVector3Converter.FromFloat(value));
    }

    [Fact]
    public void FixedPointVector3Converter_PosBitsToVec3_ConvertsWorldPosition()
    {
        var position = new WolvenKit.RED4.Types.WorldPosition
        {
            X = new WolvenKit.RED4.Types.FixedPoint { Bits = FixedPointVector3Converter.FromFloat(1.5f) },
            Y = new WolvenKit.RED4.Types.FixedPoint { Bits = FixedPointVector3Converter.FromFloat(-2f) },
            Z = new WolvenKit.RED4.Types.FixedPoint { Bits = FixedPointVector3Converter.FromFloat(0.25f) }
        };

        var result = FixedPointVector3Converter.PosBitsToVec3(position);

        Assert.Equal(new Vector3(1.5f, -2f, 0.25f), result);
    }

    [Fact]
    public void WolvenkitToSharpDXConverter_ToSDXV3_FromVector3()
    {
        var input = new WVector3 { X = 1, Y = 2, Z = 3 };

        Assert.Equal(new Vector3(1, 2, 3), input.ToSDXV3());
    }

    [Fact]
    public void WolvenkitToSharpDXConverter_ToSDXV3_FromVector4_DropsW()
    {
        var input = new WVector4 { X = 1, Y = 2, Z = 3, W = 4 };

        Assert.Equal(new Vector3(1, 2, 3), input.ToSDXV3());
    }

    [Fact]
    public void WolvenkitToSharpDXConverter_ToSDXV4_KeepsAllComponents()
    {
        var input = new WVector4 { X = 1, Y = 2, Z = 3, W = 4 };

        Assert.Equal(new Vector4(1, 2, 3, 4), input.ToSDXV4());
    }

    [Fact]
    public void WolvenkitToSharpDXConverter_ToSDXQ_MapsIJKRToXYZW()
    {
        var input = new WQuaternion { I = 1, J = 2, K = 3, R = 4 };

        Assert.Equal(new Quaternion(1, 2, 3, 4), input.ToSDXQ());
    }

    [Fact]
    public void WolvenkitToSharpDXConverter_ToSDXAABB_ConvertsBox()
    {
        var input = new WBox
        {
            Min = new WVector4 { X = -1, Y = -2, Z = -3 },
            Max = new WVector4 { X = 1, Y = 2, Z = 3 }
        };

        var result = input.ToSDXAABB();

        Assert.Equal(new Vector3(-1, -2, -3), result.Minimum);
        Assert.Equal(new Vector3(1, 2, 3), result.Maximum);
    }

    [Fact]
    public void WorldBuilderToSharpDXConverter_ToSDXQ_IdentityEulerAnglesProducesIdentityQuaternion()
    {
        var result = new EulerAngles(0, 0, 0).ToSDXQ();

        Assert.Equal(0f, result.X, 5);
        Assert.Equal(0f, result.Y, 5);
        Assert.Equal(0f, result.Z, 5);
        Assert.Equal(1f, result.W, 5);
    }

    [Fact]
    public void SharpDXToWorldBuilderConverter_EulerAngles_IdentityQuaternionProducesZeroAngles()
    {
        var result = SharpDXToWorldBuilderConverter.EulerAngles(Quaternion.Identity);

        Assert.Equal(0f, result.yaw, 4);
        Assert.Equal(0f, result.pitch, 4);
        Assert.Equal(0f, result.roll, 4);
    }
}
