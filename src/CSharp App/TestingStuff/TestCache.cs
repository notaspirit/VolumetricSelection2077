using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Documents;
using Serilog;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class TestCache : IDebugTool
{
    public void Run()
    {
        Logger.Info("Starting cache testing...");

        var gfs = GameFileService.Instance;
        var cs = CacheService.Instance;
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
            regularCMesh = gfs.GetCMesh(testMeshPath);
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
        
        var cachedCMesh = gfs.GetCMesh(testMeshPath);

        swGetMeshCache.Stop();
        Logger.Info($"Got CMesh cached time: {swGetMeshCache.ElapsedMilliseconds} ms");
        Logger.Info($"cached mesh is correct: {cachedCMesh == regularCMesh}");
        
        Logger.Info("Getting Sector without cache...");
        var swGetSectorReg = new Stopwatch();
        swGetSectorReg.Start();
        
        var regularSector = gfs.GetSector(testSectorPath);

        swGetSectorReg.Stop();
        Logger.Info($"Got Sector regular time: {swGetSectorReg.ElapsedMilliseconds} ms");

        Task.Delay(2000).Wait();
        
        Logger.Info("Getting Sector with cache...");
        var swGetSectorCache = new Stopwatch();
        swGetSectorCache.Start();
        
        var cachedSector = gfs.GetSector(testSectorPath);

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
        var cs = CacheService.Instance;
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
        Logger.Info($"{testSector?.Length ?? null}");
        
        var testMesh = cs.GetEntry(
            new ReadCacheRequest("base\\environment\\decoration\\medical\\accessories\\medkit\\medkit_d_mobile.mesh"));
        Logger.Info($"{testMesh?.Length ?? null}");
    }
}