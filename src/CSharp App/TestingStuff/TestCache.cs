using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Documents;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class TestCache : IDebugTool
{
    private readonly GameFileService _gameFileService;
    private readonly CacheService _cacheService;

    public TestCache(GameFileService gameFileService, CacheService cacheService)
    {
        _gameFileService = gameFileService;
        _cacheService = cacheService;
    }

    public void Run()
    {
        Logger.Info("Starting cache testing...");

        var gfs = _gameFileService;
        var cs = _cacheService;
        string testMeshPath = @"base\environment\decoration\medical\accessories\medkit\medkit_d_mobile.mesh";
        string testSectorPath = @"base\worlds\03_night_city\_compiled\default\exterior_-4_-8_0_4.streamingsector";

        cs.StartListening();
        cs.ClearDatabase(CacheDatabases.Modded);
        cs.ClearDatabase(CacheDatabases.Vanilla);
        
        Logger.Info("Getting CMesh without cache...");
        var swGetMeshReg = new Stopwatch();
        swGetMeshReg.Start();

        AbbrMesh? regularCMesh = null;
        try
        { 
            regularCMesh = gfs.GetCMesh(testMeshPath).Resource as AbbrMesh;
        }
        catch (Exception e)
        {
            Logger.Error($"{e}");
        }

        swGetMeshReg.Stop();
        Logger.Info($"Got CMesh regular time: {swGetMeshReg.ElapsedMilliseconds} ms");

        Task.Delay(2000).Wait();
        
        Logger.Info("Getting CMesh with cache...");
        var swGetMeshCache = new Stopwatch();
        swGetMeshCache.Start();
        
        var cachedCMesh = gfs.GetCMesh(testMeshPath).Resource as AbbrMesh;

        swGetMeshCache.Stop();
        Logger.Info($"Got CMesh cached time: {swGetMeshCache.ElapsedMilliseconds} ms");
        Logger.Info($"cached mesh is correct: {cachedCMesh == regularCMesh}");
        
        Logger.Info("Getting Sector without cache...");
        var swGetSectorReg = new Stopwatch();
        swGetSectorReg.Start();
        
        var regularSector = gfs.GetSector(testSectorPath).Resource as AbbrSector;

        swGetSectorReg.Stop();
        Logger.Info($"Got Sector regular time: {swGetSectorReg.ElapsedMilliseconds} ms");

        Task.Delay(2000).Wait();
        
        Logger.Info("Getting Sector with cache...");
        var swGetSectorCache = new Stopwatch();
        swGetSectorCache.Start();
        
        var cachedSector = gfs.GetSector(testSectorPath).Resource as AbbrSector;

        swGetSectorCache.Stop();
        Logger.Info($"Got Sector cached time: {swGetSectorCache.ElapsedMilliseconds} ms");
        Logger.Info($"cached sector is correct: {cachedSector == regularSector}");
        
        cs.WriteEntry(new WriteCacheRequest("YesSIR", Encoding.UTF8.GetBytes("value"), CacheDatabases.Vanilla));
        Task.Delay(1000).Wait();
        var result = cs.GetEntry(new ReadCacheRequest("YesSIR", CacheDatabases.Vanilla));
        if (result == null)
        {
            Logger.Error("Failed to get entry");
        }
        else
        {
            Logger.Success("Got test entry");
        }
        cs.StopListening();

        
        cs.StopListening();
        Logger.Info("Reset Database and concluded tests");
    }

    public static void Run2()
    {
        var cs = App.AppHost!.Services.GetRequiredService<CacheService>();
        cs.StartListening();
        cs.WriteEntry(new WriteCacheRequest("YesSIR", Encoding.UTF8.GetBytes("value"), CacheDatabases.Vanilla));
        Task.Delay(1000).Wait();
        var result = cs.GetEntry(new ReadCacheRequest("YesSIR", CacheDatabases.Vanilla));
        if (result == null)
        {
            Logger.Error("Failed to get entry");
        }
        else
        {
            Logger.Success("Got test entry");
        }
        cs.StopListening();

        var testSector = cs.GetEntry(
            new ReadCacheRequest("base\\worlds\\03_night_city\\_compiled\\default\\exterior_-4_-8_0_4.streamingsector"));
        Logger.Info($"{testSector?.Length}");
        
        var testMesh = cs.GetEntry(
            new ReadCacheRequest("base\\environment\\decoration\\medical\\accessories\\medkit\\medkit_d_mobile.mesh"));
        Logger.Info($"{testMesh?.Length}");
    }
}