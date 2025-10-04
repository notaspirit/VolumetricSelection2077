using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LightningDB;
using MessagePack;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Models;

namespace VolumetricSelection2077.Services;

/// <summary>
/// This part of the partial class is responsible for read and write operations to the cache.
/// </summary>
public partial class CacheService
{
    /// <summary>
    /// Gets a single entry from the cache 
    /// </summary>
    /// <param name="request"></param>
    /// <returns>null if db is not supported, value doesn't exist or cache service is uninitialized</returns>
    public byte[]? GetEntry(ReadCacheRequest request)
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
    /// Gets all entries from the specified database.
    /// </summary>
    /// <param name="database"></param>
    /// <returns></returns>
    /// <remarks>returns null if the cache is not initialized, database doesn't exist or the target is all databases</remarks>
    public KeyValuePair<string, byte[]>[]? GetAllEntries(CacheDatabases database)
    {
        if (!_isInitialized) return null;
        switch (database)
        {
            case CacheDatabases.Vanilla:
                return GetAllEntries(_vanillaDatabase);
            case CacheDatabases.Modded:
                return GetAllEntries(_moddedDatabase);
            case CacheDatabases.VanillaBounds:
                return GetAllEntries(_vanillaBoundsDatabase);
            case CacheDatabases.ModdedBounds:
                return GetAllEntries(_moddedBoundsDatabase);
            case CacheDatabases.All:
                return null;
            default:
                return null;
        }
    }
    
    /// <summary>
    /// Gets all entries from the provided database.
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    /// <exception cref="Exception">if cache is not initialized</exception>
    private KeyValuePair<string, byte[]>[] GetAllEntries(LightningDatabase db)
    {
        using var tx = _env.BeginTransaction();
        KeyValuePair<string, byte[]>[] entries = new KeyValuePair<string, byte[]>[db.DatabaseStats.Entries];
        using (var cursor = tx.CreateCursor(db))
        {
            int i = 0;
            while (cursor.Next() == MDBResultCode.Success)
            {
                entries[i] = new KeyValuePair<string, byte[]>(Encoding.UTF8.GetString(cursor.GetCurrent().key.CopyToNewArray()), cursor.GetCurrent().value.CopyToNewArray());
                i++;
            }
        }
        return entries;
    }
    
    /// <summary>
    /// Writes a single entry to the cache
    /// </summary>
    /// <param name="request"></param>
    /// <exception cref="Exception">Cache is uninitialized</exception>
    public void WriteSingleEntry(WriteCacheRequest request)
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
            var request = new WriteCacheRequest(path, MessagePackSerializer.Serialize(sector), database);
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
            var request = new WriteCacheRequest(path, MessagePackSerializer.Serialize(mesh), database);
            _requestWriteQueue.Enqueue(request);
        });
    }
    
    /// <summary>
    /// Enqueues serialized data to be written to cache
    /// </summary>
    /// <param name="request"></param>
    public void WriteEntry(WriteCacheRequest request)
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
            WriteCacheRequest[] requests;
    
            lock (_lock)
            {
                requests = _requestWriteQueue.ToArray();
                _requestWriteQueue.Clear();
            }
            
            List<WriteCacheRequest> requestsModded = new();
            List<WriteCacheRequest> requestsVanilla = new();
            List<WriteCacheRequest> requestsModdedBounds = new();
            List<WriteCacheRequest> requestsVanillaBounds = new();
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
                    case CacheDatabases.VanillaBounds:
                        requestsVanillaBounds.Add(request);
                        break;
                    case CacheDatabases.ModdedBounds:
                        requestsModdedBounds.Add(request);
                        break;
                }
            }
            
            using var tx = _env.BeginTransaction();
            if (requestsModded.Count > 0)
            {
                foreach (var request in requestsModded)
                {
                    var status = tx.Put(_moddedDatabase,Encoding.UTF8.GetBytes(request.Key), request.Data);
                    if (status != MDBResultCode.Success)
                        Logger.Error($"Failed to write data with key {request.Key} to {request.Database} with status {status}");
                }
            }
            
            if (requestsVanilla.Count > 0)
            {
                foreach (var request in requestsVanilla)
                {
                    var status = tx.Put(_vanillaDatabase,Encoding.UTF8.GetBytes(request.Key), request.Data);
                    if (status != MDBResultCode.Success)
                        Logger.Error($"Failed to write data with key {request.Key} to {request.Database} with status {status}");
                }
            }
            
            if (requestsVanillaBounds.Count > 0)
            {
                foreach (var request in requestsVanillaBounds)
                {
                    var status = tx.Put(_vanillaBoundsDatabase,Encoding.UTF8.GetBytes(request.Key), request.Data);
                    if (status != MDBResultCode.Success)
                        Logger.Error($"Failed to write data with key {request.Key} to {request.Database} with status {status}");
                }
            }
                
            if (requestsModdedBounds.Count > 0)
            {
                foreach (var request in requestsModdedBounds)
                {
                    var status = tx.Put(_moddedBoundsDatabase,Encoding.UTF8.GetBytes(request.Key), request.Data);
                    if (status != MDBResultCode.Success)
                        Logger.Error($"Failed to write data with key {request.Key} to {request.Database} with status {status}");
                }
            }

            var commitStatus = tx.Commit();
            if (commitStatus != MDBResultCode.Success)
                Logger.Error($"Failed to commit {requests.Length} entries to cache with status {commitStatus}");
        }

        if (!_settings.CacheEnabled)
        {
            ClearDatabase(CacheDatabases.Vanilla, true);
            ClearDatabase(CacheDatabases.Modded, true);
        }
            
        
        if (wroteExitLog)
        {
            Logger.Success("Finished writing all queued entries to cache");
        }
    }
}