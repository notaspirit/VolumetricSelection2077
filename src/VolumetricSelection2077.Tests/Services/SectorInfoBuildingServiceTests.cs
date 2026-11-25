using Moq;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Services;
using WolvenKit.Common;

namespace VolumetricSelection2077.Tests.Services;

public class SectorInfoBuildingServiceTests
{
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<IGameFileService> _gameFileService = new();
    private readonly Mock<IArchiveManager> _archiveManager = new();

    private SectorInfoBuildingService CreateService()
    {
        return new SectorInfoBuildingService(_cacheService.Object, _gameFileService.Object, _archiveManager.Object);
    }

    [Fact]
    public async Task BuildBounds_ReturnsErrorWhenCacheServiceIsNotInitialized()
    {
        _cacheService.SetupGet(x => x.IsInitialized).Returns(false);

        var result = await CreateService().BuildAllSectorInfo(BuildBoundsMode.All);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Cache service is not initialized", result.Message);
    }

    [Fact]
    public async Task BuildBounds_MissingModded_GetAllEntriesError_ReturnsError()
    {
        _cacheService.SetupGet(x => x.IsInitialized).Returns(true);
        var err = new Result<KeyValuePair<string, byte[]>[]> { Status = ResultStatus.Error };
        err.AddMessage("db error");
        _cacheService.Setup(x => x.GetAllEntries(CacheDatabase.ModdedBounds)).Returns(err);

        var result = await CreateService().BuildAllSectorInfo(BuildBoundsMode.MissingModded);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Failed to get cached modded bounds: db error", result.Message);
    }
}
