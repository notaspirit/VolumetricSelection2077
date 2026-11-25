using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Core.Services;

/// <summary>
/// Builds accurate bounding boxes for streaming sectors and stores them in the cache.
/// </summary>
public interface ISectorInfoBuildingService
{
    Task<Result> BuildAllSectorInfo(BuildBoundsMode mode, IProgressService? progressService = null, bool verbose = false);
    Task<Result> BuildSectorInfo(string sectorPath, CacheDatabase? database, bool verbose = false);
}
