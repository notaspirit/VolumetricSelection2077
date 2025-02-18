using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
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
    }
    private void Initialize()
    {
        var gameExePath = new FileInfo(_settingsService.GameDirectory + @"\bin\x64\Cyberpunk2077.exe");
        _archiveManager.Initialize(gameExePath);
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

    public (bool, string, string?) GetGameFileAsJsonString(string filename)
    {
        if (!ulong.TryParse(filename, out var hash))
        {
            hash = FNV1A64HashAlgorithm.HashString(filename);
        }
        CR2WFile? gameFileRaw = _archiveManager.GetCR2WFile(hash, false, false);
        if (gameFileRaw == null)
        {
            return (false, "Failed to get gamefile from archives!", null);
        }
        var dto = new RedFileDto(gameFileRaw);
        string gameFileJson = RedJsonSerializer.Serialize(dto);
        return (true, "", gameFileJson);
    }

    public (bool, string, ModelRoot?) GetGameFileAsGlb(string filename)
    {
        CR2WFile? gameFileRaw = _archiveManager.GetCR2WFile(filename, false, false);
        if (gameFileRaw == null)
        {
            return (false, "Failed to get gamefile from archives!", null);
        }
        var resultModel = MeshTools.GetModel(gameFileRaw, false, false);
        if (resultModel == null)
        {
            return (false, "Failed to convert CR2W file to Glb!", null);
        }
        return (true, "", resultModel);
    }

    public (bool, string, string?) GetGeometryFromCache(string sectorHashString, string actorHashString)
    {
        if (!ulong.TryParse(sectorHashString, out ulong sectorHash) ||
            !ulong.TryParse(actorHashString, out ulong actorHash))
        {
            return (false, "Failed to parse input into ulong!", null);
        }
        var cacheEntry = _geometryCacheService.GetEntry(sectorHash, actorHash);
        if (cacheEntry == null)
        {
            return (false, "Failed to get geometry from archives!", null);
        }
        string outJson =  JsonSerializer.Serialize((object)cacheEntry, new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });
        return (true, "", outJson);
    }
    
    public async Task<(bool, string, string?)> GetGeometryFromCacheAsync(string sectorHashString, string actorHashString)
    {
        if (!ulong.TryParse(sectorHashString, out ulong sectorHash) ||
            !ulong.TryParse(actorHashString, out ulong actorHash))
        {
            return (false, "Failed to parse input into ulong!", null);
        }
        var cacheEntry = await _geometryCacheService.GetEntryAsync(sectorHash, actorHash);
        if (cacheEntry == null)
        {
            return (false, "Failed to get geometry from archives!", null);
        }
        string outJson =  JsonSerializer.Serialize((object)cacheEntry, new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });
        return (true, "", outJson);
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

    public AbbrMesh? GetCMesh(string path)
    {
        var rawMesh = _archiveManager.GetCR2WFile(path);
        if (rawMesh == null)
        {
            return null;
        }
        
        return DirectAbbrMeshParser.ParseFromCR2W(rawMesh);
    }

    public AbbrSector? GetSector(string path)
    {
        var rawSector = _archiveManager.GetCR2WFile(path);
        if (rawSector == null)
        {
            return null;
        }
        return DirectAbbrSectorParser.ParseFromCR2W(rawSector);
    }
}