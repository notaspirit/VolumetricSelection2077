using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Json.Helpers;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace VolumetricSelection2077.TestingStuff;

public class Benchmarking
{
    private readonly ValidationService _validationService;
    private readonly DialogService _dialogService;
    private readonly BoundingBoxBuilderService _boundingBoxBuilderService;
    private readonly SettingsService _settings;
    private readonly CacheService _cacheService;
    private readonly Progress _progress;
    private readonly GameFileService _gfs;
    
    private const int BenchmarkCount = 3;
    
    public Benchmarking(DialogService dialogService,
        ValidationService validationService,
        BoundingBoxBuilderService boundingBoxBuilderService,
        SettingsService settings, CacheService cacheService,
        Progress progress,
        GameFileService gameFileService)
    {
        _validationService = validationService;
        _dialogService = dialogService;
        _boundingBoxBuilderService = boundingBoxBuilderService;
        _settings = settings;
        _cacheService = cacheService;
        _progress = progress;
        _gfs = gameFileService;
    }
    
    public async Task Run()
    {
        var benchmarkSourceDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VolumetricSelection2077", "BenchmarkDefinitions");
        var benchmarkResultsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "BenchmarkResults", FormatDateTime(DateTime.Now));
        Directory.CreateDirectory(benchmarkSourceDir);
        Directory.CreateDirectory(benchmarkResultsDir);
        
        var benchmarkResult = new Dictionary<string, List<TimeSpan>>();
        
        var benchmarkDirs = new DirectoryInfo(benchmarkSourceDir).GetDirectories();
        
        for (var i = 0; i < benchmarkDirs.Length; i++)
        {
            var dir = benchmarkDirs[i];
            Logger.Info($"Running benchmark {i + 1} of {benchmarkDirs.Length}...");

            if (dir.Name.EndsWith("skip"))
            {
                Logger.Warning($"Skipping {dir.Name} because it's marked as 'skip'.");
                continue;
            }
            
            var files = dir.GetFiles("*.json");
            if (files.All(f => !f.FullName.EndsWith("settings.json")))
            {
                Logger.Warning($"Skipping {dir.Name} because it doesn't contain a settings.json file.");
                continue;
            }
            
            if (files.All(f => !f.FullName.EndsWith("selection.json")))
            {
                Logger.Warning($"Skipping {dir.Name} because it doesn't contain a selection.json file.");
                continue;
            }

            for (var j = 0; j < BenchmarkCount; j++)
            {
                var result = await RunBenchmark(dir, benchmarkResultsDir);
                TryGetOrCreateValue(benchmarkResult, dir.Name, out var prevResults);
                prevResults.Add(result);
            }
        }
        
        File.WriteAllText(Path.Combine(benchmarkResultsDir, "results.json"), JsonConvert.SerializeObject(benchmarkResult, JsonSerializerPresets.Default));
        Logger.Info($"Benchmarking complete, results saved to {benchmarkResultsDir}.");
    }

    private async Task<TimeSpan> RunBenchmark(DirectoryInfo definitionDir, string resultsDir)
    {
        var sw = new Stopwatch();
        
        var settingsService = BuildAndConfigureSettingsService(definitionDir, resultsDir);
        var outputFilePath = Path.Combine(resultsDir, settingsService.Data.OutputFilename + ".json");
        var processDispatcher = BuildDispatcher(settingsService, outputFilePath);
        
        sw.Start();
        
        await processDispatcher.StartProcess(Path.Combine(definitionDir.FullName, "selection.json"));
        
        sw.Stop();
        return sw.Elapsed;
    }

    private SettingsService BuildAndConfigureSettingsService(DirectoryInfo definitionDir, string resultsDir)
    {
        var settingsService = new SettingsService(Path.Combine(definitionDir.FullName, "settings.json"));
        settingsService.LoadSettings();

        settingsService.Data.SaveFileLocation = SaveFileLocation.OutputDirectory;
        if (settingsService.Data.SaveFileFormat == SaveFileFormat.ArchiveXLYaml)
            settingsService.Data.SaveFileFormat = SaveFileFormat.ArchiveXLJson;
        settingsService.Data.OutputDirectory = resultsDir;
        settingsService.Data.OutputFilename = $"{FormatDateTime(DateTime.Now)}_{definitionDir.Name}";
        return settingsService;
    }

    private ProcessDispatcher BuildDispatcher(SettingsService settingsService, string outputFilePath)
    {
        var postProcessingService = new PostProcessingService(settingsService, _progress, _gfs, outputFilePath);
        var processService = new ProcessService(settingsService, _gfs, _cacheService, postProcessingService, _progress);
        return new ProcessDispatcher(_dialogService, processService, _validationService, _boundingBoxBuilderService,
            settingsService, _cacheService, _progress);
    }
    private string FormatDateTime(DateTime dateTime) => dateTime.ToString("yyyy-MM-dd HH-mm-ss");

    private void TryGetOrCreateValue(Dictionary<string, List<TimeSpan>> dict, string key, out List<TimeSpan> value)
    {
        if (dict.TryGetValue(key, out var ev))
            value = ev;
        else
        {
            dict[key] = [];
            value = dict[key];
        }
    }
}