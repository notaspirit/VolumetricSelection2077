using System;
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
    private readonly ArchiveManager _archiveManager;
    private readonly SettingsService _settingsService;
    private readonly HashService _hashService;
    private readonly HookService _hookService;
    private readonly Red4ParserService _red4ParserService;
    private readonly GeometryCacheService _geometryCacheService;
    private CacheService _cacheService;
    
    private GameFileService()
    {
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
        _settingsService = SettingsService.Instance;
        _geometryCacheService = new GeometryCacheService(
            _archiveManager,
            _red4ParserService
        );
        _cacheService = CacheService.Instance;
    }
    private void Initialize()
    {
        var gameExePath = new FileInfo(_settingsService.GameDirectory + @"\bin\x64\Cyberpunk2077.exe");
        _archiveManager.Initialize(gameExePath, false);
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
                    _instance.Initialize();
                }
                return _instance;
            }
        }
    }
    public async Task<AbbrMesh?> GetPhysXMesh(ulong sectorHash, ulong actorHash)
    {
        var rawMesh = await _geometryCacheService.GetEntryAsync(sectorHash, actorHash);
        if (rawMesh == null)
        {
            return null;
        }
        
        return DirectAbbrMeshParser.ParseFromPhysX(rawMesh);
    }

    public async Task<AbbrMesh?> GetCMesh(string path)
    {
        var cachedMesh = await _cacheService.GetEntry(new ReadRequest(path));
        if (cachedMesh != null)
        {
            Logger.Info($"{cachedMesh}");
            return MessagePackSerializer.Deserialize<AbbrMesh>(cachedMesh);
        }
        var rawMesh = _archiveManager.GetCR2WFile(path);
        if (rawMesh == null)
        {
            return null;
        }
        var parsedMesh = DirectAbbrMeshParser.ParseFromCR2W(rawMesh);
        _cacheService.WriteEntry(new WriteRequest(path, MessagePackSerializer.Serialize(parsedMesh)));
        return parsedMesh;
    }

    public async Task<AbbrSector?> GetSector(string path)
    {
        var cachedSector = await _cacheService.GetEntry(new ReadRequest(path));
        if (cachedSector != null)
        {
            Logger.Info($"{cachedSector}");
            return MessagePackSerializer.Deserialize<AbbrSector>(cachedSector);;
        }
        var rawSector = _archiveManager.GetCR2WFile(path);
        if (rawSector == null)
        {
            return null;
        }

        var parsedSector = DirectAbbrSectorParser.ParseFromCR2W(rawSector);
        _cacheService.WriteEntry(new WriteRequest(path, MessagePackSerializer.Serialize(parsedSector)));
        return parsedSector;
    }
}