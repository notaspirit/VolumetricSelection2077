using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MessagePack;
using WolvenKit;
using WolvenKit.Common.Services;
using WolvenKit.Core.Interfaces;
using WolvenKit.Core.Services;
using WolvenKit.RED4.CR2W.Archive;
using WolvenKit.RED4.CR2W;
using SharpGLTF.Schema2;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Parsers;
using WolvenKit.Common;
using WolvenKit.Common.Conversion;
using WolvenKit.Common.FNV1A;
using WolvenKit.Common.PhysX;
using WolvenKit.Modkit.RED4.Tools;
using WolvenKit.RED4.Archive.CR2W;
using WolvenKit.RED4.CR2W.JSON;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace VolumetricSelection2077.Services;

public class GameFileService
{
    private static GameFileService? _instance;
    private static readonly object _lock = new object();
    private readonly ILoggerService _loggerService = new SerilogWrapper();
    private readonly IProgressService<double> _progressService = new ProgressService<double>();
    private ArchiveManager? _archiveManager;
    private readonly SettingsService _settingsService;
    private HashService? _hashService;
    private HookService? _hookService;
    private Red4ParserService? _red4ParserService;
    private GeometryCacheService? _geometryCacheService;
    private CacheService? _cacheService;
    private bool _initialized;
    private CacheDatabases? _readCacheTarget;
    public bool IsInitialized
    {
        get => _initialized;
    }
    
    private GameFileService()
    {
        _settingsService = SettingsService.Instance;
    }
    public void Initialize()
    {
        if (_initialized) return;
        if (_settingsService.SupportModdedResources)
        {
            Logger.Info($"Initializing Game File Service with modded resources, this may take a while...");
        }
        else
        {
            Logger.Info($"Initializing Game File Service...");
        }

        var sw = Stopwatch.StartNew();
        _hashService = new HashService();
        _hookService = new HookService();
        _red4ParserService = new Red4ParserService(
            _hashService,
            _loggerService,
            _hookService);
        _archiveManager = new ArchiveManager(
            _hashService,
            _red4ParserService,
            _loggerService,
            _progressService
        );
        _geometryCacheService = new GeometryCacheService(
            _archiveManager,
            _red4ParserService
        );
        var gameExePath = new FileInfo(_settingsService.GameDirectory + @"\bin\x64\Cyberpunk2077.exe");
        _archiveManager.Initialize(gameExePath, _settingsService.SupportModdedResources);
        _cacheService = CacheService.Instance;
        _readCacheTarget = _settingsService.SupportModdedResources ? CacheDatabases.All : CacheDatabases.Vanilla;
        _initialized = true;
        sw.Stop();
        Logger.Success($"Initialized Game File Service in {UtilService.FormatElapsedTime(sw.Elapsed)}");
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

    public AbbrMesh? GetCMesh(string path)
    {
        if (!_initialized) throw new Exception("GameFileService must be initialized before calling GetCMesh.");
        var sw = Stopwatch.StartNew();
        var cachedMesh = _cacheService.GetEntry(new ReadRequest(path,CacheDatabases.Vanilla));
        sw.Stop();
        // Logger.Info($"Took to {sw.ElapsedMilliseconds}ms to get raw cache entry.");
        // Logger.Info($"Cached mesh is: {cachedMesh}");
        if (cachedMesh != null)
        {
            return MessagePackSerializer.Deserialize<AbbrMesh>(cachedMesh);
        }
        var rawMesh = _archiveManager.GetCR2WFile(path);
        if (rawMesh == null)
        {
            return null;
        }
        CacheDatabases db = CacheDatabases.Vanilla;
        if (_settingsService.SupportModdedResources && _settingsService.CacheModdedResources)
        {
            var fileLookup = _archiveManager.Lookup(path, ArchiveManagerScope.Mods);
            if (fileLookup != null)
            {
                db = CacheDatabases.Modded;
            }
        }
        var parsedMesh = DirectAbbrMeshParser.ParseFromCR2W(rawMesh);
        _cacheService.WriteMeshEntry(path, parsedMesh, db);
        return parsedMesh;
    }

    public AbbrSector? GetSector(string path)
    {
        if (!_initialized) throw new Exception("GameFileService must be initialized before calling GetSector.");
        var sw = Stopwatch.StartNew();
        var cachedSector = _cacheService.GetEntry(new ReadRequest(path, CacheDatabases.Vanilla));
        sw.Stop();
        // Logger.Info($"Took to {sw.ElapsedMilliseconds}ms to get raw cache entry.");
        // Logger.Info($"Cached sector is: {cachedSector}");
        if (cachedSector != null)
        {
            return MessagePackSerializer.Deserialize<AbbrSector>(cachedSector);
        }
        var rawSector = _archiveManager.GetCR2WFile(path);
        if (rawSector == null)
        {
            return null;
        }
        CacheDatabases db = CacheDatabases.Vanilla;
        if (_settingsService.SupportModdedResources && _settingsService.CacheModdedResources)
        {
            var fileLookup = _archiveManager.Lookup(path, ArchiveManagerScope.Mods);
            if (fileLookup != null)
            {
                db = CacheDatabases.Modded;
            }
        }
        var parsedSector = DirectAbbrSectorParser.ParseFromCR2W(rawSector);
        _cacheService.WriteSectorEntry(path, parsedSector, db);
        return parsedSector;
    }
}