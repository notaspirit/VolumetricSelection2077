using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace VolumetricSelection2077.Services;

public class ProcessService
{
    private readonly GameFileService _gameFileService;
    private readonly CacheService _cacheService;
    private readonly SettingsService _settings;
    private readonly WolvenkitCLIService _wolvenkitCLIService;
    private readonly WolvenkitAPIService _wolvenkitAPIService;
    public ProcessService()
    {
        _gameFileService = new GameFileService();
        _cacheService = CacheService.Instance;
        _settings = SettingsService.Instance;
        _wolvenkitCLIService = new WolvenkitCLIService();
        _wolvenkitAPIService = new WolvenkitAPIService();
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
    /*
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
            
            Logger.Info("Getting selection...");
            // _gameFileService.GetFiles();
            string selectionFilePath = Path.Combine(_settings.GameDirectory, "bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "VolumetricSelection2077", "data", "selection.json");
            string jsonString = File.ReadAllText(selectionFilePath);
            var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;
            var sectorsElement = root[1];
            
            var sectors = sectorsElement.EnumerateArray()
                .Select(s => s.GetString()?.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s!)  // Add this line to convert List<string?> to List<string>
                .ToList();
            Logger.Info($"Found {sectors.Count} sectors");
            Logger.Info("Getting sectors...");
            var (success3, error3, files3) = await _gameFileService.GetBulkMPackFiles(sectors);
            Logger.Info("Done!");
            /*
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
    */

    public async Task<(bool success, string error)> Process()
    {
        var stopwatch = Stopwatch.StartNew();
        Logger.Info("Starting process...");
        
        if (ValidationService.ValidateInput(_settings.GameDirectory, _settings.OutputFilename))
        {
            stopwatch.Stop();
            Logger.Error($"Process failed after {FormatElapsedTime(stopwatch.Elapsed)}");
            return (false, "Validation failed");
        }

        Logger.Info("Running checks on new WolvenkitAPI...");

        Logger.Info("Getting Version...");
        var (success, error, version) = await _wolvenkitAPIService.GetWolvenkitAPIScriptVersion();
        if (!success || string.IsNullOrEmpty(version))
        {
            Logger.Error($"Failed to get WolvenkitAPI version: {error}");
        } else {
            Logger.Success($"WolvenkitAPI Version: {version}");
        }

        Logger.Info("Getting File as JSON...");
        var (success2, error2, fileContent) = await _wolvenkitAPIService.GetFileAsJson("base\\worlds\\03_night_city\\_compiled\\default\\exterior_16_-15_1_1.streamingsector");
        if (!success2 || fileContent == null)
        {
            Logger.Error($"Failed to get file as JSON: {error2}");
        } else {
            Logger.Success($"File as JSON: {fileContent}");
        }

        Logger.Info("Getting File as GLB...");
        var (success3, error3, model) = await _wolvenkitAPIService.GetFileAsGlb("base\\worlds\\03_night_city\\sectors\\_external\\road_meshes\\i_roadintersection_ee67a915\\prx.mesh");
        if (!success3 || fileContent == null)
        {
            Logger.Error($"Failed to get file as GLB: {error3}");
        } else {
            Logger.Success($"Got file Successfully as GLB");
        }
        
        Logger.Info("Getting Geometry Cache from Hash...");
        var (success4, error4, geometryCache) = await _wolvenkitAPIService.GetGeometryCacheFromHash("4605862353872203317", "13549315671994267542");
        if (!success4 || geometryCache == null)
        {
            Logger.Error($"Failed to get geometry cache from hash: {error4}");
        } else {
            Logger.Success($"Got geometry cache from hash: {geometryCache}");
        }

        Logger.Info("Refreshing Settings...");
        var (success5, error5) = await _wolvenkitAPIService.RefreshSettings();
        if (!success5)
        {
            Logger.Error($"Failed to refresh settings: {error5}");
        } else {
            Logger.Success($"Settings refreshed successfully");
        }
        
        stopwatch.Stop();
        var elapsed = stopwatch.Elapsed;
        Logger.Success($"Process completed in {FormatElapsedTime(elapsed)}");
        return (true, string.Empty);
    }
}