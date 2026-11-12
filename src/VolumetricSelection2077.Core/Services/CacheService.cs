using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using LightningDB;
using MessagePack;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.VisualBasic.FileIO;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;
using BoundingBox = SharpDX.BoundingBox;
using SearchOption = System.IO.SearchOption;

namespace VolumetricSelection2077.Core.Services;

/// <summary>
/// This part of the class is responsible for managing the instance, lightingdb and debug / metadata.
/// </summary>
public partial class CacheService
{
    private string _cacheDirectory;
    private string _gameVersion;

    private const uint CurrentCacheVersion = 1;

    private LightningEnvironment _env;

    private static readonly long Kb = 1024;
    private static readonly long Gb = Kb * Kb * Kb;
    private static readonly int BatchDelay = 1;
    private static readonly int MaxReaders = 512;
    private static readonly long MapSize = Gb * 100;
    private static readonly ulong EstimatedBoundsEntrySizeInBytes = 116;
    static ConcurrentQueue<WriteCacheRequest> _requestWriteQueue = new();

    private readonly object _lock = new object();
    private readonly object _saveKnownBadResourcesLock = new();

    private LightningDatabase _vanillaDatabase;
    private LightningDatabase _moddedDatabase;
    private LightningDatabase _vanillaBoundsDatabase;
    private LightningDatabase _moddedBoundsDatabase;

    private readonly ConcurrentDictionary<string, byte> _knownBadResources = new();
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;
    private static bool IsProcessing { get; set; } = false;

    public bool ClearCacheOnExit { get; set; }

    /// <summary>
    /// Result of the initialization performed by the constructor.
    /// </summary>
    public Result InitializationResult { get; private set; } = new(ResultStatus.Ok);

    /// <summary>
    /// Result of the most recently completed write queue processing run.
    /// </summary>
    public Result LastWriteResult { get; private set; } = new(ResultStatus.Ok);

    public CacheService(string cacheDirectory,
        string gameVersion,
        bool clearCacheOnExit = false)
    {
        _cacheDirectory = cacheDirectory;
        _gameVersion = gameVersion;
        ClearCacheOnExit = clearCacheOnExit;
        InitializationResult = Initialize();
    }

    /// <summary>
    /// Initializes the cache service.
    /// </summary>
    /// <returns>A result indicating whether initialization succeeded.</returns>
    public Result Initialize()
    {
        try
        {
            Directory.CreateDirectory(_cacheDirectory);

            if (_isInitialized) return new Result(ResultStatus.Ok);

            _env = new LightningEnvironment(_cacheDirectory)
            {
                MaxDatabases = 4,
                MapSize = MapSize,
                MaxReaders = MaxReaders
            };
            _env.Open();

            var tx = _env.BeginTransaction();
            _moddedDatabase = tx.OpenDatabase(CacheDatabase.Modded.ToString(), new DatabaseConfiguration() { Flags = DatabaseOpenFlags.Create });
            _vanillaDatabase = tx.OpenDatabase(CacheDatabase.Vanilla.ToString(), new DatabaseConfiguration() { Flags = DatabaseOpenFlags.Create });
            _moddedBoundsDatabase = tx.OpenDatabase(CacheDatabase.ModdedBounds.ToString(), new DatabaseConfiguration() { Flags = DatabaseOpenFlags.Create });
            _vanillaBoundsDatabase = tx.OpenDatabase(CacheDatabase.VanillaBounds.ToString(), new DatabaseConfiguration() { Flags = DatabaseOpenFlags.Create });
            tx.Commit();

            var result = new Result(ResultStatus.Ok);

            var loadKnownBadResourcesResult = LoadKnownBadResources();
            if (loadKnownBadResourcesResult.Status != ResultStatus.Ok)
            {
                result.Status = ResultStatus.Warning;
                result.AddMessage(loadKnownBadResourcesResult.Message);
            }

            var validationResult = ValidateCache();
            if (validationResult.Status == ResultStatus.Error)
            {
                _env.Dispose();
                return validationResult;
            }

            if (validationResult.Data != true)
            {
                var resetResult = ResetStaleCache();
                if (resetResult.Status == ResultStatus.Error)
                {
                    _env.Dispose();
                    return resetResult;
                }

                result.Status = resetResult.Status;
                result.AddMessage(resetResult.Message);
            }

            _isInitialized = true;
            return result;
        }
        catch (Exception e)
        {
            try
            {
                _env?.Dispose();
            }
            catch
            {
                // Ignore failures while cleaning up a partially initialized environment.
            }

            return new Result(ResultStatus.Error, $"Failed to initialize cache service: {e.Message}");
        }
    }

