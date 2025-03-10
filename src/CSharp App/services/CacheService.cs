using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LightningDB;
using MessagePack;
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
    static ConcurrentQueue<WriteRequest> _requestWriteQueue = new();
    private readonly object _lock = new object();
    private LightningDatabase _vanillaDatabase;
    private LightningDatabase _moddedDatabase;
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
        _moddedDatabase = tx.OpenDatabase(CacheDatabases.Modded.ToString(), new DatabaseConfiguration() { Flags = DatabaseOpenFlags.Create });
        _vanillaDatabase = tx.OpenDatabase(CacheDatabases.Vanilla.ToString(), new DatabaseConfiguration() { Flags = DatabaseOpenFlags.Create });
        tx.Commit();
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
        _ = Task.Run(() => WriteSingleEntry(new WriteRequest(path, MessagePackSerializer.Serialize(sector), database)));
    }
    
    public void WriteMeshEntry(string path, AbbrMesh mesh, CacheDatabases database)
    {
        _ = Task.Run(() => WriteSingleEntry(new WriteRequest(path, MessagePackSerializer.Serialize(mesh), database)));
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
    
    public void ClearDatabase(CacheDatabases database)
    {
        try
        {
            using var tx = _env.BeginTransaction();
            switch (database)
            {
                case CacheDatabases.Vanilla:
                    using (var cursor = tx.CreateCursor(_vanillaDatabase))
                    {
                        cursor.First();
                        for (int i = 0;  i < cursor.AsEnumerable().Count(); i++)
                        {
                            cursor.Delete();
                            cursor.Next();
                        }
                    }
                    break;
                case CacheDatabases.Modded:
                    using (var cursor = tx.CreateCursor(_vanillaDatabase))
                    {
                        cursor.First();
                        for (int i = 0;  i < cursor.AsEnumerable().Count(); i++)
                        {
                            cursor.Delete();
                            cursor.Next();
                        }
                    }
                    break;
            }
            tx.Commit();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to drop database {database} with error: {ex}");
        }
    }
}