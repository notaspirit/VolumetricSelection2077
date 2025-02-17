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
            DirectAbbrSectorParser.Parse(testSectorCR2W);
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
            DirectAbbrSectorParser.Parse(testSectorCR2W);
        }
        catch (Exception e)
        {
            Logger.Error(e.Message);
        }
        sw2.Stop();
        Logger.Info($"Elapsed: {sw2.ElapsedMilliseconds} ms");
    }
}