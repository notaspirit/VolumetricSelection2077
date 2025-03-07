using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls.Documents;
using Serilog;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class TestCache
{
    public static async Task Run()
    {
        Logger.Info("Starting cache testing...");

        var gfs = GameFileService.Instance;
        var cs = CacheService.Instance;
        string testMeshPath = @"base\environment\decoration\medical\accessories\medkit\medkit_d_mobile.mesh";
        string testSectorPath = @"base\worlds\03_night_city\_compiled\default\exterior_-4_-8_0_4.streamingsector";

        string testMesh2 =
            @"ep1\worlds\03_night_city\sectors\_external\proxy\1930979112\hill_park_totem.mesh";
        
        cs.StartListening();
        cs.DropDatabases(CacheDatabases.Vanilla);
        
        Logger.Info("Getting CMesh without cache...");
        var swGetMeshReg = new Stopwatch();
        swGetMeshReg.Start();

        AbbrMesh? regularCMesh = null;
        try
        { 
            regularCMesh = await gfs.GetCMesh(testMeshPath);
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
        
        var cachedCMesh = await gfs.GetCMesh(testMeshPath);

        swGetMeshCache.Stop();
        Logger.Info($"Got Sector regular time: {swGetMeshCache.ElapsedMilliseconds} ms");
        Logger.Info($"cached mesh is correct: {cachedCMesh == regularCMesh}");
        
        Logger.Info("Getting CMesh2 without cache...");
        var swGetMeshCache2 = new Stopwatch();
        swGetMeshCache2.Start();
        
        var cachedCMesh2 = await gfs.GetCMesh(testMeshPath);

        swGetMeshCache2.Stop();
        Logger.Info($"Got Sector regular time: {swGetMeshCache2.ElapsedMilliseconds} ms");
        
        
        Logger.Info("Getting Sector without cache...");
        var swGetSectorReg = new Stopwatch();
        swGetSectorReg.Start();
        
        var regularSector = await gfs.GetSector(testSectorPath);

        swGetSectorReg.Stop();
        Logger.Info($"Got Sector regular time: {swGetSectorReg.ElapsedMilliseconds} ms");

        Task.Delay(2000).Wait();
        
        Logger.Info("Getting Sector with cache...");
        var swGetSectorCache = new Stopwatch();
        swGetSectorCache.Start();
        
        var cachedSector = await gfs.GetSector(testSectorPath);

        swGetSectorCache.Stop();
        Logger.Info($"Got Sector regular time: {swGetSectorCache.ElapsedMilliseconds} ms");
        Logger.Info($"cached sector is correct: {cachedSector == regularSector}");

        cs.DropDatabases(CacheDatabases.Vanilla);
        cs.StopListening();
        Logger.Info("Reset Database and concluded tests");
    }
}