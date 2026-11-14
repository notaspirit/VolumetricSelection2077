using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Core.Services;

/// <summary>
/// Builds accurate bounding boxes for streaming sectors and stores them in the cache.
/// </summary>
public interface ISectorBBBuilderService
{
    Task<Result> BuildBounds(BuildBoundsMode mode);
}
