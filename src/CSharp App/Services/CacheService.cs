using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LightningDB;
using MessagePack;
using Microsoft.VisualBasic.FileIO;
using SharpDX;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Models;
using SearchOption = System.IO.SearchOption;

namespace VolumetricSelection2077.Services;

/// <summary>
/// This part of the class is responsible for managing the instance, lightingdb and debug / metadata. 
/// </summary>
public partial class CacheService
{
    private static CacheService? _instance;
    private SettingsService _settings;
    private LightningEnvironment _env;
    private static readonly long Kb = 1024;
    private static readonly long Gb = Kb * Kb * Kb;
    private static readonly int BatchDelay = 1;
    private static readonly int MaxReaders = 512;
    private static readonly long MapSize = Gb * 100;
    private static readonly ulong EstimatedBoundsEntrySizeInBytes = 116;
    static ConcurrentQueue<WriteCacheRequest> _requestWriteQueue = new();
    private readonly object _lock = new object();
    private LightningDatabase _vanillaDatabase;
    private LightningDatabase _moddedDatabase;
    private LightningDatabase _vanillaBoundsDatabase;
    private LightningDatabase _moddedBoundsDatabase;
    private bool _isInitialized;

    public bool IsInitialized
    {
        get => _isInitialized;
    }
    private static bool IsProcessing { get; set; } = false;
    