    /// <summary>
    /// Resets the cache after detecting that it is stale.
    /// </summary>
    /// <returns>A result indicating whether the reset succeeded.</returns>
    private Result ResetStaleCache()
    {
        var result = new Result(ResultStatus.Warning, "Cache is stale, resetting database.");

        var clearMetaDataResult = ClearMetaData();
        if (clearMetaDataResult.Status == ResultStatus.Error)
            return clearMetaDataResult;

        var clearKnownBadResourcesResult = ClearKnownBadResources();
        if (clearKnownBadResourcesResult.Status == ResultStatus.Error)
            return clearKnownBadResourcesResult;

        var clearDatabaseResult = ClearDatabase(CacheDatabase.AllDatabases, true);
        if (clearDatabaseResult.Status == ResultStatus.Error)
            return clearDatabaseResult;

        return result;
    }

    private Result<bool> ValidateCache()
    {
        var metadataResult = GetMetadata();
        if (metadataResult.Status == ResultStatus.Error || metadataResult.Data == null)
        {
            var errorResult = new Result<bool> { Status = ResultStatus.Error };
            errorResult.AddMessage($"Failed to validate cache: {metadataResult.Message}");
            return errorResult;
        }

        var metaData = metadataResult.Data;
        var result = new Result<bool> { Status = ResultStatus.Ok };
        result.Data = metaData.DatabaseVersion == CurrentCacheVersion && _gameVersion == metaData.GameVersion;
        return result;
    }

    /// <summary>
    /// Disposes of the cache service
    /// </summary>
    /// <returns>A result indicating whether disposing succeeded.</returns>
    public Task<Result> Dispose()
    {
        return Task.Run<Result>(() =>
        {
            try
            {
                if (!_isInitialized) return new Result(ResultStatus.Ok);
                if (_env == null) return new Result(ResultStatus.Ok);
                _isInitialized = false;
                if (IsProcessing)
                {
                    IsProcessing = false;
                    Task.Delay(BatchDelay * 10).Wait();
                }
                _env.Dispose();
                return new Result(ResultStatus.Ok);
            }
            catch (Exception e)
            {
                return new Result(ResultStatus.Error, $"Failed to dispose cache service: {e.Message}");
            }
        });
    }

    /// <summary>
    /// Starts listening to read requests
    /// </summary>
    /// <returns>A result indicating whether listening could be started.</returns>
    public Result StartListening()
    {
        if (!_isInitialized)
            return new Result(ResultStatus.Error, "Cache service must be initialized before calling StartListening.");
        if (IsProcessing) return new Result(ResultStatus.Ok);
        IsProcessing = true;
        _ = Task.Run(async () => LastWriteResult = await ProcessWriteQueue());
        return new Result(ResultStatus.Ok);
    }

    /// <summary>
    /// Stops listening to read requests
    /// </summary>
    /// <returns>A result indicating whether the known bad resources were saved successfully.</returns>
    public Result StopListening()
    {
        IsProcessing = false;
        return SaveKnownBadResources();
    }

