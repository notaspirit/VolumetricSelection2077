using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightningDB;
using MessagePack;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Core.Services;

/// <summary>
/// This part of the partial class is responsible for read and write operations to the cache.
/// </summary>
public partial class CacheService
{
    /// <summary>
    /// Gets a single entry from the cache
    /// </summary>
    /// <param name="request"></param>
    /// <returns>A result containing the entry data. The data is null if the entry does not exist.</returns>
    public Result<byte[]> GetEntry(ReadCacheRequest request)
    {
        if (!_isInitialized)
        {
            var errorResult = new Result<byte[]> { Status = ResultStatus.Error };
            errorResult.AddMessage("Cache service is not initialized.");
            return errorResult;
        }

        try
        {
            using var tx = _env.BeginTransaction();
            LightningDatabase[] dbs;
            switch (request.Database)
            {
                case CacheDatabase.Vanilla:
                    dbs = [_vanillaDatabase];
                    break;
                case CacheDatabase.Modded:
                    dbs = [_moddedDatabase];
                    break;
                case CacheDatabase.VanillaBounds:
                    dbs = [_vanillaBoundsDatabase];
                    break;
                case CacheDatabase.ModdedBounds:
                    dbs = [_moddedBoundsDatabase];
                    break;
                case CacheDatabase.AllBounds:
                    dbs = [_vanillaBoundsDatabase, _moddedBoundsDatabase];
                    break;
                case CacheDatabase.AllDatabases:
                    dbs = [_vanillaDatabase, _moddedDatabase];
                    break;
                default:
                    var unsupportedResult = new Result<byte[]> { Status = ResultStatus.Error };
                    unsupportedResult.AddMessage($"Unsupported database: {request.Database}");
                    return unsupportedResult;
            }

            foreach (LightningDatabase db in dbs)
            {
                var (code, _, value) = tx.Get(db, Encoding.UTF8.GetBytes(request.Key));
                if (code == MDBResultCode.Success)
                {
                    return new Result<byte[]>
                    {
                        Status = ResultStatus.Ok,
                        Data = value.CopyToNewArray()
                    };
                }
            }

            return new Result<byte[]> { Status = ResultStatus.Ok };
        }
        catch (Exception ex)
        {
            var result = new Result<byte[]> { Status = ResultStatus.Error };
            result.AddMessage($"Failed to get cache entry with key {request.Key}: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Gets all entries from the specified database.
    /// </summary>
    /// <param name="database"></param>
    /// <returns>A result containing all entries in the requested database.</returns>
    public Result<KeyValuePair<string, byte[]>[]> GetAllEntries(CacheDatabase database)
    {
        if (!_isInitialized)
        {
            var errorResult = new Result<KeyValuePair<string, byte[]>[]> { Status = ResultStatus.Error };
            errorResult.AddMessage("Cache service is not initialized.");
            return errorResult;
        }

        switch (database)
        {
            case CacheDatabase.Vanilla:
                return GetAllEntries(_vanillaDatabase);
            case CacheDatabase.Modded:
                return GetAllEntries(_moddedDatabase);
            case CacheDatabase.VanillaBounds:
                return GetAllEntries(_vanillaBoundsDatabase);
            case CacheDatabase.ModdedBounds:
                return GetAllEntries(_moddedBoundsDatabase);
            case CacheDatabase.AllBounds:
                var vanillaResult = GetAllEntries(_vanillaBoundsDatabase);
                if (vanillaResult.Status == ResultStatus.Error || vanillaResult.Data == null)
                    return vanillaResult;

                var moddedResult = GetAllEntries(_moddedBoundsDatabase);
                if (moddedResult.Status == ResultStatus.Error || moddedResult.Data == null)
                    return moddedResult;

                return new Result<KeyValuePair<string, byte[]>[]>
                {
                    Status = ResultStatus.Ok,
                    Data = vanillaResult.Data.Concat(moddedResult.Data).ToArray()
                };
            case CacheDatabase.AllDatabases:
                var unsupportedResult = new Result<KeyValuePair<string, byte[]>[]> { Status = ResultStatus.Error };
                unsupportedResult.AddMessage("Getting all entries from all databases is not supported.");
                return unsupportedResult;
            default:
                var defaultResult = new Result<KeyValuePair<string, byte[]>[]> { Status = ResultStatus.Error };
                defaultResult.AddMessage($"Unsupported database: {database}");
                return defaultResult;
        }
    }

    /// <summary>
    /// Gets all entries from the provided database.
    /// </summary>
    /// <param name="db"></param>
    /// <returns>A result containing all entries in the database.</returns>
    private Result<KeyValuePair<string, byte[]>[]> GetAllEntries(LightningDatabase db)
    {
        try
        {
            using var tx = _env.BeginTransaction();
            KeyValuePair<string, byte[]>[] entries = new KeyValuePair<string, byte[]>[db.DatabaseStats.Entries];
            using (var cursor = tx.CreateCursor(db))
            {
                int i = 0;
                while (cursor.Next().resultCode == MDBResultCode.Success)
                {
                    entries[i] = new KeyValuePair<string, byte[]>(Encoding.UTF8.GetString(cursor.GetCurrent().key.CopyToNewArray()), cursor.GetCurrent().value.CopyToNewArray());
                    i++;
                }
            }

            return new Result<KeyValuePair<string, byte[]>[]>
            {
                Status = ResultStatus.Ok,
                Data = entries
            };
        }
        catch (Exception ex)
        {
            var result = new Result<KeyValuePair<string, byte[]>[]> { Status = ResultStatus.Error };
            result.AddMessage($"Failed to get all cache entries: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Writes a single entry to the cache
    /// </summary>
    /// <param name="request"></param>
    /// <returns>A result indicating whether the entry was written.</returns>
    public Result WriteSingleEntry(WriteCacheRequest request)
    {
        if (!_isInitialized)
            return new Result(ResultStatus.Error, "Cache service must be initialized before calling WriteSingleEntry");

        try
        {
            using var tx = _env.BeginTransaction();
            MDBResultCode status;
            switch (request.Database)
            {
                case CacheDatabase.Vanilla:
                    status = tx.Put(_vanillaDatabase, Encoding.UTF8.GetBytes(request.Key), request.Data);
                    break;
                case CacheDatabase.Modded:
                    status = tx.Put(_moddedDatabase, Encoding.UTF8.GetBytes(request.Key), request.Data);
                    break;
                case CacheDatabase.VanillaBounds:
                    status = tx.Put(_vanillaBoundsDatabase, Encoding.UTF8.GetBytes(request.Key), request.Data);
                    break;
                case CacheDatabase.ModdedBounds:
                    status = tx.Put(_moddedBoundsDatabase, Encoding.UTF8.GetBytes(request.Key), request.Data);
                    break;
                case CacheDatabase.AllBounds:
                    status = tx.Put(_vanillaBoundsDatabase, Encoding.UTF8.GetBytes(request.Key), request.Data);
                    if (status == MDBResultCode.Success)
                        status = tx.Put(_moddedBoundsDatabase, Encoding.UTF8.GetBytes(request.Key), request.Data);
                    break;
                default:
                    return new Result(ResultStatus.Error, $"Unsupported database: {request.Database}");
            }

            if (status != MDBResultCode.Success)
                return new Result(ResultStatus.Error, $"Failed to write data with key {request.Key} to {request.Database} with status {status}");

            var commitStatus = tx.Commit();
            if (commitStatus != MDBResultCode.Success)
                return new Result(ResultStatus.Error, $"Failed to commit data with key {request.Key} to {request.Database} with status {commitStatus}");

            return new Result(ResultStatus.Ok);
        }
        catch (Exception ex)
        {
            return new Result(ResultStatus.Error, $"Failed to write data with key {request.Key} to {request.Database}: {ex.Message}");
        }
    }

    /// <summary>
    /// Enqueues AbbrSector (serialized on a background thread) to be written to cache
    /// </summary>
    /// <param name="path">game file path</param>
    /// <param name="sector"></param>
    /// <param name="database"></param>
    public void WriteEntry(string path, VSSector sector, CacheDatabase database)
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
    public void WriteEntry(string path, VSMesh mesh, CacheDatabase database)
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
    /// <returns>A result describing the outcome of writing all queued entries.</returns>
    private async Task<Result> ProcessWriteQueue()
    {
        if (!_isInitialized)
            return new Result(ResultStatus.Error, "Cache service must be initialized before calling ProcessWriteQueue");

        var result = new Result(ResultStatus.Ok);
        bool wroteExitLog = false;
        try
        {
            while (IsProcessing || _requestWriteQueue.Count > 0)
            {
                await Task.Delay(BatchDelay);
                if (!IsProcessing && !wroteExitLog && _requestWriteQueue.Count > 0)
                {
                    result.Status = ResultStatus.Warning;
                    result.AddMessage($"Continuing to write {_requestWriteQueue.Count} entries to cache, do not close the application...");
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
                        case CacheDatabase.Vanilla:
                            requestsVanilla.Add(request);
                            break;
                        case CacheDatabase.Modded:
                            requestsModded.Add(request);
                            break;
                        case CacheDatabase.VanillaBounds:
                            requestsVanillaBounds.Add(request);
                            break;
                        case CacheDatabase.ModdedBounds:
                            requestsModdedBounds.Add(request);
                            break;
                        case CacheDatabase.AllBounds:
                            requestsVanillaBounds.Add(request);
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
                        {
                            result.Status = ResultStatus.Error;
                            result.AddMessage($"Failed to write data with key {request.Key} to {request.Database} with status {status}");
                        }
                    }
                }

                if (requestsVanilla.Count > 0)
                {
                    foreach (var request in requestsVanilla)
                    {
                        var status = tx.Put(_vanillaDatabase,Encoding.UTF8.GetBytes(request.Key), request.Data);
                        if (status != MDBResultCode.Success)
                        {
                            result.Status = ResultStatus.Error;
                            result.AddMessage($"Failed to write data with key {request.Key} to {request.Database} with status {status}");
                        }
                    }
                }

                if (requestsVanillaBounds.Count > 0)
                {
                    foreach (var request in requestsVanillaBounds)
                    {
                        var status = tx.Put(_vanillaBoundsDatabase,Encoding.UTF8.GetBytes(request.Key), request.Data);
                        if (status != MDBResultCode.Success)
                        {
                            result.Status = ResultStatus.Error;
                            result.AddMessage($"Failed to write data with key {request.Key} to {request.Database} with status {status}");
                        }
                    }
                }

                if (requestsModdedBounds.Count > 0)
                {
                    foreach (var request in requestsModdedBounds)
                    {
                        var status = tx.Put(_moddedBoundsDatabase,Encoding.UTF8.GetBytes(request.Key), request.Data);
                        if (status != MDBResultCode.Success)
                        {
                            result.Status = ResultStatus.Error;
                            result.AddMessage($"Failed to write data with key {request.Key} to {request.Database} with status {status}");
                        }
                    }
                }

                var commitStatus = tx.Commit();
                if (commitStatus != MDBResultCode.Success)
                {
                    result.Status = ResultStatus.Error;
                    result.AddMessage($"Failed to commit {requests.Length} entries to cache with status {commitStatus}");
                }
            }

            if (ClearCacheOnExit)
            {
                var clearVanillaResult = ClearDatabase(CacheDatabase.Vanilla, true);
                if (clearVanillaResult.Status != ResultStatus.Ok)
                {
                    if (clearVanillaResult.Status == ResultStatus.Error)
                        result.Status = ResultStatus.Error;
                    else if (result.Status == ResultStatus.Ok)
                        result.Status = ResultStatus.Warning;
                    result.AddMessage(clearVanillaResult.Message);
                }

                var clearModdedResult = ClearDatabase(CacheDatabase.Modded, true);
                if (clearModdedResult.Status != ResultStatus.Ok)
                {
                    if (clearModdedResult.Status == ResultStatus.Error)
                        result.Status = ResultStatus.Error;
                    else if (result.Status == ResultStatus.Ok)
                        result.Status = ResultStatus.Warning;
                    result.AddMessage(clearModdedResult.Message);
                }
            }

            if (wroteExitLog)
                result.AddMessage("Finished writing all queued entries to cache");
        }
        catch (Exception ex)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage($"Failed to process cache write queue: {ex.Message}");
        }

        return result;
    }
}
