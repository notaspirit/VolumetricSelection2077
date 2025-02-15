using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
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
    
    // only the parsing 
    public ConcurrentBag<TimeSpan> SectorParsing { get; set; } = new();
    public ConcurrentBag<TimeSpan> MeshGlbParsing { get; set; } = new();
    public ConcurrentBag<TimeSpan> MeshJsonParsing { get; set; } = new();
    
    // the serialization within game file service
    public ConcurrentBag<TimeSpan> SerializingToSectorJson { get; set; } = new();
    public ConcurrentBag<TimeSpan> SerializingToMeshGlb { get; set; } = new();
    public ConcurrentBag<TimeSpan> SerializingToMeshJson { get; set; } = new();
    
    // entire game file getting
    
    public ConcurrentBag<TimeSpan> GetSector { get; set; } = new(); 
    public ConcurrentBag<TimeSpan> GetMeshGlb { get; set; } = new(); 
    public ConcurrentBag<TimeSpan> GetMeshJson { get; set; } = new(); 
    
    
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
    
    
    public async void RunBenchmarks()
    {
        Logger.Info("Starting Benchmarks");
        var processingService = new ProcessService();
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
                var (status, error) =  await processingService.MainProcessTask(file, benchOutputDir);
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
        
        float GetPercentage(TimeSpan time)
        {
            return (float)Math.Round((double)(time.Milliseconds / totalProcssTime.Milliseconds) * 100);
        }
        
        var totalTimeParsingSector = TimeSpan.Zero;
        foreach (var timespan in SectorParsing)
        {
            totalTimeParsingSector += timespan;
        }
        
        var totalTimeSerializingSectorJson = TimeSpan.Zero;
        foreach (var timespan in SerializingToSectorJson)
        {
            totalTimeSerializingSectorJson += timespan;
        }
        
        var totalTimeGetingSector= TimeSpan.Zero;
        foreach (var timespan in GetSector)
        {
            totalTimeGetingSector += timespan;
        }
        
        Logger.Info("Sector Stats:");
        Logger.Info($"Parsing: total: {UtilService.FormatElapsedTime(totalTimeParsingSector)} ({GetPercentage(totalTimeParsingSector)}% of total time), on average: {UtilService.FormatElapsedTime(totalTimeParsingSector / SectorParsing.Count)}.");
        Logger.Info($"Serializing to Json: total: {UtilService.FormatElapsedTime(totalTimeSerializingSectorJson)} ({GetPercentage(totalTimeSerializingSectorJson)}% of total time), on average: {UtilService.FormatElapsedTime(totalTimeSerializingSectorJson / SerializingToSectorJson.Count)}.");
        Logger.Info($"Getting Gamefile (without parsing, but with serialization): total: {UtilService.FormatElapsedTime(totalTimeGetingSector)} ({GetPercentage(totalTimeGetingSector)}% of total time), on average: {UtilService.FormatElapsedTime(totalTimeGetingSector / GetSector.Count)}.");
        Logger.Info("\n");
        
        var totalTimeParsingMeshJson = TimeSpan.Zero;
        foreach (var timespan in MeshJsonParsing)
        {
            totalTimeParsingMeshJson += timespan;
        }
        
        var totalTimeSerializingMeshJson = TimeSpan.Zero;
        foreach (var timespan in SerializingToMeshJson)
        {
            totalTimeSerializingMeshJson += timespan;
        }
        
        var totalTimeGetingMeshJson= TimeSpan.Zero;
        foreach (var timespan in GetMeshJson)
        {
            totalTimeGetingMeshJson += timespan;
        }
        
        Logger.Info("Mesh Json Stats:");
        Logger.Info($"Parsing: total: {UtilService.FormatElapsedTime(totalTimeParsingMeshJson)} ({GetPercentage(totalTimeParsingMeshJson)}% of total time), on average: {UtilService.FormatElapsedTime(totalTimeParsingMeshJson / MeshJsonParsing.Count)}.");
        Logger.Info($"Serializing to Json: total: {UtilService.FormatElapsedTime(totalTimeSerializingMeshJson)} ({GetPercentage(totalTimeSerializingMeshJson)}% of total time), on average: {UtilService.FormatElapsedTime(totalTimeSerializingMeshJson / SerializingToMeshJson.Count)}.");
        Logger.Info($"Getting Gamefile (without parsing, but with serialization): total: {UtilService.FormatElapsedTime(totalTimeGetingMeshJson)} ({GetPercentage(totalTimeGetingMeshJson)}% of total time), on average: {UtilService.FormatElapsedTime(totalTimeGetingMeshJson / GetMeshJson.Count)}.");
        Logger.Info("\n");
        
        var totalTimeParsingMeshGlb = TimeSpan.Zero;
        foreach (var timespan in MeshGlbParsing)
        {
            totalTimeParsingMeshGlb += timespan;
        }
        
        var totalTimeSerializingMeshGlb = TimeSpan.Zero;
        foreach (var timespan in SerializingToMeshGlb)
        {
            totalTimeSerializingMeshGlb += timespan;
        }
        
        var totalTimeGettingMeshGlb = TimeSpan.Zero;
        foreach (var timespan in GetMeshGlb)
        {
            totalTimeGettingMeshGlb += timespan;
        }
        
        Logger.Info("Mesh Glb Stats:");
        Logger.Info($"Parsing: total: {UtilService.FormatElapsedTime(totalTimeParsingMeshGlb)} ({GetPercentage(totalTimeParsingMeshGlb)}% of total time), on average: {UtilService.FormatElapsedTime(totalTimeParsingMeshGlb / MeshGlbParsing.Count)}.");
        Logger.Info($"Serializing to Glb: total: {UtilService.FormatElapsedTime(totalTimeSerializingMeshGlb)} ({GetPercentage(totalTimeSerializingMeshGlb)}% of total time), on average: {UtilService.FormatElapsedTime(totalTimeSerializingMeshGlb / SerializingToMeshGlb.Count)}.");
        Logger.Info($"Getting Gamefile (without parsing, but with serialization): total: {UtilService.FormatElapsedTime(totalTimeGettingMeshGlb)} ({GetPercentage(totalTimeGettingMeshGlb)}% of total time), on average: {UtilService.FormatElapsedTime(totalTimeGettingMeshGlb / GetMeshGlb.Count)}.");
        Logger.Info("\n");

        var resultsFile = new statsJsonFormat()
        {
            individualTimes = results,
            totalTime = UtilService.FormatElapsedTime(totalProcssTime),
            averageTime = UtilService.FormatElapsedTime(avgProcssTime)
        };

        string serializedResults = JsonConvert.SerializeObject(resultsFile,
            new JsonSerializerSettings()
                { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented });

        string outPath = Path.Combine(benchOutputDir, "results.json");
        File.WriteAllText(outPath, serializedResults);
        Logger.Info($"Wrote results to {outPath}");
        Logger.Info("Finished Benchmarking");
    }
}