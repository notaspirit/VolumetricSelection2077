using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Core.Services;

/// <summary>
/// Provides read and write access to the VolumetricSelection2077 cache databases.
/// </summary>
public interface ICacheService
{
    bool IsInitialized { get; }

    bool ClearCacheOnExit { get; set; }

    Result InitializationResult { get; }

    Result LastWriteResult { get; }

    Result Initialize();

    Task<Result> Dispose();

    Result StartListening();

    Result StopListening();

    Result ResizeEnvironment(bool bypass = false);

    Result ClearDatabase(CacheDatabase database, bool bypass = false);

    Result Move(string fromPath, string toPath);

    Result<CacheStats> GetStats();

    Result<CacheDatabaseSample> GetSample(int sampleSize);

    Result<CacheDatabaseMetadata> GetMetadata();

    Result SetMetaDataVanillaBoundsStatus(bool newValue);

    Result<SerializedSectorBoundingBoxes> SerializeSectorBoundingBoxesToStruct();

    Result<byte[]> SerializeSectorBoundingBoxesToMemory();

    Result SerializeSectorBoundingBoxesToFile(string filePath);

    Result LoadSectorBBFromFile(string path);

    Result<byte[]> GetEntry(ReadCacheRequest request);

    Result<KeyValuePair<string, byte[]>[]> GetAllEntries(CacheDatabase database);

    Result WriteSingleEntry(WriteCacheRequest request);

    void WriteEntry(string path, VSSector sector, CacheDatabase database);

    void WriteEntry(string path, VSMesh mesh, CacheDatabase database);

    void WriteEntry(WriteCacheRequest request);
}
