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
using VolumetricSelection2077.Parsers;
using WolvenKit.Common;

namespace VolumetricSelection2077.Services;

public class GameFileService
{
    private static GameFileService? _instance;
    private static readonly object _lock = new object();
    private readonly ILoggerService _loggerService = new SerilogWrapper();
    private readonly IProgressService<double> _progressService = new ProgressService<double>();
    public ArchiveManager? ArchiveManager;
    private readonly SettingsService _settingsService;
    private HashService? _hashService;
    private HookService? _hookService;
    private Red4ParserService? _red4ParserService;
    private GeometryCacheService? _geometryCacheService;
    private CacheService? _cacheService;
    private bool _initialized;
    private CacheDatabases _readCacheTarget;
    public bool IsInitialized
    {
        get => _initialized;
    }
    
    private GameFileService()
    {
        _settingsService = SettingsService.Instance;
    }
    
    /// <summary>
    ///  Initializes GameFileService if it isn't already
    /// </summary>
    /// <returns>true if operation was successful</returns>
    public bool Initialize()
    {
        if (_initialized) return true;
        if (_settingsService.SupportModdedResources)
        {
            Logger.Info($"Initializing Game File Service with modded resources, this may take a while...");
        }
        else
        {
            Logger.Info($"Initializing Game File Service...");
        }
        
        var sw = Stopwatch.StartNew();
        try
        {
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
            var gameExePath = new FileInfo(_settingsService.GameDirectory + @"\bin\x64\Cyberpunk2077.exe");
            ArchiveManager.Initialize(gameExePath, _settingsService.SupportModdedResources);
            _cacheService = CacheService.Instance;
            _readCacheTarget = _settingsService.SupportModdedResources ? CacheDatabases.All : CacheDatabases.Vanilla;
            _initialized = true;
            sw.Stop();
            Logger.Success($"Initialized Game File Service in {UtilService.FormatElapsedTime(sw.Elapsed)}");
            return true;
        }
        catch (Exception e)
        {
            sw.Stop();
            Logger.Exception(e, "Initializing Game File Service failed!");
            return false;
        }

    }
    public static GameFileService Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new GameFileService();
                }
                return _instance;
            }
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
        
        if (!_initialized)
        {
            token.Result = ResourceTokenResult.NotInitialized;
            return token;
        }
        
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

        if (!_initialized)
        {
            token.Result = ResourceTokenResult.NotInitialized;
            return token;
        }

        if (_settingsService.RememberFailedResources && _cacheService.IsResourceKnownBad(path))
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
        if (_settingsService.SupportModdedResources)
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

        if (db != CacheDatabases.Modded || _settingsService.CacheModdedResources)
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

        if (!_initialized)
        {
            token.Result = ResourceTokenResult.NotInitialized;
            return token;
        }
        
        if (_settingsService.RememberFailedResources && _cacheService.IsResourceKnownBad(path))
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
        if (_settingsService.SupportModdedResources)
        {
            var fileLookup = ArchiveManager.Lookup(path, ArchiveManagerScope.Mods);
            if (fileLookup != null) db = CacheDatabases.Modded;
        }

        AbbrSector? parsedSector;
        try
        {
            parsedSector = DirectAbbrSectorParser.ParseFromCR2W(rawSector);
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
        
        if (db != CacheDatabases.Modded || _settingsService.CacheModdedResources)
            _cacheService.WriteEntry(path, parsedSector, db);
        
        return token;
    }
}