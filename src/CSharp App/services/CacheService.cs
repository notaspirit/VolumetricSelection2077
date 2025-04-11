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
using Microsoft.ClearScript.Util.Web;
using Microsoft.VisualBasic.FileIO;
using VolumetricSelection2077.Models;
using WolvenKit.Core.Extensions;
using WolvenKit.RED4.Types;
using SearchOption = System.IO.SearchOption;

namespace VolumetricSelection2077.Services;

public enum CacheDatabases
{
    Vanilla,
    Modded,
    All
}

public class ReadRequest
{
    public CacheDatabases Database { get; set; }
    public string Key { get; set; }
    public ReadRequest(string key, CacheDatabases database = CacheDatabases.Vanilla)
    {
        Key = key;
        Database = database;
    }
}

public class WriteRequest
{
    public CacheDatabases Database { get; set; }
    public string Key { get; set; }
    public byte[] Data { get; set; }
    public WriteRequest(string key, byte[] data, CacheDatabases database = CacheDatabases.Vanilla)
    {
        Key = key;
        Data = data;
        Database = database;
    }
}

public class CacheService
{
    private static CacheService? _instance;
    private SettingsService _settings;
    private LightningEnvironment _env;
    private static readonly long Gb = 1024 * 1024 * 1024;
    private static readonly int BatchDelay = 1;
    private static readonly int MaxReaders = 512;
    private static readonly long MapSize = Gb * 100;
    static ConcurrentQueue<WriteRequest> _requestWriteQueue = new();
    private readonly object _lock = new object();
    private LightningDatabase _vanillaDatabase;
    private LightningDatabase _moddedDatabase;
    private bool _isInitialized;

    public bool IsInitialized
    {
        get => _isInitialized;
    }
    private static bool IsProcessing { get; set; } = false;
    
    private CacheService()
    {
        Initialize();
    }
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
            if (_env != null) return;
            
            _env = new LightningEnvironment(_settings.CacheDirectory)
            {
                MaxDatabases = 2,
                MapSize = MapSize,
                MaxReaders = MaxReaders
            };
            _env.Open();
        
            var tx = _env.BeginTransaction();
            _moddedDatabase = tx.OpenDatabase(CacheDatabases.Modded.ToString(), new DatabaseConfiguration() { Flags = DatabaseOpenFlags.Create });
            _vanillaDatabase = tx.OpenDatabase(CacheDatabases.Vanilla.ToString(), new DatabaseConfiguration() { Flags = DatabaseOpenFlags.Create });
            tx.Commit();
            
