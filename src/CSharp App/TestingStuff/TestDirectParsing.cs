using System;
using System.Diagnostics;
using System.IO;
using VolumetricSelection2077.Parsers;
using VolumetricSelection2077.Services;
using WolvenKit;
using WolvenKit.Common.Services;
using WolvenKit.Core.Interfaces;
using WolvenKit.Core.Services;
using WolvenKit.RED4.CR2W;
using WolvenKit.RED4.CR2W.Archive;

namespace VolumetricSelection2077.TestingStuff;

public class TestDirectParsing
{
    public static void TestSectors()
    {
        string testSector = @"base\worlds\03_night_city\_compiled\default\exterior_-15_-2_0_1.streamingsector";
        
        ILoggerService _loggerService = new SerilogWrapper();
        IProgressService<double> _progressService = new ProgressService<double>();
        
        var _hashService = new HashService();
        var _hookService = new HookService();
        var _red4ParserService = new Red4ParserService(
            _hashService,
            _loggerService,
            _hookService);
        var _archiveManager = new ArchiveManager(
            _hashService,
            _red4ParserService,
            _loggerService,
            _progressService
        );

        var gameExePath = new FileInfo(SettingsService.Instance.GameDirectory + @"\bin\x64\Cyberpunk2077.exe");
        _archiveManager.Initialize(gameExePath);
        Logger.Info($"Getting Sector...");
        var swGet = new Stopwatch();
        swGet.Start();
        var testSectorCR2W = _archiveManager.GetCR2WFile(testSector);
        if (testSectorCR2W == null)
        {
            Logger.Error("Failed to get cr2w file");
            return;
        }
        swGet.Stop();
        Logger.Info($"Elapsed: {swGet.ElapsedMilliseconds} ms");
        
        Logger.Info("First Run...");
        var sw = new Stopwatch();
        sw.Start();
        try
        {
            DirectAbbrSectorParser.ParseFromCR2W(testSectorCR2W);
        }
        catch (Exception e)
        {
            Logger.Error(e.Message);
        }
        sw.Stop();
        Logger.Info($"Elapsed: {sw.ElapsedMilliseconds} ms");
        
        Logger.Info("Second Run...");
        var sw2 = new Stopwatch();
        sw2.Start();
        try
        {
            DirectAbbrSectorParser.ParseFromCR2W(testSectorCR2W);
        }
        catch (Exception e)
        {
            Logger.Error(e.Message);
        }
        sw2.Stop();
        Logger.Info($"Elapsed: {sw2.ElapsedMilliseconds} ms");
    }

    public static void TestPhysXMeshes()
    {
        ILoggerService _loggerService = new SerilogWrapper();
        IProgressService<double> _progressService = new ProgressService<double>();
        
        var _hashService = new HashService();
        var _hookService = new HookService();
        var _red4ParserService = new Red4ParserService(
            _hashService,
            _loggerService,
            _hookService);
        var _archiveManager = new ArchiveManager(
            _hashService,
            _red4ParserService,
            _loggerService,
            _progressService
        );
        
        var gameExePath = new FileInfo(SettingsService.Instance.GameDirectory + @"\bin\x64\Cyberpunk2077.exe");
        _archiveManager.Initialize(gameExePath);
        
        var gfs = new GeometryCacheService(_archiveManager, _red4ParserService);
        
        Logger.Info($"Getting First PhysX Mesh...");
        var sw1 = new Stopwatch();
        sw1.Start();
        var physX1 = gfs.GetEntry(12717457377011094652, 9246134327794375400);
        
        var parsedMesh1 = DirectAbbrMeshParser.ParseFromPhysX(physX1);
        
        sw1.Stop();
        Logger.Info($"Elapsed: {sw1.ElapsedMilliseconds} ms");

        foreach (var vertex in parsedMesh1.SubMeshes[0].Vertices)
        {
            Logger.Info($"Vertex: {vertex}");
        }
        
        Logger.Info($"AABB: {parsedMesh1.SubMeshes[0].BoundingBox.Minimum}, {parsedMesh1.SubMeshes[0].BoundingBox.Maximum}");
        
        Logger.Info($"Getting Second PhysX Mesh...");
        var sw2 = new Stopwatch();
        sw2.Start();
        var physX2 = gfs.GetEntry(12717457377011094652, 9386483786976406912);
        
        var parsedMesh2 = DirectAbbrMeshParser.ParseFromPhysX(physX2);
        
        sw2.Stop();
        Logger.Info($"Elapsed: {sw2.ElapsedMilliseconds} ms");
        
        foreach (var vertex in parsedMesh2.SubMeshes[0].Vertices)
        {
            Logger.Info($"Vertex: {vertex}");
        }
        
        Logger.Info($"AABB: {parsedMesh2.SubMeshes[0].BoundingBox.Minimum}, {parsedMesh2.SubMeshes[0].BoundingBox.Maximum}");
    }

    public static void TestCMeshParser()
    {
        ILoggerService _loggerService = new SerilogWrapper();
        IProgressService<double> _progressService = new ProgressService<double>();
        
        var _hashService = new HashService();
        var _hookService = new HookService();
        var _red4ParserService = new Red4ParserService(
            _hashService,
            _loggerService,
            _hookService);
        var _archiveManager = new ArchiveManager(
            _hashService,
            _red4ParserService,
            _loggerService,
            _progressService
        );
        
        var gameExePath = new FileInfo(SettingsService.Instance.GameDirectory + @"\bin\x64\Cyberpunk2077.exe");
        _archiveManager.Initialize(gameExePath);

        string testMeshPath = @"base\environment\decoration\public_utility\vents\ventilation_fan\ventilation_fan_6m.mesh";
        
        var swGet = new Stopwatch();
        swGet.Start();
        
        var testMeshCR2W = _archiveManager.GetCR2WFile(testMeshPath);
        if (testMeshCR2W == null)
        {
            Logger.Error("Failed to get cr2w file");
            return;
        }
        swGet.Stop();
        Logger.Info($"Elapsed: {swGet.ElapsedMilliseconds} ms");
        
        var sw1 = new Stopwatch();
        sw1.Start();
        
        var parsedMesh = DirectAbbrMeshParser.ParseFromCR2W(testMeshCR2W);
        
        sw1.Stop();
        Logger.Info($"Elapsed: {sw1.ElapsedMilliseconds} ms");
    }
    
}