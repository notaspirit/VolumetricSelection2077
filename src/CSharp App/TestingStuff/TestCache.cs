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
        cs.DropDatabase(CacheDatabases.Vanilla);
        
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

        cs.DropDatabase(CacheDatabases.Vanilla);
        cs.StopListening();
        Logger.Info("Reset Database and concluded tests");
    }
}