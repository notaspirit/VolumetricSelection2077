using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LightningDB;

namespace VolumetricSelection2077.Services;

public enum CacheDatabases
{
    Vanilla,
    Modded
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
    static ConcurrentQueue<WriteRequest> _requestWriteQueue = new();
    private LightningDatabase _vanillaDb;
    private LightningDatabase _moddedDb;
    private readonly object _lock = new object();
    private static bool IsProcessing { get; set; } = false;
    
    private CacheService()
    {
        _settings = SettingsService.Instance;
        string cacheDir = Path.Combine(_settings.CacheDirectory, "cache");
        Directory.CreateDirectory(cacheDir);
        _env = new LightningEnvironment(cacheDir)
        {
            MaxDatabases = 2,
            MapSize = 10485760 * 100, // 1gb (should be more for actual use prob but for testing it will do)
            MaxReaders = MaxReaders
        };
        _env.Open();
        using var tx = _env.BeginTransaction();
        // temporarily treat all as vanilla files
        _vanillaDb = tx.OpenDatabase(CacheDatabases.Vanilla.ToString(), new DatabaseConfiguration()
        {
            Flags = DatabaseOpenFlags.Create
        });
        _moddedDb = tx.OpenDatabase(CacheDatabases.Modded.ToString(), new DatabaseConfiguration()
        {
            Flags = DatabaseOpenFlags.Create
        });
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

    public void StartListening()
    {
        IsProcessing = true;
        _ = Task.Run(() => ProcessWriteQueue());
    }

    public void StopListening()
    {
        IsProcessing = false;
    }
    
    // only call this at the end of the program once it's certain that the env never has to be accessed in the lifecycle
    public void DisposeEnv()
    {
        _env.Dispose();
    }
    
    public byte[]? GetEntry(ReadRequest request)
    {
        using var tx = _env.BeginTransaction(TransactionBeginFlags.ReadOnly);
        LightningDatabase db;
        if (request.Database == CacheDatabases.Vanilla)
        {
            db = _vanillaDb;
        }
        else
        {
            db = _moddedDb;
        }
        var (code, _, value) = tx.Get(db, Encoding.UTF8.GetBytes(request.Key));
        if (code == MDBResultCode.Success)
        {
            return value.CopyToNewArray();
        }
        return null;
    }

    public void WriteEntry(WriteRequest request)
    {
        _requestWriteQueue.Enqueue(request);
    }
    private async Task ProcessWriteQueue()
    {
        bool wroteExitLog = false;
        while (IsProcessing || _requestWriteQueue.Count > 0)
        {
            await Task.Delay(BatchDelay);
            if (!IsProcessing && !wroteExitLog)
            {
                Logger.Info($"Continuing to write {_requestWriteQueue.Count} entries to cache, do not close the application...");
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
            foreach (var request in requestsModded)
            {
                tx.Put(_moddedDb,Encoding.UTF8.GetBytes(request.Key), request.Data);
            }
            foreach (var request in requestsVanilla)
            {
                tx.Put(_vanillaDb,Encoding.UTF8.GetBytes(request.Key), request.Data);
            }
            tx.Commit();
        }
        Logger.Success("Finished writing all queued entries to cache");
    }
    
    public void DropDatabase(CacheDatabases database)
    {
        try
        {
            using var tx = _env.BeginTransaction();
            using var db = tx.OpenDatabase(database.ToString());
            tx.DropDatabase(db);
            tx.Commit();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to drop database {database} with error: {ex}");
        }
    }
}