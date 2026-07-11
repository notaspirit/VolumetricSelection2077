using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using VolumetricSelection2077.Enums;
using WolvenKit;
using WolvenKit.Common.Services;
using WolvenKit.Core.Interfaces;
using WolvenKit.Core.Services;
using WolvenKit.RED4.CR2W.Archive;
using WolvenKit.RED4.CR2W;
using VolumetricSelection2077.MessagePack.Helpers;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Models.WorldBuilder.Spawn.Collision;
using VolumetricSelection2077.Parsers;
using WolvenKit.Common;
using WolvenKit.RED4.Types.Pools;

namespace VolumetricSelection2077.Services;

public class GameFileService
{
    private readonly CacheService _cacheService;
    private readonly SettingsService _settingsService;
    private readonly DirectAbbrSectorParser _sectorParser;
    
    private readonly ILoggerService _loggerService = new SerilogWrapper();
    private readonly IProgressService<double> _progressService = new ProgressService<double>();
    public readonly ArchiveManager ArchiveManager;
    private readonly HashService _hashService;
    private readonly HookService _hookService;
    private readonly Red4ParserService _red4ParserService;
    private readonly GeometryCacheService _geometryCacheService;
    
    private CacheDatabases _readCacheTarget;
    
    public GameFileService(SettingsService settingsService, CacheService cacheService)
    {
        _settingsService = settingsService;
        _cacheService = cacheService;
        _sectorParser = new DirectAbbrSectorParser(this, cacheService);
        
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
        
        Logger.Info(_settingsService.Data.SupportModdedResources
            ? "Initializing Game File Service with modded resources, this may take a while..."
            : "Initializing Game File Service...");
        var sw = Stopwatch.StartNew();

        var gameExePath = new FileInfo(Path.Join(_settingsService.Data.GameDirectory, "bin", "x64", "Cyberpunk2077.exe"));
        ArchiveManager.Initialize(gameExePath, _settingsService.Data.SupportModdedResources);
        _readCacheTarget = _settingsService.Data.SupportModdedResources ? CacheDatabases.All : CacheDatabases.Vanilla;
        InitializeCNames();
        
        Logger.Success($"Initialized Game File Service in {Utils.FormatElapsedTime(sw.Elapsed)}");
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
    public async Task<ResourceToken> GetPhysXMesh(ulong sectorHash, ulong actorHash)
    {
        var token = new ResourceToken();
        
        var rawMesh = await _geometryCacheService.GetEntryAsync(sectorHash, actorHash);
        if (rawMesh == null)
        {
            token.Result = ResourceTokenResult.Failure;
            return token;
        }
        
        token.Result = ResourceTokenResult.Success;
        token.Resource = DirectAbbrMeshParser.ParseFromPhysX(rawMesh);

        return token;
    }
    
    /// <summary>
    /// Gets a CMesh from the archives or cache and returns the parsed AbbrMesh
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public ResourceToken GetCMesh(string path)
    {
        var token = new ResourceToken();

        if (_settingsService.Data.RememberFailedResources && _cacheService.IsResourceKnownBad(path))
        {
            token.Result = ResourceTokenResult.KnownBad;
            return token;
        }
        
        var cachedMesh = _cacheService.GetEntry(new ReadCacheRequest(path, _readCacheTarget));
        if (MessagePackHelper.TryDeserialize<AbbrMesh>(cachedMesh, out var mesh))
        {
            token.Result = ResourceTokenResult.Success;
            token.Resource = mesh;
            return token;
        }
        
        var rawMesh = ArchiveManager.GetCR2WFile(path);
        if (rawMesh == null)
        {
            _cacheService.AddKnownBadResource(path);
            token.Result = ResourceTokenResult.Failure;
            return token;
        }
        CacheDatabases db = CacheDatabases.Vanilla;
        if (_settingsService.Data.SupportModdedResources)
        {
            var fileLookup = ArchiveManager.Lookup(path, ArchiveManagerScope.Mods);
            if (fileLookup != null) db = CacheDatabases.Modded;
        }

        AbbrMesh? parsedMesh;
        try
        {
            parsedMesh = DirectAbbrMeshParser.ParseFromCR2W(rawMesh);
        }
        catch (Exception ex)
        {
            Logger.Exception(ex,$"Failed to parse mesh {path}");
            _cacheService.AddKnownBadResource(path);
            token.Result = ResourceTokenResult.Failure;
            return token;
        }

        if (parsedMesh == null)
        {
            token.Result = ResourceTokenResult.Failure;
            return token;
        }
        
        token.Result = ResourceTokenResult.Success;
        token.Resource = parsedMesh;

        if (db != CacheDatabases.Modded || _settingsService.Data.CacheModdedResources)
            _cacheService.WriteEntry(path, parsedMesh, db); 
        
        return token;
    }
    
    /// <summary>
    /// Gets a worldStreamingSector from the archives or cache and returns the parsed AbbrSector
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public ResourceToken GetSector(string path)
    {
        var token = new ResourceToken();
        
        if (_settingsService.Data.RememberFailedResources && _cacheService.IsResourceKnownBad(path))
        {
            token.Result = ResourceTokenResult.KnownBad;
            return token;
        }
        
        var cachedSector = _cacheService.GetEntry(new ReadCacheRequest(path, _readCacheTarget));
        if (MessagePackHelper.TryDeserialize<AbbrSector>(cachedSector, out var sector))
        {
            token.Result = ResourceTokenResult.Success;
            token.Resource = sector;
            return token;
        }
        
        var rawSector = ArchiveManager.GetCR2WFile(path);
        if (rawSector == null)
        {
            _cacheService.AddKnownBadResource(path);
            token.Result = ResourceTokenResult.Failure;
            return token;
        }
        CacheDatabases db = CacheDatabases.Vanilla;
        if (_settingsService.Data.SupportModdedResources)
        {
            var fileLookup = ArchiveManager.Lookup(path, ArchiveManagerScope.Mods);
            if (fileLookup != null) db = CacheDatabases.Modded;
        }

        AbbrSector? parsedSector;
        try
        {
            parsedSector = _sectorParser.ParseFromCR2W(rawSector);
        }
        catch (Exception ex)
        {
            Logger.Exception(ex,$"Failed to parse sector {path}");
            _cacheService.AddKnownBadResource(path);
            token.Result = ResourceTokenResult.Failure;
            return token;
        }
        
        token.Result = ResourceTokenResult.Success;
        token.Resource = parsedSector;
        
        if (db != CacheDatabases.Modded || _settingsService.Data.CacheModdedResources)
            _cacheService.WriteEntry(path, parsedSector, db);
        
        return token;
    }
}