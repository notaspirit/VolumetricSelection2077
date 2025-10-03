using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VolumetricSelection2077.Json.Helpers;
using VolumetricSelection2077.Services;
using WolvenKit.RED4.Types;

namespace VolumetricSelection2077.TestingStuff;

public class Benchmarking
{
    private class statsJsonFormat
    {
        public List<KeyValuePair<string, TimeSpan>> individualTimes { get; set; }
        public string totalTime { get; set; }
        public string averageTime { get; set; }
    }
    
    private static Benchmarking? _instance;
    private static readonly object _lock = new object();
    
    public static Benchmarking Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new Benchmarking();
                }
                return _instance;
            }
        }
    }
    
    public ConcurrentBag<double> GetFileMs = new();
    public ConcurrentBag<double> CheckIntersectionMs = new();
    
    
    
    
    public async Task RunBenchmarks(DialogService dialogService)
    {
        Logger.Info("Starting Benchmarks");
        // CacheService.Instance.DropDatabase(CacheDatabases.Vanilla);
        var processingService = new ProcessDispatcher(dialogService);
        string benchFileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VolumetricSelection2077", "benchmarks");
        string benchOutputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VolumetricSelection2077", "benchmarks", "results", DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss"));
        
        Directory.CreateDirectory(benchOutputDir);
        Directory.CreateDirectory(benchFileDir);
        
        var files = Directory.GetFiles(benchFileDir);
        if (files.Length == 0)
        {
            Logger.Error("No files found in benchmark directory!");
        }

        List<KeyValuePair<string, TimeSpan>> results = new();
        
        foreach (var file in files)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var (status, error) =  await processingService.StartProcess(file, benchOutputDir);
                if (status != true || error != "")
                {
                    throw new Exception($"Error: {error}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to processes: {file} with exception: {ex}");
            }
            stopwatch.Stop();
            string fileName = Path.GetFileNameWithoutExtension(file);
            Logger.Info($"Processed {fileName} in {UtilService.FormatElapsedTime(stopwatch.Elapsed)}.");
            results.Add(new(fileName, stopwatch.Elapsed));
        }

        TimeSpan totalProcssTime = TimeSpan.Zero;
        foreach (var result in results)
        {
            totalProcssTime += result.Value;
        }
        Logger.Info($"Total procss: {UtilService.FormatElapsedTime(totalProcssTime)}");
        TimeSpan avgProcssTime = totalProcssTime / results.Count;
        Logger.Info($"Average process time: {UtilService.FormatElapsedTime(avgProcssTime)}");

        double CalcPercent(double timeSpend, double totalTime)
        {
            return timeSpend / totalTime * 100;
        }

        double CalcAvg(double time, int occurrence)
        {
            return time / occurrence;
        }
        
        double totalTimeMs = totalProcssTime.TotalMilliseconds;

        double getFilesTotal = GetFileMs.Sum();

        double checkTotal = CheckIntersectionMs.Sum();
        
        Logger.Info($"\n" +
                    $"Getting Files:" +
                    $"Total time spend: {getFilesTotal}ms\n" +
                    $"Average time spend: {CalcAvg(getFilesTotal, GetFileMs.Count)}ms\n" +
                    $"Percentage time spend: {CalcPercent(getFilesTotal, totalTimeMs)}%");
        
        Logger.Info($"\n" +
                    $"Getting Files:" +
                    $"Total time spend: {checkTotal}ms\n" +
                    $"Average time spend: {CalcAvg(checkTotal, CheckIntersectionMs.Count)}ms\n" +
                    $"Percentage time spend: {CalcPercent(checkTotal, totalTimeMs)}%");
        
        var resultsFile = new statsJsonFormat()
        {
            individualTimes = results,
            totalTime = UtilService.FormatElapsedTime(totalProcssTime),
            averageTime = UtilService.FormatElapsedTime(avgProcssTime)
        };

        string serializedResults = JsonConvert.SerializeObject(resultsFile, JsonSerializerPresets.Default);

        string outPath = Path.Combine(benchOutputDir, "results.json");
        File.WriteAllText(outPath, serializedResults);
        Logger.Info($"Wrote results to {outPath}");
        Logger.Info("Finished Benchmarking");
    }
}