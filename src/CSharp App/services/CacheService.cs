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
using VolumetricSelection2077.Models;

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
    private static readonly int BatchDelay = 1;
    private static readonly int MaxReaders = 512;
    private static readonly int MapSize = 10485760 * 100; // 1gb (should be more for actual use prob but for testing it will do)
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

    public void Initialize()
    {
        _settings = SettingsService.Instance;
        if (_isInitialized) return;
        if (_settings.CacheEnabled == false) return;
        
        Directory.CreateDirectory(_settings.CacheDirectory);
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
        
        try
        {
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
            Logger.Error($"Could not initialize CacheService: {e}");
        }
        
    }
    
    
    public void StartListening()
    {
        IsProcessing = true;
        _ = Task.Run(() => ProcessWriteQueue());
    }

    public void StopListening()
    {
        IsProcessing = false;
    }

    public byte[]? GetEntry(ReadRequest request)
    {
        if (!_isInitialized) throw new Exception("Cache service must be initialized before calling GetEntry");
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
                throw new ArgumentException("Unknown Database");
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

    public void WriteSectorEntry(string path, AbbrSector sector, CacheDatabases database)
    {
        if (!_isInitialized) throw new Exception("Cache service must be initialized before calling WriteSectorEntry");
        _ = Task.Run(() => WriteSingleEntry(new WriteRequest(path, MessagePackSerializer.Serialize(sector), database)));
    }
    
    public void WriteMeshEntry(string path, AbbrMesh mesh, CacheDatabases database)
    {
        if (!_isInitialized) throw new Exception("Cache service must be initialized before calling WriteMeshEntry");
        _ = Task.Run(() => WriteSingleEntry(new WriteRequest(path, MessagePackSerializer.Serialize(mesh), database)));
    }
    
    public void WriteEntry(WriteRequest request)
    {
        if (!_isInitialized) throw new Exception("Cache service must be initialized before calling WriteEntry");
        _requestWriteQueue.Enqueue(request);
    }
    
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

        if (wroteExitLog)
        {
            Logger.Success("Finished writing all queued entries to cache");
        }
    }

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
            Logger.Error($"Failed to drop database {database} with error: {ex}");
        }
    }

    public bool Move(string fromPath, string toPath)
    {
        _isInitialized = false;

        if (!Directory.Exists(fromPath))
        {
            Logger.Error($"Directory {fromPath} does not exist");
            return false;
        }
        
        if (Directory.Exists(toPath))
            if (Directory.EnumerateFiles(toPath, "*.*", SearchOption.AllDirectories).Any())
            {
                Logger.Error($"Directory {toPath} already exists and is not empty");
                return false;
            }
            else
                Directory.Delete(toPath, true);
        
        _env.Dispose();
        
        Directory.Move(fromPath, toPath);
        Initialize();
        return true;
    }

    public class CacheStats
    {
        public int VanillaEntries { get; set; }
        public double EstVanillaSize { get; set; }
        public int ModdedEntries { get; set; }
        public double EstModdedSize { get; set; }

        public CacheStats()
        {
            VanillaEntries = 0;
            EstVanillaSize = 0;
            ModdedEntries = 0;
            EstModdedSize = 0;
        }
    }

    public CacheStats GetStats()
    {
        if (!_isInitialized) throw new Exception("Cache service must be initialized before calling ClearDatabase");
        DirectoryInfo dirInfo = new DirectoryInfo(_settings.CacheDirectory);
        var totalSize = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length) / (1024.0 * 1024.0 * 1024.0);
        int vanillaEntries = 0;
        int moddedEntries = 0;
        using var tx = _env.BeginTransaction();
        using (var cursor = tx.CreateCursor(_vanillaDatabase))
        {
            while (cursor.Next() == MDBResultCode.Success)
                vanillaEntries++;
        }
        using (var cursor = tx.CreateCursor(_moddedDatabase))
        {
            while (cursor.Next() == MDBResultCode.Success)
                moddedEntries++;
        }

        var estVanillaSize = totalSize / (vanillaEntries + moddedEntries) * vanillaEntries;
        var estModdedSize = totalSize / (vanillaEntries + moddedEntries) * moddedEntries;
        
        
        return new CacheStats()
        {
            VanillaEntries = vanillaEntries,
            ModdedEntries = moddedEntries,
            EstVanillaSize = double.IsNaN(estVanillaSize) ? 0 : estVanillaSize,
            EstModdedSize = double.IsNaN(estModdedSize) ? 0 : estModdedSize,
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

    public CacheDatabaseMetadata GetMetadata()
    {
        string filePath = Path.Combine(_settings.CacheDirectory, "metadata.json");
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            var metadata = JsonSerializer.Deserialize<CacheDatabaseMetadata>(json);
            if (metadata != null) return metadata;
        }
        
        var gameExePath = Path.Combine(_settings.GameDirectory, "bin", "x64", "Cyberpunk2077.exe");
        if (!File.Exists(gameExePath))
        {
            throw new Exception("Could not find Game Executable.");
        }
        var fileVerInfo = FileVersionInfo.GetVersionInfo(gameExePath);
        string? version = fileVerInfo.ProductVersion;
        if (version == null)
        {
            Logger.Warning($"{version}");
            throw new Exception("Could not find Game Executable.");
        }
        var newMetadata = new CacheDatabaseMetadata(_settings.MinimumCacheVersion, version);
        Directory.CreateDirectory(_settings.CacheDirectory);
        File.WriteAllText(filePath, JsonSerializer.Serialize(newMetadata));
        return newMetadata;
    }
}