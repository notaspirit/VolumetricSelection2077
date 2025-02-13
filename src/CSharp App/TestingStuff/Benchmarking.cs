using System;
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
    
    public static async void RunBenchmarks()
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
                var (status, error) =  await processingService.Process(file, benchOutputDir);
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