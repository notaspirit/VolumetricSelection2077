using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
    public string Database { get; set; }
    public string Key { get; set; }
    public ReadRequest(string key, CacheDatabases database = CacheDatabases.Vanilla)
    {
        Key = key;
        Database = database.ToString();
    }
}

public class WriteRequest
{
    public string Database { get; set; }
    public string Key { get; set; }
    public byte[] Data { get; set; }
    public WriteRequest(string key, byte[] data, CacheDatabases database = CacheDatabases.Vanilla)
    {
        Key = key;
        Data = data;
        Database = database.ToString();
    }
}


public class CacheService
{
    private static CacheService? _instance;
    private SettingsService _settings;
    private LightningEnvironment _env;
    private static readonly int BatchDelay = 10;
    private static readonly int MaxReaders = 512;
    private SemaphoreSlim _semaphore;
    static ConcurrentQueue<(ReadRequest key, TaskCompletionSource<byte[]?> tcs)> _requestReadQueue = new();
    static ConcurrentQueue<WriteRequest> _requestWriteQueue = new();
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
        if (_env.IsOpened == false) _env.Open();
        IsProcessing = true;
        _ = Task.Run(() => ProcessReadQueue());

    }

    public void StopListening()
    {
        IsProcessing = false;
    }

    // only call this at the end of the program once it's certain that the env never has to be accessed in the lifecycle
    public void DesposeEnv()
    {
        _env.Dispose();
    }
    
    public Task<byte[]?> GetEntry(ReadRequest request)
    {
        var tcs = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _requestReadQueue.Enqueue((request, tcs));
        return tcs.Task;
    }
    

    private async Task ProcessReadQueue()
    {
        while (IsProcessing || _requestReadQueue.Count > 0)
        {
            await Task.Delay(BatchDelay);
            var requestArray = _requestReadQueue.ToArray();
            _requestReadQueue.Clear();
            
            try
            {
                using var tx = _env.BeginTransaction();
                // temporarily treat all as vanilla files
                using var db = tx.OpenDatabase(CacheDatabases.Vanilla.ToString(), new DatabaseConfiguration
                {
                    Flags = DatabaseOpenFlags.Create
                });
                foreach (var request in requestArray)
                {
                    try
                    {
                        var (code, _, value) = tx.Get(db, Encoding.UTF8.GetBytes(request.key.Key));
                        if (code == MDBResultCode.Success)
                        {
                            request.tcs.SetResult(value.CopyToNewArray());
                        }
                        else
                        {
                            request.tcs.SetResult(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        request.tcs.SetResult(null);
                    }
                }
                tx.Commit();
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to process read queue: {e}");
            }
        }
    }
    public void WriteEntry(WriteRequest request)
    {
        _requestWriteQueue.Enqueue(request);
    }
    private async Task ProcessWriteQueue()
    {
        while (IsProcessing || _requestWriteQueue.Count > 0)
        {
            await Task.Delay(BatchDelay);
            var requestArray = _requestWriteQueue.ToArray();
            _requestWriteQueue.Clear();
            try
            {
                using var tx = _env.BeginTransaction();
                // temporarily treat all as vanilla files
                using var db = tx.OpenDatabase(CacheDatabases.Vanilla.ToString(), new DatabaseConfiguration
                {
                    Flags = DatabaseOpenFlags.Create
                });
                foreach (var request in requestArray)
                {
                    try
                    {
                        tx.Put(db,Encoding.UTF8.GetBytes(request.Key), request.Data);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to write entry: {request.Key} with error: {ex}");
                    }
                }
                tx.Commit();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to write batch to cache with error: {ex}");
            }
        }
    }

    public void DropDatabases(CacheDatabases database)
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