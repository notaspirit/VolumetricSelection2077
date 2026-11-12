using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using LightningDB;
using MessagePack;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.VisualBasic.FileIO;
using SharpDX;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;
using SearchOption = System.IO.SearchOption;

namespace VolumetricSelection2077.Core.Services;

/// <summary>
/// This part of the class is responsible for managing the instance, lightingdb and debug / metadata. 
/// </summary>
public partial class CacheService
{
    private IVSLogger _logger;
    
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
    
    public CacheService(IVSLogger logger, 
        string cacheDirectory,
        string gameVersion,
        bool clearCacheOnExit = false)
    {
        _logger = logger;
        _cacheDirectory = cacheDirectory;
        _gameVersion = gameVersion;
        ClearCacheOnExit = clearCacheOnExit;
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            Directory.CreateDirectory(_cacheDirectory);
            
            if (_isInitialized) return;
            
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
            
            LoadKnownBadResources();
            
            var metaData = GetMetadata();
            if (!ValidateCache())
            {
                _logger.Warning("Cache is stale, resetting database");
                ClearMetaData();
                ClearKnownBadResources();
                ClearDatabase(CacheDatabase.AllDatabases, true);
            }
            _isInitialized = true;
        }
        catch (Exception e)
        {
            _logger.Exception(e, "Failed to initialize cache service!");
        }
    }
    
    private bool ValidateCache()
    {
        var metaData = GetMetadata();

        if (metaData.DatabaseVersion != CurrentCacheVersion)
            return false;
        
        return _gameVersion == metaData.GameVersion;
    }

    /// <summary>
    /// Disposes of the cache service
    /// </summary>
    /// <returns></returns>
    public Task Dispose()
    {
        return Task.Run(() =>
        {
            if (!_isInitialized) return;
            if (_env == null) return;
            _isInitialized = false;
            if (IsProcessing)
            {
                IsProcessing = false;
                Task.Delay(BatchDelay * 10).Wait();
            }
            _env.Dispose();
        });
    }
    
    /// <summary>
    /// Starts listening to read requests
    /// </summary>
    /// <exception cref="Exception">Cache service is not initialized</exception>
    public void StartListening()
    {
        if (!_isInitialized) throw new Exception("Cache service must be initialized before calling StartListening.");
        if (IsProcessing) return;
        IsProcessing = true;
        _ = Task.Run(() => ProcessWriteQueue());
    }
    /// <summary>
    /// Stops listening to read requests
    /// </summary>
    public void StopListening()
    {
        IsProcessing = false;
        SaveKnownBadResources();
    }
    
    /// <summary>
    /// Resizes the environment 
    /// </summary>
    /// <param name="bypass">bypass initialization check => callers responsibility to ensure env is ready</param>
    /// <exception cref="Exception">If cache service is not initialized</exception>
    public void ResizeEnvironment(bool bypass = false)
    {
        if (!_isInitialized && !bypass) throw new Exception("Cache service must be initialized before calling ResizeEnvironment");
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
        Initialize();
    }
    
    /// <summary>
    /// Removes all entries from a database
    /// </summary>
    /// <param name="database">target database(s)</param>
    /// <param name="bypass">bypass initialization check</param>
    /// <exception cref="Exception">Cache service is not initialized</exception>
    public void ClearDatabase(CacheDatabase database, bool bypass = false)
    {
        if (!_isInitialized && !bypass) throw new Exception("Cache service must be initialized before calling ClearDatabase");
        var resize = ShouldResize(database, GetStats(), _cacheDirectory);
        
        try
        {
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
                    SetMetaDataVanillaBoundsStatus(false);
                    break;
                case CacheDatabase.ModdedBounds:
                    queuedDataBases.Add(_moddedBoundsDatabase);
                    break;
                case CacheDatabase.AllBounds:
                    queuedDataBases.Add(_vanillaBoundsDatabase);
                    queuedDataBases.Add(_moddedBoundsDatabase);
                    SetMetaDataVanillaBoundsStatus(false);
                    break;
                case CacheDatabase.AllDatabases:
                    queuedDataBases.Add(_vanillaDatabase);
                    queuedDataBases.Add(_moddedDatabase);
                    queuedDataBases.Add(_vanillaBoundsDatabase);
                    queuedDataBases.Add(_moddedBoundsDatabase);
                    SetMetaDataVanillaBoundsStatus(false);
                    break;
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
            if (resize) ResizeEnvironment(bypass);
        }
        catch (Exception ex)
        {
            _logger.Exception(ex, $"Failed to clear database: {database}.");
        }
    }

    /// <summary>
    /// Moves the cache directory
    /// </summary>
    /// <param name="fromPath">source path</param>
    /// <param name="toPath">target path</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">at least one provided path is invalid or relative</exception>
    /// <exception cref="Exception">target directory is not empty</exception>
    public bool Move(string fromPath, string toPath)
    {
        if (fromPath == toPath) return true;
        
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
            return true;
        }
        
        if (toExists)
        {
            if (!Utils.IsDirectoryEmpty(toPath))
                throw new Exception("Target directory is not empty");
            Directory.Delete(toPath, true);
        }
        
        _isInitialized = false;
        
        if (_env != null)
            _env.Dispose();
        
        FileSystem.MoveDirectory(fromPath, toPath, UIOption.OnlyErrorDialogs);
        return true;
    }

    /// <summary>
    /// Gets cache stats
    /// </summary>
    /// <returns>populated cache stats if initialized else -1 on all entry counts and 0 on all size fields</returns>
    public CacheStats GetStats()
    {
        if (!_isInitialized) return new CacheStats();
        DirectoryInfo dirInfo = new DirectoryInfo(_cacheDirectory);
        ulong totalSize;
        try
        {
            totalSize = (ulong)dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        }
        catch (Exception ex)
        {
            _logger.Exception(ex, "Failed to get total size of cache!", true);
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
        
        return new CacheStats()
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
    }

    /// <summary>
    /// Gets a sample of all cache entries (only used for testing)
    /// </summary>
    /// <param name="sampleSize"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public CacheDatabaseSample GetSample(int sampleSize)
    {
        if (!_isInitialized) throw new Exception("Cache service must be initialized before calling GetSample");
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
        return new CacheDatabaseSample(moddedCount, vanillaCount, moddedSample, vanillaSample);
    }
    
    /// <summary>
    /// Returns additional metadata about cache
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception">any used filepath is invalid (cache, game) or no game exe file found</exception>
    public CacheDatabaseMetadata GetMetadata()
    {
        string filePath = Path.Combine(_cacheDirectory, "metadata.json");
        
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            var metadata = JsonSerializer.Deserialize<CacheDatabaseMetadata>(json);
            if (metadata != null) return metadata;
        }
        
        var newMetadata = new CacheDatabaseMetadata(CurrentCacheVersion, _gameVersion);
        Directory.CreateDirectory(_cacheDirectory);
        File.WriteAllText(filePath, JsonSerializer.Serialize(newMetadata));
        return newMetadata;
    }

    /// <summary>
    /// Changes the VanillaSectorBoundsStatus in the metadata.json file.
    /// </summary>
    /// <param name="newValue"></param>
    /// <returns></returns>
    public void SetMetaDataVanillaBoundsStatus(bool newValue)
    {
        var metadata = GetMetadata();
        metadata.AreVanillaSectorBBsBuild = newValue;
        File.WriteAllText(Path.Combine(_cacheDirectory, "metadata.json"), JsonSerializer.Serialize(metadata));
    }

    public SerializedSectorBoundingBoxes? SerializeSectorBoundingBoxesToStruct()
    {
        try
        {
            var outData = new SerializedSectorBoundingBoxes();
            var cacheMetadata = GetMetadata();
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
            
            return outData;
        }
        catch (Exception ex)
        {
            _logger.Exception(ex, "Failed to serialize sector bounds!");       
        }
        return null;
    }

    public byte[]? SerializeSectorBoundingBoxesToMemory()
    {
        var outData = SerializeSectorBoundingBoxesToStruct();
        return outData == null ? null : MessagePackSerializer.Serialize(outData);
    }

    /// <summary>
    /// Dumps sector bounding box data from the cache to a binary file.
    /// The output file is stored in the user's application data directory within a debug folder, named with the game's version and VS2077 version.
    /// Intended for internal use only.
    /// </summary>
    public void SerializeSectorBoundingBoxesToFile(string filePath)
    {
        try
        {
            var outData = SerializeSectorBoundingBoxesToMemory();
            if (outData == null) 
                throw new Exception();
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, outData);
            _logger.Info($"Serialized sector bounds to file {filePath}...");
        }
        catch (Exception ex)
        {
            _logger.Exception(ex, "Failed to serialize sector bounds to file!");       
        }
    }

    /// <summary>
    /// Loads sector bounding boxes from a file into the cache database.
    /// The file must contain a serialized SectorBBDump object, and it must match the current game version
    /// and VS2077 version defined in the cache metadata.
    /// </summary>
    /// <param name="path">The file path containing the serialized sector bounding boxes.</param>
    /// <exception cref="Exception">
    /// Thrown if the file does not exist, or if the file's game version or VS2077 version
    /// does not match the current cache metadata version.
    /// </exception>
    public void LoadSectorBBFromFile(string path)
    {
        if (!File.Exists(path))
            throw new Exception("File does not exist!");
        ClearDatabase(CacheDatabase.VanillaBounds);
        var dump = MessagePackSerializer.Deserialize<SerializedSectorBoundingBoxes>(File.ReadAllBytes(path));
        var cacheMetadata = GetMetadata();
        if (dump.GameVersion != cacheMetadata.GameVersion || dump.DatabaseVersion != cacheMetadata.DatabaseVersion)
            throw new Exception("File does not match current cache version!");
        using var tx = _env.BeginTransaction();
        foreach (var sector in dump.SectorBoundingBoxes)
        {
            tx.Put(_vanillaBoundsDatabase, Encoding.UTF8.GetBytes(sector.Key), MessagePackSerializer.Serialize(sector.Value));
        }
        tx.Commit();
        SetMetaDataVanillaBoundsStatus(true);
        _logger.Info($"Loaded {dump.SectorBoundingBoxes.Count} sector bounds from file {path}...");
    }

    /// <summary>
    /// Deletes the cache metadata file if it exists
    /// </summary>
    private void ClearMetaData()
    {
        var metaDataFilePath = Path.Combine(_cacheDirectory, "metadata.json");
        if (File.Exists(metaDataFilePath))
            File.Delete(metaDataFilePath);
    }
    
    /// <summary>
    /// Deletes the cache metadata file if it exists
    /// </summary>
    public void ClearKnownBadResources()
    {
        var knownBadResourcesFile = Path.Combine(_cacheDirectory, "knownBadResources.txt");
        if (File.Exists(knownBadResourcesFile))
            File.Delete(knownBadResourcesFile);
        _knownBadResources.Clear();
    }
    
    /// <summary>
    /// Gets the count of known bad resources
    /// </summary>
    /// <returns></returns>
    public int GetKnownBadResourceCount() => _knownBadResources.Count;
    
    private void SaveKnownBadResources()
    {
        lock (_saveKnownBadResourcesLock)
        {
            if (_knownBadResources.IsEmpty) return;
            var knownBadResourcesFile = Path.Combine(_cacheDirectory, "knownBadResources.txt");
            File.WriteAllLines(knownBadResourcesFile, _knownBadResources.Keys);
        }
    }

    private void LoadKnownBadResources()
    {
        var knownBadResourcesFile = Path.Combine(_cacheDirectory, "knownBadResources.txt");
        if (!File.Exists(knownBadResourcesFile))
            return;
        foreach (var line in File.ReadAllLines(knownBadResourcesFile))
            _knownBadResources.TryAdd(line, 0);
    }
    
    /// <summary>
    /// Checks if there is enough space on the drive and if the change is significant enough to resize the database
    /// </summary>
    /// <param name="db">database to calculate for</param>
    /// <param name="stats">current cache stats</param>
    /// <param name="cacheDirectory">current cache directory</param>
    /// <param name="resizeAfterBytes">threshold for resizing</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">if CacheDatabases.All is passed</exception>
    private static bool ShouldResize(CacheDatabase db, CacheStats stats, string cacheDirectory, ulong resizeAfterBytes = 1024 * 1024 * 1024)
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
                throw new ArgumentOutOfRangeException(nameof(db), db, null);
        }
        
        var cacheDriveInfo = new DriveInfo(cacheDirectory);
        var freeSpace = (ulong)cacheDriveInfo.AvailableFreeSpace;
        
        return sizeToRemove.Bytes > resizeAfterBytes && freeSpace > totalSize.Bytes - sizeToRemove.Bytes;
    }
}