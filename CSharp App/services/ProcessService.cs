using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace VolumetricSelection2077.Services;

public class ProcessService
{
    private readonly GameFileService _gameFileService;
    private readonly CacheService _cacheService;
    private readonly SettingsService _settings;
    private readonly WolvenkitCLIService _wolvenkitCLIService;
    public ProcessService()
    {
        _gameFileService = new GameFileService();
        _cacheService = CacheService.Instance;
        _settings = SettingsService.Instance;
        _wolvenkitCLIService = new WolvenkitCLIService();
    }

    private string FormatElapsedTime(TimeSpan elapsed)
    {
        var parts = new List<string>();
        
        if (elapsed.Hours > 0)
        {
            parts.Add($"{elapsed.Hours} hour{(elapsed.Hours == 1 ? "" : "s")}");
        }
        if (elapsed.Minutes > 0)
        {
            parts.Add($"{elapsed.Minutes} minute{(elapsed.Minutes == 1 ? "" : "s")}");
        }
        if (elapsed.Seconds > 0 || parts.Count == 0)
        {
            parts.Add($"{elapsed.Seconds}.{elapsed.Milliseconds:D3} seconds");
        }
        
        return string.Join(", ", parts);
    }

    public async Task<(bool success, string error)> Process()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            Logger.Info("Starting process...");
            
            if (!await ValidationService.ValidateInput(_settings.GameDirectory, _settings.OutputFilename))
            {
                stopwatch.Stop();
                Logger.Info($"Process failed after {FormatElapsedTime(stopwatch.Elapsed)}");
                return (false, "Validation failed");
            }
            
            Logger.Info("Checking for filemap...");
            var (success, error) = await _gameFileService.buildFileMap();
            
            stopwatch.Stop();
            var elapsed = stopwatch.Elapsed;

            
            if (!success)
            {
               Logger.Info($"Process failed after {FormatElapsedTime(elapsed)}");
               return (false, error);
            }
            
            _gameFileService.GetFiles();
            
            string testFilePath = @"base\worlds\03_night_city\sectors\_generated\collisions\03_night_city.geometry_cache";
            var (WKsuccess, WKerror, WKoutputCR2WFile) = await _gameFileService.GetCR2WFile(testFilePath);
            if (!WKsuccess || WKoutputCR2WFile == null || !string.IsNullOrEmpty(WKerror))
            {
                Logger.Error($"Failed to extract CR2W file: {WKerror}");
                return (false, WKerror);
            }
            Logger.Info($"Successfully extracted CR2W file: {testFilePath}");
            
            Logger.Success($"Process completed in {FormatElapsedTime(elapsed)}");
            
            return (true, string.Empty);
            
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.Error($"Process failed after {FormatElapsedTime(stopwatch.Elapsed)}");
            return (false, ex.Message);
        }
    }
}