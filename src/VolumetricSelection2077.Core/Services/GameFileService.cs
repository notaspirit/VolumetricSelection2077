using VolumetricSelection2077.Core.Enums;
using WolvenKit.Common;
using WolvenKit.Common.Services;
using VolumetricSelection2077.Core.MessagePack.Helpers;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Models.WorldBuilder.Spawn.Collision;
using VolumetricSelection2077.Core.Parsers;
using WolvenKit.RED4.Types.Pools;

namespace VolumetricSelection2077.Core.Services;

public class GameFileService : IGameFileService
{
    private readonly ICacheService _cacheService;
    private readonly IVSSectorParser _sectorParser;
    private readonly IArchiveManager _archiveManager;
    private readonly GeometryCacheService _geometryCacheService;
    
    public GameFileService(
        ICacheService cacheService,
        IVSSectorParser sectorParser,
        IArchiveManager archiveManager,
        GeometryCacheService geometryCacheService)
    {
        _cacheService = cacheService;
        _sectorParser = sectorParser;
        _archiveManager = archiveManager;
        _geometryCacheService = geometryCacheService;
    }
    
    /// <summary>
    /// Gets a PhysX Mesh entry
    /// </summary>
    /// <param name="sectorHash"></param>
    /// <param name="actorHash"></param>
    /// <returns></returns>
    /// <remarks>Collision meshes are not cached</remarks>
    public async Task<Result<VSMesh>> GetPhysXMesh(ulong sectorHash, ulong actorHash)
    {
        var result = new Result<VSMesh>();
        
        var rawMesh = await _geometryCacheService.GetEntryAsync(sectorHash, actorHash);
        if (rawMesh == null)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage($"Failed to get PhysX mesh for sector hash {sectorHash} and actor hash {actorHash}");
            return result;
        }

        try
        {
            result.Data = VSMeshParser.ParseFromPhysX(rawMesh);
            result.Status = ResultStatus.Ok;
        }
        catch (Exception ex)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage($"Failed to parse PhysX mesh: {ex.Message}");
        }

        return result;
    }
    
    /// <summary>
    /// Gets a resource from the archives or cache and returns the parsed result
    /// </summary>
    /// <param name="path"></param>
    /// <param name="type">Resource type, must be <see cref="VSMesh"/> or <see cref="VSSector"/></param>
    /// <returns>Result containing the parsed resource, with any warnings collected in the message</returns>
    public Result GetResource(string path, Type type)
    {
        if (type == typeof(VSMesh))
            return GetResource<VSMesh>(path);
        if (type == typeof(VSSector))
            return GetResource<VSSector>(path);

        return new Result(ResultStatus.Error, $"Unsupported resource type: {type.Name}");
    }

    /// <summary>
    /// Gets a resource of type <typeparamref name="T"/> from the archives or cache and returns the parsed result
    /// </summary>
    /// <param name="path"></param>
    /// <typeparam name="T">Resource type, must be <see cref="VSMesh"/> or <see cref="VSSector"/></typeparam>
    /// <returns>Result containing the parsed resource, with any warnings collected in the message</returns>
    public Result<T> GetResource<T>(string path)
    {
        var result = new Result<T>();

        if (typeof(T) != typeof(VSMesh) && typeof(T) != typeof(VSSector))
        {
            result.Status = ResultStatus.Error;
            result.AddMessage($"Unsupported resource type: {typeof(T).Name}");
            return result;
        }
        
        var cachedResource = _cacheService.GetEntry(new ReadCacheRequest(path, CacheDatabase.AllDatabases));
        if (cachedResource is { Status: ResultStatus.Ok, Data: not null } &&
            MessagePackHelper.TryDeserialize<T>(cachedResource.Data, out var cached))
        {
            result.Data = cached;
            result.Status = ResultStatus.Ok;
            return result;
        }

        var rawResource = _archiveManager.GetCR2WFile(path);
        if (rawResource == null)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage("Failed to get CR2W file at specified filepath");
            return result;
        }
        
        var db = _archiveManager.Lookup(path, ArchiveManagerScope.Mods) != null ? CacheDatabase.Modded : CacheDatabase.Vanilla;

        if (typeof(T) == typeof(VSMesh))
        {
            VSMesh? parsedMesh;
            try
            {
                parsedMesh = VSMeshParser.ParseFromCR2W(rawResource);
            }
            catch (Exception ex)
            {
                result.Status = ResultStatus.Error;
                result.AddMessage($"Failed to parse mesh {path}: {ex.Message}");
                return result;
            }

            if (parsedMesh == null)
            {
                result.Status = ResultStatus.Error;
                result.AddMessage($"Failed to parse mesh {path}");
                return result;
            }

            result.Data = (T)(object)parsedMesh;
            result.Status = ResultStatus.Ok;
            
            _cacheService.WriteEntry(path, parsedMesh, db);
            
            return result;
        }

        Result<VSSector> parsedSector;
        try
        {
            parsedSector = _sectorParser.ParseFromCR2W(rawResource);
        }
        catch (Exception ex)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage($"Failed to parse sector {path}: {ex.Message}");
            return result;
        }

        if (parsedSector.Status == ResultStatus.Error)
        {
            result.Status = parsedSector.Status;
            result.AddMessage(parsedSector.Message);
            return result;
        }

        result.Data = (T)(object)parsedSector.Data!;
        result.Status = parsedSector.Status;
        result.AddMessage(parsedSector.Message);
        
        _cacheService.WriteEntry(path, parsedSector.Data!, db);
        
        return result;
    }
}