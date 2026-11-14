using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Core.Converters.Complex;

/// <summary>
/// Converts AXL removal files into World Builder element trees.
/// </summary>
public interface IAxlRemovalToWorldBuilderConverter
{
    Result<WBConversionResult> Convert(AxlRemovalFile axlFile, string rootName, WBConverterConfig config);
}
