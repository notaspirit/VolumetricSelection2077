using VolumetricSelection2077.Core.Models;
using WolvenKit.RED4.Archive.CR2W;

namespace VolumetricSelection2077.Core.Parsers;

/// <summary>
/// Parses CR2W files into the VolumetricSelection2077 sector model.
/// </summary>
public interface IVSSectorParser
{
    Result<VSSector> ParseFromCR2W(CR2WFile input);
}
