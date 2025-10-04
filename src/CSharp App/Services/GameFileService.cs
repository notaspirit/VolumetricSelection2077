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
    /// <exception cref="Exception">Game file service is not initialized</exception>
    /// <remarks>Collision meshes are not cached</remarks>
    public async Task<AbbrMesh?> GetPhysXMesh(ulong sectorHash, ulong actorHash)
    {
        if (!_initialized) throw new Exception("GameFileService must be initialized before calling GetPhysXMesh.");
        var rawMesh = await _geometryCacheService.GetEntryAsync(sectorHash, actorHash);
        if (rawMesh == null)
        {
            return null;
        }
        
        return DirectAbbrMeshParser.ParseFromPhysX(rawMesh);
    }
    
    /// <summary>
    /// Gets a CMesh from the archives or cache and returns the parsed AbbrMesh
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="Exception">Game file service is not initialized</exception>
    public AbbrMesh? GetCMesh(string path)
    {
        if (!_initialized) throw new Exception("GameFileService must be initialized before calling GetCMesh.");

        var cachedMesh = _cacheService.GetEntry(new ReadCacheRequest(path, _readCacheTarget));
        if (MessagePackHelper.TryDeserialize<AbbrMesh>(cachedMesh, out var mesh)) return mesh;
        
        var rawMesh = ArchiveManager.GetCR2WFile(path);
        if (rawMesh == null) return null;
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
            return null;
        }
        if (parsedMesh == null) return null;

        if (!_settingsService.CacheModdedResources && db == CacheDatabases.Modded) return parsedMesh;
        _cacheService.WriteEntry(path, parsedMesh, db); 
        
        return parsedMesh;
    }
    
    /// <summary>
    /// Gets a worldStreamingSector from the archives or cache and returns the parsed AbbrSector
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="Exception">Game file service is not initialized</exception>
    public AbbrSector? GetSector(string path)
    {
        if (!_initialized) throw new Exception("GameFileService must be initialized before calling GetCMesh.");
        var cachedSector = _cacheService.GetEntry(new ReadCacheRequest(path, _readCacheTarget));
        if (MessagePackHelper.TryDeserialize<AbbrSector>(cachedSector, out var mesh))
        {
            return mesh;
        }
        
        var rawSector = ArchiveManager.GetCR2WFile(path);
        if (rawSector == null) return null;
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
            return null;
        }

        if (!_settingsService.CacheModdedResources && db == CacheDatabases.Modded) return parsedSector;
        _cacheService.WriteEntry(path, parsedSector, db);
        return parsedSector;
    }
}