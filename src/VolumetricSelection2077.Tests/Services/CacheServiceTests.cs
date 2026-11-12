using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.Tests.Services;

public class CacheServiceTests : IAsyncLifetime
{
    private const string GameVersion = "test-version";
    private string _cacheDirectory = null!;
    private CacheService _cacheService = null!;

    public Task InitializeAsync()
    {
        _cacheDirectory = Path.Combine(Path.GetTempPath(), "vs2077-tests", "cache", Guid.NewGuid().ToString("N"));
        _cacheService = new CacheService(_cacheDirectory, GameVersion);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _cacheService.Dispose();
        if (Directory.Exists(_cacheDirectory))
            Directory.Delete(_cacheDirectory, true);
    }

    [Fact]
    public void Constructor_InitializesCache()
    {
        Assert.True(_cacheService.IsInitialized);
        Assert.Equal(ResultStatus.Ok, _cacheService.InitializationResult.Status);
        Assert.True(Directory.Exists(_cacheDirectory));
        Assert.True(File.Exists(Path.Combine(_cacheDirectory, "metadata.json")));
    }

    [Fact]
    public void Initialize_WhenAlreadyInitialized_ReturnsOk()
    {
        var result = _cacheService.Initialize();

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.True(_cacheService.IsInitialized);
    }

    [Fact]
    public void GetMetadata_ReturnsCacheMetadata()
    {
        var result = _cacheService.GetMetadata();

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal(GameVersion, result.Data.GameVersion);
        Assert.Equal(1U, result.Data.DatabaseVersion);
        Assert.False(result.Data.AreVanillaSectorBBsBuild);
    }

    [Fact]
    public void SetMetaDataVanillaBoundsStatus_PersistsValue()
    {
        var result = _cacheService.SetMetaDataVanillaBoundsStatus(true);

        Assert.Equal(ResultStatus.Ok, result.Status);
        var metadata = _cacheService.GetMetadata();
        Assert.NotNull(metadata.Data);
        Assert.True(metadata.Data.AreVanillaSectorBBsBuild);
    }

    [Fact]
    public void WriteSingleEntry_GetEntry_RoundTripsData()
    {
        var data = new byte[] { 1, 2, 3, 4 };

        var writeResult = _cacheService.WriteSingleEntry(new WriteCacheRequest("path", data));
        var readResult = _cacheService.GetEntry(new ReadCacheRequest("path"));

        Assert.Equal(ResultStatus.Ok, writeResult.Status);
        Assert.Equal(ResultStatus.Ok, readResult.Status);
        Assert.Equal(data, readResult.Data);
    }

    [Fact]
    public void GetEntry_MissingKey_ReturnsOkWithoutData()
    {
        var result = _cacheService.GetEntry(new ReadCacheRequest("missing"));

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Null(result.Data);
    }

    [Fact]
    public void GetEntry_AllDatabases_FindsEntryInModdedDatabase()
    {
        _cacheService.WriteSingleEntry(new WriteCacheRequest("path", [7, 8], CacheDatabase.Modded));

        var result = _cacheService.GetEntry(new ReadCacheRequest("path", CacheDatabase.AllDatabases));

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(new byte[] { 7, 8 }, result.Data);
    }

    [Fact]
    public void GetEntry_UnsupportedDatabase_ReturnsError()
    {
        var result = _cacheService.GetEntry(new ReadCacheRequest("path", (CacheDatabase)999));

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Unsupported database", result.Message);
    }

    [Fact]
    public void GetAllEntries_ReturnsWrittenEntries()
    {
        _cacheService.WriteSingleEntry(new WriteCacheRequest("first", [1]));
        _cacheService.WriteSingleEntry(new WriteCacheRequest("second", [2]));

        var result = _cacheService.GetAllEntries(CacheDatabase.Vanilla);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Length);
        Assert.Contains(result.Data, entry => entry.Key == "first" && entry.Value.SequenceEqual(new byte[] { 1 }));
        Assert.Contains(result.Data, entry => entry.Key == "second" && entry.Value.SequenceEqual(new byte[] { 2 }));
    }

    [Fact]
    public void GetAllEntries_AllBounds_CombinesVanillaAndModdedBounds()
    {
        _cacheService.WriteSingleEntry(new WriteCacheRequest("vanilla", [1], CacheDatabase.VanillaBounds));
        _cacheService.WriteSingleEntry(new WriteCacheRequest("modded", [2], CacheDatabase.ModdedBounds));

        var result = _cacheService.GetAllEntries(CacheDatabase.AllBounds);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Length);
        Assert.Contains(result.Data, entry => entry.Key == "vanilla");
        Assert.Contains(result.Data, entry => entry.Key == "modded");
    }

    [Fact]
    public void GetAllEntries_AllDatabases_IsNotSupported()
    {
        var result = _cacheService.GetAllEntries(CacheDatabase.AllDatabases);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("not supported", result.Message);
    }

    [Fact]
    public async Task Dispose_ThenOperations_ReturnErrors()
    {
        var disposeResult = await _cacheService.Dispose();

        Assert.Equal(ResultStatus.Ok, disposeResult.Status);
        Assert.False(_cacheService.IsInitialized);
        Assert.Equal(ResultStatus.Error, _cacheService.GetEntry(new ReadCacheRequest("path")).Status);
        Assert.Equal(ResultStatus.Error, _cacheService.WriteSingleEntry(new WriteCacheRequest("path", [1])).Status);
        Assert.Equal(ResultStatus.Error, _cacheService.GetAllEntries(CacheDatabase.Vanilla).Status);
    }

    [Fact]
    public void StartListening_AfterInitialization_ReturnsOk()
    {
        var result = _cacheService.StartListening();

        Assert.Equal(ResultStatus.Ok, result.Status);
        _cacheService.StopListening();
    }
}