    private CacheService() { }
    public static CacheService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new CacheService();
            }
            return _instance;
        }
    }
    
    /// <summary>
    /// Initializes the Cache Service at the directory defined in the settings, sets Initialized bool if successful
    /// </summary>
    public void Initialize()
    {
        try
        {
            _settings = SettingsService.Instance;
            if (!ValidationService.ValidateAndCreateDirectory(_settings.CacheDirectory).Item1)
                throw new Exception("Invalid cache directory");
            
            if (_isInitialized) return;
            
            _env = new LightningEnvironment(_settings.CacheDirectory)
            {
                MaxDatabases = 4,
                MapSize = MapSize,
                MaxReaders = MaxReaders
            };
            _env.Open();
        
            var tx = _env.BeginTransaction();
            _moddedDatabase = tx.OpenDatabase(CacheDatabases.Modded.ToString(), new DatabaseConfiguration() { Flags = DatabaseOpenFlags.Create });
            _vanillaDatabase = tx.OpenDatabase(CacheDatabases.Vanilla.ToString(), new DatabaseConfiguration() { Flags = DatabaseOpenFlags.Create });
            _moddedBoundsDatabase = tx.OpenDatabase(CacheDatabases.ModdedBounds.ToString(), new DatabaseConfiguration() { Flags = DatabaseOpenFlags.Create });
            _vanillaBoundsDatabase = tx.OpenDatabase(CacheDatabases.VanillaBounds.ToString(), new DatabaseConfiguration() { Flags = DatabaseOpenFlags.Create });
            tx.Commit();
            
            var metaData = GetMetadata();
            if (!ValidationService.ValidateCache(metaData, _settings.GameDirectory, _settings.MinimumCacheVersion))
            {
                Logger.Warning("Cache is stale, resetting database");
                ClearMetaData();
                ClearDatabase(CacheDatabases.All, true);
            }
            _isInitialized = true;
        }
        catch (Exception e)
        {
            Logger.Exception(e, "Failed to initialize cache service!");
        }
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
        
        string tempCacheDir = Path.Combine(Directory.GetParent(_settings.CacheDirectory)?.FullName, $"temp_cache_{Guid.NewGuid().ToString()}");
        Directory.CreateDirectory(tempCacheDir);
        
        var metaDataFilePath = Path.Combine(_settings.CacheDirectory, "metadata.json");

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
        var databases = new [] { CacheDatabases.Vanilla.ToString(), CacheDatabases.Modded.ToString(), CacheDatabases.VanillaBounds.ToString(), CacheDatabases.ModdedBounds.ToString() };

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
                while (cursor.Next() == MDBResultCode.Success)
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
        
        Directory.Delete(_settings.CacheDirectory, true);
        Directory.Move(tempCacheDir, _settings.CacheDirectory);
        Initialize();
    }
    
    /// <summary>
    /// Removes all entries from a database
    /// </summary>
    /// <param name="database">target database(s)</param>
    /// <param name="bypass">bypass initialization check</param>
    /// <exception cref="Exception">Cache service is not initialized</exception>
    public void ClearDatabase(CacheDatabases database, bool bypass = false)
    {
        if (!_isInitialized && !bypass) throw new Exception("Cache service must be initialized before calling ClearDatabase");
        var resize = ShouldResize(database, GetStats(), _settings.CacheDirectory);
        
        try
        {
            
            using var tx = _env.BeginTransaction();
            switch (database)
            {
                case CacheDatabases.Vanilla:
                    using (var cursor = tx.CreateCursor(_vanillaDatabase))
                    {
                        while (cursor.Next() == MDBResultCode.Success)
                        {
                            cursor.Delete();
                        }
                    }
                    break;
                case CacheDatabases.Modded:
                    using (var cursor = tx.CreateCursor(_moddedDatabase))
                    {
                        while (cursor.Next() == MDBResultCode.Success)
                        {
                            cursor.Delete();
                        }
                    }
                    break;
                case CacheDatabases.VanillaBounds:
                    using (var cursor = tx.CreateCursor(_vanillaBoundsDatabase))
                    {
                        while (cursor.Next() == MDBResultCode.Success)
                        {
                            cursor.Delete();
                        }
                    }

                    SetMetaDataVanillaBoundsStatus(false);
                    break;
                case CacheDatabases.ModdedBounds:
                    using (var cursor = tx.CreateCursor(_moddedBoundsDatabase))
                    {
                        while (cursor.Next() == MDBResultCode.Success)
                        {
                            cursor.Delete();
                        }
                    }
                    break;
                case CacheDatabases.All:
                    using (var cursor = tx.CreateCursor(_vanillaDatabase))
                    {
                        while (cursor.Next() == MDBResultCode.Success)
                        {
                            cursor.Delete();
                        }
                    }
                    using (var cursor = tx.CreateCursor(_moddedDatabase))
                    {
                        while (cursor.Next() == MDBResultCode.Success)
                        {
                            cursor.Delete();
                        }
                    }
                    using (var cursor = tx.CreateCursor(_vanillaBoundsDatabase))
                    {
                        while (cursor.Next() == MDBResultCode.Success)
                        {
                            cursor.Delete();
                        }
                    }
                    SetMetaDataVanillaBoundsStatus(false);
                    using (var cursor = tx.CreateCursor(_moddedBoundsDatabase))
                    {
                        while (cursor.Next() == MDBResultCode.Success)
                        {
                            cursor.Delete();
                        }
                    }
                    break;
            }
            tx.Commit();
            if (resize) ResizeEnvironment(bypass);
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, $"Failed to clear database: {database}.");
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

        var toPathVr = ValidationService.ValidatePath(toPath);
        if (toPathVr != ValidationService.PathValidationResult.Valid)
            throw new ArgumentException($"Invalid target path provided: {toPathVr}");

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
            var fromPathVr = ValidationService.ValidatePath(fromPath);
            if (fromPathVr != ValidationService.PathValidationResult.Valid)
                throw new ArgumentException($"Invalid source path provided: {fromPathVr}");
            fromInfo = new DirectoryInfo(fromPath);
            fromExists = fromInfo.Exists;
        }
        
        if (!fromExists)
        {
            _settings.CacheDirectory = toPath;
            _settings.SaveSettings();
            return true;
        }
        
        if (toExists)
        {
            if (!UtilService.IsDirectoryEmpty(toPath))
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
        DirectoryInfo dirInfo = new DirectoryInfo(_settings.CacheDirectory);
        ulong totalSize;
        try
        {
            totalSize = (ulong)dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "Failed to get total size of cache!", true);
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
            while (cursor.Next() == MDBResultCode.Success)
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
            while (cursor.Next() == MDBResultCode.Success)
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
        string filePath = Path.Combine(_settings.CacheDirectory, "metadata.json");
        if (ValidationService.ValidatePath(filePath) != ValidationService.PathValidationResult.Valid)
            throw new Exception("Cache directory is invalid!");
        
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            var metadata = JsonSerializer.Deserialize<CacheDatabaseMetadata>(json);
            if (metadata != null) return metadata;
        }
        
        var gameExePath = Path.Combine(_settings.GameDirectory, "bin", "x64", "Cyberpunk2077.exe");
        if (ValidationService.ValidatePath(filePath) != ValidationService.PathValidationResult.Valid)
            throw new Exception("Game directory is invalid!");
        
        if (!File.Exists(gameExePath))
            throw new Exception("Could not find Game Executable.");
        
        var fileVerInfo = FileVersionInfo.GetVersionInfo(gameExePath);
        string? version = fileVerInfo.ProductVersion;
        if (version == null)
            throw new Exception("Could not find Game Version.");
        
        var newMetadata = new CacheDatabaseMetadata(_settings.MinimumCacheVersion, version);
        Directory.CreateDirectory(_settings.CacheDirectory);
        File.WriteAllText(filePath, JsonSerializer.Serialize(newMetadata));
        return newMetadata;
    }

    /// <summary>
    /// Changes the VanillaSectorBoundsStatus in the metadata.json file.
    /// </summary>
    /// <param name="newValue"></param>
    /// <returns></returns>
    public bool SetMetaDataVanillaBoundsStatus(bool newValue)
    {
        var metadata = GetMetadata();
        metadata.AreVanillaSectorBBsBuild = newValue;
        File.WriteAllText(Path.Combine(_settings.CacheDirectory, "metadata.json"), JsonSerializer.Serialize(metadata));
        return true;
    }

    /// <summary>
    /// Dumps sector bounding box data from the cache to a binary file.
    /// The output file is stored in the user's application data directory within a debug folder, named with the game's version and VS2077 version.
    /// Intended for internal use only.
    /// </summary>
    public void DumpSectorBBToFile()
    {
        try
        {
            var dump = new SectorBBDump();
            var cacheMetadata = GetMetadata();
            dump.GameVersion = cacheMetadata.GameVersion;
            dump.VS2077Version = cacheMetadata.VS2077Version;
            dump.Sectors = new List<KeyValuePair<string, BoundingBox>>();
            using var tx = _env.BeginTransaction();
            using var cursor = tx.CreateCursor(_vanillaBoundsDatabase);
            while (cursor.Next() == MDBResultCode.Success)
            {
                var entry = cursor.GetCurrent();
                dump.Sectors.Add(new KeyValuePair<string, BoundingBox>(
                    Encoding.Default.GetString(entry.Item2.CopyToNewArray()),
                    MessagePackSerializer.Deserialize<BoundingBox>(entry.Item3.CopyToNewArray())));
            }

            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "debug",
                $"{dump.GameVersion}-{dump.VS2077Version}.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, MessagePackSerializer.Serialize(dump));
            Logger.Info($"Dumped {dump.Sectors.Count} sector bounds to file {filePath}...");
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "Failed to dump sector bounds to file!");       
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
        ClearDatabase(CacheDatabases.VanillaBounds);
        var dump = MessagePackSerializer.Deserialize<SectorBBDump>(File.ReadAllBytes(path));
        var cacheMetadata = GetMetadata();
        if (dump.GameVersion != cacheMetadata.GameVersion || dump.VS2077Version != cacheMetadata.VS2077Version)
            throw new Exception("File does not match current cache version!");
        using var tx = _env.BeginTransaction();
        foreach (var sector in dump.Sectors)
        {
            tx.Put(_vanillaBoundsDatabase, Encoding.UTF8.GetBytes(sector.Key), MessagePackSerializer.Serialize(sector.Value));
        }
        tx.Commit();
        SetMetaDataVanillaBoundsStatus(true);
        Logger.Info($"Loaded {dump.Sectors.Count} sector bounds from file {path}...");
    }

    /// <summary>
    /// Deletes the cache metadata file if it exists
    /// </summary>
    private void ClearMetaData()
    {
        var metaDataFilePath = Path.Combine(_settings.CacheDirectory, "metadata.json");
        if (File.Exists(metaDataFilePath))
            File.Delete(metaDataFilePath);
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
    private static bool ShouldResize(CacheDatabases db, CacheStats stats, string cacheDirectory, ulong resizeAfterBytes = 1024 * 1024 * 1024)
    {
        FileSize sizeToRemove;
        FileSize totalSize = new FileSize(stats.EstVanillaSize.Bytes + 
                                          stats.EstModdedSize.Bytes +
                                          stats.EstVanillaBoundsSize.Bytes +
                                          stats.EstModdedBoundsSize.Bytes);
        
        switch (db)
        {
            case CacheDatabases.Vanilla:
                sizeToRemove = stats.EstVanillaSize;
                break;
            case CacheDatabases.Modded:
                sizeToRemove = stats.EstModdedSize;
                break;
            case CacheDatabases.VanillaBounds:
                sizeToRemove = stats.EstVanillaBoundsSize;
                break;
            case CacheDatabases.ModdedBounds:
                sizeToRemove = stats.EstModdedBoundsSize;
                break;
            case CacheDatabases.All:
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