    /// <summary>
    /// Resizes the environment
    /// </summary>
    /// <param name="bypass">bypass initialization check => callers responsibility to ensure env is ready</param>
    /// <returns>A result indicating whether resizing succeeded.</returns>
    public Result ResizeEnvironment(bool bypass = false)
    {
        if (!_isInitialized && !bypass)
            return new Result(ResultStatus.Error, "Cache service must be initialized before calling ResizeEnvironment");

        try
        {
            _isInitialized = false;

            string tempCacheDir = Path.Combine(Directory.GetParent(_cacheDirectory)?.FullName, $"temp_cache_{Guid.NewGuid().ToString()}");
            Directory.CreateDirectory(tempCacheDir);

            var metaDataFilePath = Path.Combine(_cacheDirectory, "metadata.json");

            if (File.Exists(metaDataFilePath))
            {
                File.Copy(metaDataFilePath, Path.Combine(tempCacheDir, "metadata.json"));
            }

            var tempEnv = new LightningEnvironment(tempCacheDir)
            {
                MaxDatabases = 4,
                MapSize = MapSize,
                MaxReaders = MaxReaders
            };
            tempEnv.Open();
            var databases = new [] { CacheDatabase.Vanilla.ToString(), CacheDatabase.Modded.ToString(), CacheDatabase.VanillaBounds.ToString(), CacheDatabase.ModdedBounds.ToString() };

            foreach (var database in databases)
            {
                using (var createTxn = tempEnv.BeginTransaction())
                {
                    using (var db = createTxn.OpenDatabase(database, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
                    {
                        // Just create the database
                    }
                    createTxn.Commit();
                }

            }
            var srcTxn = _env.BeginTransaction();
            var dstTxn = tempEnv.BeginTransaction();
            foreach (var dbName in databases)
            {
                var srcDb = srcTxn.OpenDatabase(dbName);
                var dstDb = dstTxn.OpenDatabase(dbName, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create });
                using (var cursor = srcTxn.CreateCursor(srcDb))
                {
                    while (cursor.Next().resultCode == MDBResultCode.Success)
                    {
                        dstTxn.Put(dstDb, cursor.GetCurrent().key.CopyToNewArray(), cursor.GetCurrent().value.CopyToNewArray());
                    }
                }
            }
            dstTxn.Commit();
            srcTxn.Commit();

            _env.Dispose();
            tempEnv.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Directory.Delete(_cacheDirectory, true);
            Directory.Move(tempCacheDir, _cacheDirectory);
            return Initialize();
        }
        catch (Exception e)
        {
            return new Result(ResultStatus.Error, $"Failed to resize cache environment: {e.Message}");
        }
    }

    /// <summary>
    /// Removes all entries from a database
    /// </summary>
    /// <param name="database">target database(s)</param>
    /// <param name="bypass">bypass initialization check</param>
    /// <returns>A result indicating whether the database(s) were cleared successfully.</returns>
    public Result ClearDatabase(CacheDatabase database, bool bypass = false)
    {
        if (!_isInitialized && !bypass)
            return new Result(ResultStatus.Error, "Cache service must be initialized before calling ClearDatabase");

        try
        {
            var statsResult = GetStats();
            if (statsResult.Data == null)
                return new Result(ResultStatus.Error, $"Failed to get cache stats: {statsResult.Message}");

            var shouldResizeResult = ShouldResize(database, statsResult.Data, _cacheDirectory);
            if (shouldResizeResult.Status == ResultStatus.Error)
                return shouldResizeResult;

            var resize = shouldResizeResult.Data;
            var metaDataResult = new Result(ResultStatus.Ok);

            using var tx = _env.BeginTransaction();
            var queuedDataBases = new List<LightningDatabase>();
            switch (database)
            {
                case CacheDatabase.Vanilla:
                    queuedDataBases.Add(_vanillaDatabase);
                    break;
                case CacheDatabase.Modded:
                    queuedDataBases.Add(_moddedDatabase);
                    break;
                case CacheDatabase.VanillaBounds:
                    queuedDataBases.Add(_vanillaBoundsDatabase);
                    metaDataResult = SetMetaDataVanillaBoundsStatus(false);
                    break;
                case CacheDatabase.ModdedBounds:
                    queuedDataBases.Add(_moddedBoundsDatabase);
                    break;
                case CacheDatabase.AllBounds:
                    queuedDataBases.Add(_vanillaBoundsDatabase);
                    queuedDataBases.Add(_moddedBoundsDatabase);
                    metaDataResult = SetMetaDataVanillaBoundsStatus(false);
                    break;
                case CacheDatabase.AllDatabases:
                    queuedDataBases.Add(_vanillaDatabase);
                    queuedDataBases.Add(_moddedDatabase);
                    queuedDataBases.Add(_vanillaBoundsDatabase);
                    queuedDataBases.Add(_moddedBoundsDatabase);
                    metaDataResult = SetMetaDataVanillaBoundsStatus(false);
                    break;
                default:
                    return new Result(ResultStatus.Error, $"Unsupported database: {database}");
            }

            foreach (var db in queuedDataBases)
            {
                using var cursor = tx.CreateCursor(db);
                while (cursor.Next().resultCode == MDBResultCode.Success)
                {
                    cursor.Delete();
                }
            }
            tx.Commit();

            if (metaDataResult.Status == ResultStatus.Error)
                return metaDataResult;

            if (resize)
            {
                var resizeResult = ResizeEnvironment(bypass);
                if (resizeResult.Status == ResultStatus.Error)
                    return resizeResult;
            }

            return new Result(ResultStatus.Ok);
        }
        catch (Exception ex)
        {
            return new Result(ResultStatus.Error, $"Failed to clear database: {database}. {ex.Message}");
        }
    }

    /// <summary>
    /// Moves the cache directory
    /// </summary>
    /// <param name="fromPath">source path</param>
    /// <param name="toPath">target path</param>
    /// <returns>A result indicating whether the move succeeded.</returns>
    public Result Move(string fromPath, string toPath)
    {
        try
        {
            if (fromPath == toPath) return new Result(ResultStatus.Ok);

            DirectoryInfo fromInfo;
            bool fromExists;

            DirectoryInfo toInfo = new DirectoryInfo(toPath);
            bool toExists = toInfo.Exists;

            if (string.IsNullOrEmpty(fromPath))
            {
                fromExists = false;
            }
            else
            {
                fromInfo = new DirectoryInfo(fromPath);
                fromExists = fromInfo.Exists;
            }

            if (!fromExists)
            {
                _cacheDirectory = toPath;
                return new Result(ResultStatus.Ok);
            }

            if (toExists)
            {
                if (!Utils.IsDirectoryEmpty(toPath))
                    return new Result(ResultStatus.Error, "Target directory is not empty");
                Directory.Delete(toPath, true);
            }

            _isInitialized = false;

            if (_env != null)
                _env.Dispose();

            FileSystem.MoveDirectory(fromPath, toPath, UIOption.OnlyErrorDialogs);
            return new Result(ResultStatus.Ok);
        }
        catch (Exception ex)
        {
            return new Result(ResultStatus.Error, $"Failed to move cache directory: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets cache stats
    /// </summary>
    /// <returns>a result containing the populated cache stats if initialized, else -1 on all entry counts and 0 on all size fields</returns>
    public Result<CacheStats> GetStats()
    {
        if (!_isInitialized)
        {
            var uninitializedResult = new Result<CacheStats>
            {
                Status = ResultStatus.Warning,
                Data = new CacheStats()
            };
            uninitializedResult.AddMessage("Cache service is not initialized.");
            return uninitializedResult;
        }

        try
        {
            var result = new Result<CacheStats> { Status = ResultStatus.Ok };
            DirectoryInfo dirInfo = new DirectoryInfo(_cacheDirectory);
            ulong totalSize;
            try
            {
                totalSize = (ulong)dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
            }
            catch (Exception ex)
            {
                result.Status = ResultStatus.Warning;
                result.AddMessage($"Failed to get total size of cache: {ex.Message}");
                totalSize = 0;
            }
            ulong estVanillaSize = 0;
            ulong estModdedSize = 0;

            using var tx = _env.BeginTransaction();

            var vdbStats = _vanillaDatabase.DatabaseStats;
            var mdbStats = _moddedDatabase.DatabaseStats;
            var vbdbStats = _vanillaBoundsDatabase.DatabaseStats;
            var mdbdbStats = _moddedBoundsDatabase.DatabaseStats;

            ulong estVanillaBoundsSize = (ulong)vbdbStats.Entries * EstimatedBoundsEntrySizeInBytes;
            ulong estModdedBoundsSize = (ulong)mdbdbStats.Entries * EstimatedBoundsEntrySizeInBytes;

            var totalSizeWithoutBounds = totalSize - (estVanillaBoundsSize + estModdedBoundsSize);
            long totalEntries = vdbStats.Entries + mdbStats.Entries;
            if (totalEntries > 0)
            {
                estVanillaSize = totalSizeWithoutBounds / (ulong)totalEntries * (ulong)vdbStats.Entries;
                estModdedSize = totalSizeWithoutBounds / (ulong)totalEntries * (ulong)mdbStats.Entries;
            }

            result.Data = new CacheStats()
            {
                VanillaEntries = vdbStats.Entries,
                ModdedEntries = mdbStats.Entries,
                EstVanillaSize = new(estVanillaSize),
                EstModdedSize = new(estModdedSize),

                VanillaBoundsEntries = vbdbStats.Entries,
                ModdedBoundsEntries = mdbdbStats.Entries,
                EstVanillaBoundsSize = new(estVanillaBoundsSize),
                EstModdedBoundsSize = new(estModdedBoundsSize)
            };
            return result;
        }
        catch (Exception ex)
        {
            var result = new Result<CacheStats> { Status = ResultStatus.Error };
            result.AddMessage($"Failed to get cache stats: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Gets a sample of all cache entries (only used for testing)
    /// </summary>
    /// <param name="sampleSize"></param>
    /// <returns>A result containing the sample.</returns>
    public Result<CacheDatabaseSample> GetSample(int sampleSize)
    {
        if (!_isInitialized)
        {
            var errorResult = new Result<CacheDatabaseSample> { Status = ResultStatus.Error };
            errorResult.AddMessage("Cache service must be initialized before calling GetSample");
            return errorResult;
        }

        try
        {
            var moddedSample = new string[sampleSize];
            var vanillaSample = new string[sampleSize];
            int moddedCount;
            int vanillaCount;

            using var tx = _env.BeginTransaction();
            using (var cursor = tx.CreateCursor(_vanillaDatabase))
            {
                int i = 0;
                while (cursor.Next().resultCode == MDBResultCode.Success)
                {
                    if (i == sampleSize) break;
                    var entry = cursor.GetCurrent();
                    vanillaSample[i] = $"{BitConverter.ToString(entry.Item2.CopyToNewArray())} : {BitConverter.ToString(entry.Item3.CopyToNewArray())}";
                    i++;
                }
                cursor.First();
                vanillaCount = cursor.AsEnumerable().Count();
            }
            using (var cursor = tx.CreateCursor(_moddedDatabase))
            {
                int i = 0;
                while (cursor.Next().resultCode == MDBResultCode.Success)
                {
                    if (i == sampleSize) break;
                    var entry = cursor.GetCurrent();
                    moddedSample[i] = $"{BitConverter.ToString(entry.Item2.CopyToNewArray())} : {BitConverter.ToString(entry.Item3.CopyToNewArray())}";
                    i++;
                }
                cursor.First();
                moddedCount = cursor.AsEnumerable().Count();
            }

            return new Result<CacheDatabaseSample>
            {
                Status = ResultStatus.Ok,
                Data = new CacheDatabaseSample(moddedCount, vanillaCount, moddedSample, vanillaSample)
            };
        }
        catch (Exception ex)
        {
            var result = new Result<CacheDatabaseSample> { Status = ResultStatus.Error };
            result.AddMessage($"Failed to get cache sample: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Returns additional metadata about cache
    /// </summary>
    /// <returns>A result containing the cache metadata.</returns>
    public Result<CacheDatabaseMetadata> GetMetadata()
    {
        try
        {
            string filePath = Path.Combine(_cacheDirectory, "metadata.json");

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var metadata = JsonSerializer.Deserialize<CacheDatabaseMetadata>(json);
                if (metadata != null)
                {
                    return new Result<CacheDatabaseMetadata>
                    {
                        Status = ResultStatus.Ok,
                        Data = metadata
                    };
                }
            }

            var newMetadata = new CacheDatabaseMetadata(CurrentCacheVersion, _gameVersion);
            Directory.CreateDirectory(_cacheDirectory);
            File.WriteAllText(filePath, JsonSerializer.Serialize(newMetadata));
            return new Result<CacheDatabaseMetadata>
            {
                Status = ResultStatus.Ok,
                Data = newMetadata
            };
        }
        catch (Exception ex)
        {
            var result = new Result<CacheDatabaseMetadata> { Status = ResultStatus.Error };
            result.AddMessage($"Failed to get cache metadata: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Changes the VanillaSectorBoundsStatus in the metadata.json file.
    /// </summary>
    /// <param name="newValue"></param>
    /// <returns>A result indicating whether the metadata was updated.</returns>
    public Result SetMetaDataVanillaBoundsStatus(bool newValue)
    {
        var metadataResult = GetMetadata();
        if (metadataResult.Status == ResultStatus.Error || metadataResult.Data == null)
            return new Result(ResultStatus.Error, $"Failed to update cache metadata: {metadataResult.Message}");

        try
        {
            var metadata = metadataResult.Data;
            metadata.AreVanillaSectorBBsBuild = newValue;
            File.WriteAllText(Path.Combine(_cacheDirectory, "metadata.json"), JsonSerializer.Serialize(metadata));
            return new Result(ResultStatus.Ok);
        }
        catch (Exception ex)
        {
            return new Result(ResultStatus.Error, $"Failed to update cache metadata: {ex.Message}");
        }
    }

    /// <summary>
    /// Serializes the vanilla sector bounding boxes stored in the cache to a struct.
    /// </summary>
    /// <returns>A result containing the serialized bounding boxes.</returns>
    public Result<SerializedSectorBoundingBoxes> SerializeSectorBoundingBoxesToStruct()
    {
        if (!_isInitialized)
        {
            var errorResult = new Result<SerializedSectorBoundingBoxes> { Status = ResultStatus.Error };
            errorResult.AddMessage("Cache service must be initialized before serializing sector bounds");
            return errorResult;
        }

        try
        {
            var outData = new SerializedSectorBoundingBoxes();
            var metadataResult = GetMetadata();
            if (metadataResult.Status == ResultStatus.Error || metadataResult.Data == null)
            {
                var errorResult = new Result<SerializedSectorBoundingBoxes> { Status = ResultStatus.Error };
                errorResult.AddMessage($"Failed to serialize sector bounds: {metadataResult.Message}");
                return errorResult;
            }

            var cacheMetadata = metadataResult.Data;
            outData.GameVersion = cacheMetadata.GameVersion;
            outData.DatabaseVersion = cacheMetadata.DatabaseVersion;
            outData.SectorBoundingBoxes = new List<KeyValuePair<string, BoundingBox>>();
            using var tx = _env.BeginTransaction();
            using var cursor = tx.CreateCursor(_vanillaBoundsDatabase);
            while (cursor.Next().resultCode == MDBResultCode.Success)
            {
                var entry = cursor.GetCurrent();
                outData.SectorBoundingBoxes.Add(new KeyValuePair<string, BoundingBox>(
                    Encoding.Default.GetString(entry.Item2.CopyToNewArray()),
                    MessagePackSerializer.Deserialize<BoundingBox>(entry.Item3.CopyToNewArray())));
            }

            return new Result<SerializedSectorBoundingBoxes>
            {
                Status = ResultStatus.Ok,
                Data = outData
            };
        }
        catch (Exception ex)
        {
            var result = new Result<SerializedSectorBoundingBoxes> { Status = ResultStatus.Error };
            result.AddMessage($"Failed to serialize sector bounds: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Serializes the vanilla sector bounding boxes stored in the cache to memory.
    /// </summary>
    /// <returns>A result containing the serialized bounding boxes.</returns>
    public Result<byte[]> SerializeSectorBoundingBoxesToMemory()
    {
        var outDataResult = SerializeSectorBoundingBoxesToStruct();
        if (outDataResult.Status == ResultStatus.Error || outDataResult.Data == null)
        {
            var errorResult = new Result<byte[]> { Status = ResultStatus.Error };
            errorResult.AddMessage(outDataResult.Message);
            return errorResult;
        }

        try
        {
            return new Result<byte[]>
            {
                Status = ResultStatus.Ok,
                Data = MessagePackSerializer.Serialize(outDataResult.Data)
            };
        }
        catch (Exception ex)
        {
            var result = new Result<byte[]> { Status = ResultStatus.Error };
            result.AddMessage($"Failed to serialize sector bounds to memory: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Dumps sector bounding box data from the cache to a binary file.
    /// The output file is stored in the user's application data directory within a debug folder, named with the game's version and VS2077 version.
    /// Intended for internal use only.
    /// </summary>
    /// <param name="filePath">The file to write the serialized sector bounds to.</param>
    /// <returns>A result indicating whether the file was written.</returns>
    public Result SerializeSectorBoundingBoxesToFile(string filePath)
    {
        var outDataResult = SerializeSectorBoundingBoxesToMemory();
        if (outDataResult.Status == ResultStatus.Error || outDataResult.Data == null)
            return new Result(ResultStatus.Error, outDataResult.Message);

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllBytes(filePath, outDataResult.Data);
            return new Result(ResultStatus.Ok, $"Serialized sector bounds to file {filePath}...");
        }
        catch (Exception ex)
        {
            return new Result(ResultStatus.Error, $"Failed to serialize sector bounds to file: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads sector bounding boxes from a file into the cache database.
    /// The file must contain a serialized SectorBBDump object, and it must match the current game version
    /// and VS2077 version defined in the cache metadata.
    /// </summary>
    /// <param name="path">The file path containing the serialized sector bounding boxes.</param>
    /// <returns>A result indicating whether the file was loaded.</returns>
    public Result LoadSectorBBFromFile(string path)
    {
        if (!File.Exists(path))
            return new Result(ResultStatus.Error, "File does not exist!");

        try
        {
            var clearResult = ClearDatabase(CacheDatabase.VanillaBounds);
            if (clearResult.Status == ResultStatus.Error)
                return clearResult;

            var dump = MessagePackSerializer.Deserialize<SerializedSectorBoundingBoxes>(File.ReadAllBytes(path));
            if (dump == null)
                return new Result(ResultStatus.Error, "File does not contain valid sector bounds!");

            var metadataResult = GetMetadata();
            if (metadataResult.Status == ResultStatus.Error || metadataResult.Data == null)
                return new Result(ResultStatus.Error, $"Failed to load sector bounds: {metadataResult.Message}");

            var cacheMetadata = metadataResult.Data;
            if (dump.GameVersion != cacheMetadata.GameVersion || dump.DatabaseVersion != cacheMetadata.DatabaseVersion)
                return new Result(ResultStatus.Error, "File does not match current cache version!");

            using var tx = _env.BeginTransaction();
            foreach (var sector in dump.SectorBoundingBoxes)
            {
                var status = tx.Put(_vanillaBoundsDatabase, Encoding.UTF8.GetBytes(sector.Key), MessagePackSerializer.Serialize(sector.Value));
                if (status != MDBResultCode.Success)
                    return new Result(ResultStatus.Error, $"Failed to write sector bounds for {sector.Key} with status {status}");
            }
            var commitStatus = tx.Commit();
            if (commitStatus != MDBResultCode.Success)
                return new Result(ResultStatus.Error, $"Failed to commit sector bounds with status {commitStatus}");

            var setMetadataResult = SetMetaDataVanillaBoundsStatus(true);
            if (setMetadataResult.Status == ResultStatus.Error)
                return setMetadataResult;

            return new Result(ResultStatus.Ok, $"Loaded {dump.SectorBoundingBoxes.Count} sector bounds from file {path}...");
        }
        catch (Exception ex)
        {
            return new Result(ResultStatus.Error, $"Failed to load sector bounds from file: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes the cache metadata file if it exists
    /// </summary>
    private Result ClearMetaData()
    {
        try
        {
            var metaDataFilePath = Path.Combine(_cacheDirectory, "metadata.json");
            if (File.Exists(metaDataFilePath))
                File.Delete(metaDataFilePath);
            return new Result(ResultStatus.Ok);
        }
        catch (Exception ex)
        {
            return new Result(ResultStatus.Error, $"Failed to clear cache metadata: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes the cache metadata file if it exists
    /// </summary>
    /// <returns>A result indicating whether the known bad resources were cleared.</returns>
    public Result ClearKnownBadResources()
    {
        try
        {
            var knownBadResourcesFile = Path.Combine(_cacheDirectory, "knownBadResources.txt");
            if (File.Exists(knownBadResourcesFile))
                File.Delete(knownBadResourcesFile);
            _knownBadResources.Clear();
            return new Result(ResultStatus.Ok);
        }
        catch (Exception ex)
        {
            _knownBadResources.Clear();
            return new Result(ResultStatus.Error, $"Failed to clear known bad resources: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the count of known bad resources
    /// </summary>
    /// <returns></returns>
    public int GetKnownBadResourceCount() => _knownBadResources.Count;

    private Result SaveKnownBadResources()
    {
        lock (_saveKnownBadResourcesLock)
        {
            if (_knownBadResources.IsEmpty) return new Result(ResultStatus.Ok);
            try
            {
                var knownBadResourcesFile = Path.Combine(_cacheDirectory, "knownBadResources.txt");
                File.WriteAllLines(knownBadResourcesFile, _knownBadResources.Keys);
                return new Result(ResultStatus.Ok);
            }
            catch (Exception ex)
            {
                return new Result(ResultStatus.Error, $"Failed to save known bad resources: {ex.Message}");
            }
        }
    }

    private Result LoadKnownBadResources()
    {
        try
        {
            var knownBadResourcesFile = Path.Combine(_cacheDirectory, "knownBadResources.txt");
            if (!File.Exists(knownBadResourcesFile))
                return new Result(ResultStatus.Ok);
            foreach (var line in File.ReadAllLines(knownBadResourcesFile))
                _knownBadResources.TryAdd(line, 0);
            return new Result(ResultStatus.Ok);
        }
        catch (Exception ex)
        {
            return new Result(ResultStatus.Error, $"Failed to load known bad resources: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if there is enough space on the drive and if the change is significant enough to resize the database
    /// </summary>
    /// <param name="db">database to calculate for</param>
    /// <param name="stats">current cache stats</param>
    /// <param name="cacheDirectory">current cache directory</param>
    /// <param name="resizeAfterBytes">threshold for resizing</param>
    /// <returns>A result containing whether the database should be resized.</returns>
    private static Result<bool> ShouldResize(CacheDatabase db, CacheStats stats, string cacheDirectory, ulong resizeAfterBytes = 1024 * 1024 * 1024)
    {
        try
        {
            FileSize sizeToRemove;
            FileSize totalSize = new FileSize(stats.EstVanillaSize.Bytes +
                                              stats.EstModdedSize.Bytes +
                                              stats.EstVanillaBoundsSize.Bytes +
                                              stats.EstModdedBoundsSize.Bytes);

            switch (db)
            {
                case CacheDatabase.Vanilla:
                    sizeToRemove = stats.EstVanillaSize;
                    break;
                case CacheDatabase.Modded:
                    sizeToRemove = stats.EstModdedSize;
                    break;
                case CacheDatabase.VanillaBounds:
                    sizeToRemove = stats.EstVanillaBoundsSize;
                    break;
                case CacheDatabase.ModdedBounds:
                    sizeToRemove = stats.EstModdedBoundsSize;
                    break;
                case CacheDatabase.AllBounds:
                    sizeToRemove = new FileSize(stats.EstVanillaBoundsSize.Bytes + stats.EstModdedBoundsSize.Bytes);
                    break;
                case CacheDatabase.AllDatabases:
                    sizeToRemove = totalSize;
                    break;
                default:
                    var unsupportedResult = new Result<bool> { Status = ResultStatus.Error };
                    unsupportedResult.AddMessage($"Unsupported database: {db}");
                    return unsupportedResult;
            }

            var cacheDriveInfo = new DriveInfo(cacheDirectory);
            var freeSpace = (ulong)cacheDriveInfo.AvailableFreeSpace;

            return new Result<bool>
            {
                Status = ResultStatus.Ok,
                Data = sizeToRemove.Bytes > resizeAfterBytes && freeSpace > totalSize.Bytes - sizeToRemove.Bytes
            };
        }
        catch (Exception ex)
        {
            var result = new Result<bool> { Status = ResultStatus.Error };
            result.AddMessage($"Failed to determine whether the cache should be resized: {ex.Message}");
            return result;
        }
    }
}