            var metaData = GetMetadata();
            if (!ValidationService.ValidateCache(metaData, _settings.GameDirectory, _settings.MinimumCacheVersion))
            {
                Logger.Warning("Cache is stale, resetting database");
                ClearDatabase(CacheDatabases.All, true, true);
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
    /// Gets a single entry from the cache 
    /// </summary>
    /// <param name="request"></param>
    /// <returns>null if db is not supported, value doesn't exist or cache service is uninitialized</returns>
    public byte[]? GetEntry(ReadRequest request)
    {
        if (!_isInitialized) return null;
        using var tx = _env.BeginTransaction();
        LightningDatabase[] dbs;
        switch (request.Database)
        {
            case CacheDatabases.Vanilla:
                dbs = new[] { _vanillaDatabase };
                break;
            case CacheDatabases.Modded:
                dbs = new[] { _moddedDatabase };
                break;
            case CacheDatabases.All:
                dbs = new[] { _vanillaDatabase, _moddedDatabase };
                break;
            default:
                return null;
        }

        foreach (LightningDatabase db in dbs)
        {
            var (code, _, value) = tx.Get(db, Encoding.UTF8.GetBytes(request.Key));
            if (code == MDBResultCode.Success)
            {
                return value.CopyToNewArray();
            }
        }
        return null;
    }
    
    /// <summary>
    /// Writes a single entry to the cache
    /// </summary>
    /// <param name="request"></param>
    /// <exception cref="Exception">Cache is uninitialized</exception>
    public void WriteSingleEntry(WriteRequest request)
    {
        if (!_isInitialized) throw new Exception("Cache service must be initialized before calling WriteSingleEntry");
        using var tx = _env.BeginTransaction();
        switch (request.Database)
        {
            case CacheDatabases.Vanilla:
                tx.Put(_vanillaDatabase, Encoding.UTF8.GetBytes(request.Key), request.Data);
                break;
            case CacheDatabases.Modded:
                tx.Put(_moddedDatabase, Encoding.UTF8.GetBytes(request.Key), request.Data);
                break;
        }
        tx.Commit();
    }
    
    /// <summary>
    /// Enqueues AbbrSector (serialized on a background thread) to be written to cache
    /// </summary>
    /// <param name="path">game file path</param>
    /// <param name="sector"></param>
    /// <param name="database"></param>
    public void WriteEntry(string path, AbbrSector sector, CacheDatabases database)
    {
        if (!_isInitialized) return;
        _ = Task.Run(() =>
        {
            var request = new WriteRequest(path, MessagePackSerializer.Serialize(sector), database);
            _requestWriteQueue.Enqueue(request);
        });
    }
    
    /// <summary>
    /// Enqueues AbbrMesh (serialized on a background thread) to be written to cache
    /// </summary>
    /// <param name="path">game file path</param>
    /// <param name="mesh"></param>
    /// <param name="database"></param>
    public void WriteEntry(string path, AbbrMesh mesh, CacheDatabases database)
    {
        if (!_isInitialized) return;
        _ = Task.Run(() =>
        {
            var request = new WriteRequest(path, MessagePackSerializer.Serialize(mesh), database);
            _requestWriteQueue.Enqueue(request);
        });
    }
    
    /// <summary>
    /// Enqueues serialized data to be written to cache
    /// </summary>
    /// <param name="request"></param>
    public void WriteEntry(WriteRequest request)
    {
        if (!_isInitialized) return;
        _requestWriteQueue.Enqueue(request);
    }
    
    /// <summary>
    /// Loops while IsProcessing is true or request queue is > 0, writes all entries in bulk to cache
    /// </summary>
    /// <exception cref="Exception">Cache Service is not initialized</exception>
    private async Task ProcessWriteQueue()
    {
        if (!_isInitialized) throw new Exception("Cache service must be initialized before calling ProcessWriteQueue");
        bool wroteExitLog = false;
        while (IsProcessing || _requestWriteQueue.Count > 0)
        {
            await Task.Delay(BatchDelay);
            if (!IsProcessing && !wroteExitLog && _requestWriteQueue.Count > 0)
            {
                Logger.Warning($"Continuing to write {_requestWriteQueue.Count} entries to cache, do not close the application...");
                wroteExitLog = true;
            }

            if (!(_requestWriteQueue.Count > 0)) continue;
            WriteRequest[] requests;
    
            lock (_lock)
            {
                requests = _requestWriteQueue.ToArray();
                _requestWriteQueue.Clear();
            }
            
            List<WriteRequest> requestsModded = new();
            List<WriteRequest> requestsVanilla = new();
            foreach (var request in requests)
            {
                switch (request.Database)
                {
                    case CacheDatabases.Vanilla:
                        requestsVanilla.Add(request);
                        break;
                    case CacheDatabases.Modded:
                        requestsModded.Add(request);
                        break;
                }
            }
            
            using var tx = _env.BeginTransaction();
            if (requestsModded.Count > 0)
            {
                foreach (var request in requestsModded)
                {
                    tx.Put(_moddedDatabase,Encoding.UTF8.GetBytes(request.Key), request.Data);
                }
            }
            
            if (requestsVanilla.Count > 0)
            {
                foreach (var request in requestsVanilla)
                {
                    tx.Put(_vanillaDatabase,Encoding.UTF8.GetBytes(request.Key), request.Data);
                }
            }

            tx.Commit();
        }

        if (!_settings.CacheEnabled)
            ClearDatabase(CacheDatabases.All, true);
        
        if (wroteExitLog)
        {
            Logger.Success("Finished writing all queued entries to cache");
        }
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
        var tempEnv = new LightningEnvironment(tempCacheDir)
        {
            MaxDatabases = 2,
            MapSize = MapSize,
            MaxReaders = MaxReaders
        };
        tempEnv.Open();
        var databases = new [] { CacheDatabases.Vanilla.ToString(), CacheDatabases.Modded.ToString() };

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
    /// <param name="resize">resize environment after removing</param>
    /// <param name="bypass">bypass initialization check</param>
    /// <exception cref="Exception">Cache service is not initialized</exception>
    public void ClearDatabase(CacheDatabases database, bool resize = false, bool bypass = false)
    {
        if (!_isInitialized && !bypass) throw new Exception("Cache service must be initialized before calling ClearDatabase");
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
        if (toPathVr != ValidationService.PathValidationResult.ValidDirectory)
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
            if (fromPathVr != ValidationService.PathValidationResult.ValidDirectory)
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

    public class CacheStats
    {
        public long VanillaEntries { get; set; }
        public double EstVanillaSize { get; set; }
        public long ModdedEntries { get; set; }
        public double EstModdedSize { get; set; }

        public CacheStats()
        {
            VanillaEntries = -1;
            EstVanillaSize = -1;
            ModdedEntries = -1;
            EstModdedSize = -1;
        }
    }

    /// <summary>
    /// Gets cache stats
    /// </summary>
    /// <returns>populated cache stats if initialized else -1 on all fields</returns>
    public CacheStats GetStats()
    {
        if (!_isInitialized) return new CacheStats();
        DirectoryInfo dirInfo = new DirectoryInfo(_settings.CacheDirectory);
        var totalSize = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        long estVanillaSize = 0;
        long estModdedSize = 0;
        
        using var tx = _env.BeginTransaction();

        var vdbStats = _vanillaDatabase.DatabaseStats;
        var mdbStats = _moddedDatabase.DatabaseStats;
        
        var totalEntries = vdbStats.Entries + mdbStats.Entries;
        if (totalEntries > 0)
        {
            estVanillaSize = totalSize / totalEntries * vdbStats.Entries;
            estModdedSize = totalSize / totalEntries * mdbStats.Entries;
        }
        
        return new CacheStats()
        {
            VanillaEntries = vdbStats.Entries,
            ModdedEntries = mdbStats.Entries,
            EstVanillaSize = (double)estVanillaSize / Gb,
            EstModdedSize = (double)estModdedSize / Gb,
        };
    }
    
    public class DataBaseSample
    {
        public int moddedEntriesCount { get; set; }
        public int vanillaEntriesCount { get; set; }
        public string[] moddedEntriesSample { get; set; }
        public string[] vanillaEntriesSample { get; set; }
        public DataBaseSample(int moddedEntriesCount, int vanillaEntriesCount, string[] moddedEntriesSample, string[] vanillaEntriesSample)
        {
            this.moddedEntriesCount = moddedEntriesCount;
            this.vanillaEntriesCount = vanillaEntriesCount;
            this.moddedEntriesSample = moddedEntriesSample;
            this.vanillaEntriesSample = vanillaEntriesSample;
        }
    }

    /// <summary>
    /// Gets a sample of all cache entries (only used for testing)
    /// </summary>
    /// <param name="sampleSize"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public DataBaseSample GetSample(int sampleSize)
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
        return new DataBaseSample(moddedCount, vanillaCount, moddedSample, vanillaSample);
    }

    public class CacheDatabaseMetadata
    {
        public string VS2077Version { get; set; }
        public string GameVersion { get; set; }
        public CacheDatabaseMetadata(string vs2077Version, string gameVersion)
        {
            VS2077Version = vs2077Version;
            GameVersion = gameVersion;
        }
    }
    
    /// <summary>
    /// Returns additional metadata about cache
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception">any used filepath is invalid (cache, game) or no game exe file found</exception>
    public CacheDatabaseMetadata GetMetadata()
    {
        string filePath = Path.Combine(_settings.CacheDirectory, "metadata.json");
        if (ValidationService.ValidatePath(filePath) != ValidationService.PathValidationResult.ValidFile)
            throw new Exception("Cache directory is invalid!");
        
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            var metadata = JsonSerializer.Deserialize<CacheDatabaseMetadata>(json);
            if (metadata != null) return metadata;
        }
        
        var gameExePath = Path.Combine(_settings.GameDirectory, "bin", "x64", "Cyberpunk2077.exe");
        if (ValidationService.ValidatePath(filePath) != ValidationService.PathValidationResult.ValidFile)
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
}