using VolumetricSelection2077.Core.Enums;
using WolvenKit;
using WolvenKit.Common.Services;
using WolvenKit.Core.Interfaces;
using WolvenKit.Core.Services;
using WolvenKit.RED4.CR2W.Archive;
using WolvenKit.RED4.CR2W;
using VolumetricSelection2077.Core.MessagePack.Helpers;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Models.WorldBuilder.Spawn.Collision;
using VolumetricSelection2077.Core.Parsers;
using WolvenKit.Common;
using WolvenKit.RED4.Types.Pools;

namespace VolumetricSelection2077.Core.Services;

public class GameFileService : IGameFileService
{
    private readonly ICacheService _cacheService;
    private readonly IVSSectorParser _sectorParser;
    
    private readonly ILoggerService _loggerService = new SerilogWrapper();
    private readonly IProgressService<double> _progressService = new ProgressService<double>();
    public ArchiveManager ArchiveManager { get; }
    private readonly HashService _hashService;
    private readonly HookService _hookService;
    private readonly Red4ParserService _red4ParserService;
    private readonly GeometryCacheService _geometryCacheService;
    
    private CacheDatabase _readCacheTarget;
    private bool _useModdedResources;
    private string _gameExePath;
    
    public GameFileService(ICacheService cacheService, string gameExePath, bool useModdedResources = false)
    {
        _gameExePath = gameExePath;
        _useModdedResources = useModdedResources;
        
        _cacheService = cacheService;
        _sectorParser = new VSSectorParser(this, cacheService);
        
        _hashService = new HashService();
        _hookService = new HookService();
        _red4ParserService = new Red4ParserService(
            _hashService,
            _loggerService,
            _hookService);
        ArchiveManager = new ArchiveManager(
            _hashService,
            _red4ParserService,
            _loggerService,
            _progressService
        );
        _geometryCacheService = new GeometryCacheService(
            ArchiveManager,
            _red4ParserService
        );
    }

    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;

    public void Initialize()
    {
        if (_isInitialized) return;
        
        ArchiveManager.Initialize(new FileInfo(_gameExePath), _useModdedResources);
        _readCacheTarget = _useModdedResources ? CacheDatabase.AllDatabases : CacheDatabase.Vanilla;
        InitializeCNames();

        _isInitialized = true;
    }

    /// <summary>
    /// Initializes the CNamePool with CNames expected to be encountered.
    /// </summary>
    private static void InitializeCNames()
    {
        foreach (var entry in CollisionGenerics.OriginalMaterials)
        {
            CNamePool.AddOrGetHash(entry);
        }

        foreach (var entry in CollisionGenerics.Presets)
        {
            CNamePool.AddOrGetHash(entry);
        }
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
        
        var cachedResource = _cacheService.GetEntry(new ReadCacheRequest(path, _readCacheTarget));
        if (cachedResource is { Status: ResultStatus.Ok, Data: not null } &&
            MessagePackHelper.TryDeserialize<T>(cachedResource.Data, out var cached))
        {
            result.Data = cached;
            result.Status = ResultStatus.Ok;
            return result;
        }

        var rawResource = ArchiveManager.GetCR2WFile(path);
        if (rawResource == null)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage("Failed to get CR2W file at specified filepath");
            return result;
        }
        
        var db = _useModdedResources && ArchiveManager.Lookup(path, ArchiveManagerScope.Mods) != null ? CacheDatabase.Modded : CacheDatabase.Vanilla;

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

            if (db != CacheDatabase.Modded || _useModdedResources)
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

        if (db != CacheDatabase.Modded || _useModdedResources)
            _cacheService.WriteEntry(path, parsedSector.Data!, db);
        
        return result;
    }
}