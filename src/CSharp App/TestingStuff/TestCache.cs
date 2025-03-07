using System;
using System.Diagnostics;
using Avalonia.Controls.Documents;
using Serilog;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class TestCache
{
    public static async void Run()
    {
        Logger.Info("Starting cache testing...");

        var gfs = GameFileService.Instance;
        
        string testMeshPath = @"base\environment\decoration\medical\accessories\medkit\medkit_d_mobile.mesh";
        string testSectorPath = @"base\worlds\03_night_city\_compiled\default\exterior_-4_-8_0_4.streamingsector";


        CacheService.Instance.StartListening();
        
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

        Logger.Info("Getting CMesh with cache...");
        var swGetMeshCache = new Stopwatch();
        swGetMeshCache.Start();
        
        var cachedCMesh = await gfs.GetCMesh(testMeshPath);

        swGetMeshCache.Stop();
        Logger.Info($"Got Sector regular time: {swGetMeshCache.ElapsedMilliseconds} ms");
        Logger.Info($"cached mesh is correct: {cachedCMesh == regularCMesh}");
        
        Logger.Info("Getting Sector without cache...");
        var swGetSectorReg = new Stopwatch();
        swGetSectorReg.Start();
        
        var regularSector = await gfs.GetSector(testSectorPath);

        swGetSectorReg.Stop();
        Logger.Info($"Got Sector regular time: {swGetSectorReg.ElapsedMilliseconds} ms");

        Logger.Info("Getting Sector with cache...");
        var swGetSectorCache = new Stopwatch();
        swGetSectorCache.Start();
        
        var cachedSector = await gfs.GetSector(testSectorPath);

        swGetSectorCache.Stop();
        Logger.Info($"Got Sector regular time: {swGetSectorCache.ElapsedMilliseconds} ms");
        Logger.Info($"cached sector is correct: {cachedSector == regularSector}");

        CacheService.Instance.DropDatabases(CacheDatabases.Vanilla);
        CacheService.Instance.StopListening();
        Logger.Info("Reset Database and concluded tests");
    }
}