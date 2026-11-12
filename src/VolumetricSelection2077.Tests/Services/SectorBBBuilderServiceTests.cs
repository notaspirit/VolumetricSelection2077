using Moq;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.Tests.Services;

public class SectorBBBuilderServiceTests
{
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<IGameFileService> _gameFileService = new();

    private SectorBBBuilderService CreateService()
    {
        return new SectorBBBuilderService(_cacheService.Object, _gameFileService.Object, new ProgressService());
    }

    [Fact]
    public async Task BuildBounds_ReturnsErrorWhenGameFileServiceIsNotInitialized()
    {
        _gameFileService.SetupGet(x => x.IsInitialized).Returns(false);

        var result = await CreateService().BuildBounds(BuildBoundsMode.All);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Game file service is not initialized", result.Message);
    }

    [Fact]
    public async Task BuildBounds_ReturnsErrorWhenCacheServiceIsNotInitialized()
    {
        _gameFileService.SetupGet(x => x.IsInitialized).Returns(true);
        _cacheService.SetupGet(x => x.IsInitialized).Returns(false);

        var result = await CreateService().BuildBounds(BuildBoundsMode.All);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Cache service is not initialized", result.Message);
    }

    [Fact]
    public async Task BuildBounds_ChecksGameFileServiceBeforeCacheService()
    {
        _gameFileService.SetupGet(x => x.IsInitialized).Returns(false);

        await CreateService().BuildBounds(BuildBoundsMode.All);

        _cacheService.VerifyGet(x => x.IsInitialized, Times.Never);
    }

    [Fact]
    public async Task BuildBounds_MissingModded_GetAllEntriesError_ReturnsError()
    {
        _gameFileService.SetupGet(x => x.IsInitialized).Returns(true);
        _cacheService.SetupGet(x => x.IsInitialized).Returns(true);
        var err = new Result<KeyValuePair<string, byte[]>[]> { Status = ResultStatus.Error };
        err.AddMessage("db error");
        _cacheService.Setup(x => x.GetAllEntries(CacheDatabase.ModdedBounds)).Returns(err);

        var result = await CreateService().BuildBounds(BuildBoundsMode.MissingModded);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Failed to get cached modded bounds: db error", result.Message);
    }
}
