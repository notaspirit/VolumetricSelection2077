using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Parsers;
using Newtonsoft.Json;

namespace VolumetricSelection2077.Services;

public class ProcessService
{
    private readonly SettingsService _settings;
    private readonly WolvenkitAPIService _wolvenkitAPIService;
    public ProcessService()
    {
        _settings = SettingsService.Instance;
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

    private (bool success, string error, AxlRemovalSector? result) ProcessStreamingsector(AbbrSector sector, string sectorPath)
    {
        Logger.Info("Starting processing streaming sector");
        int meshCount = 0;
        List<string> meshTypes = new List<string>();
        
        foreach (var nodeDataEntry in sector.NodeData)
        {
            AbbrStreamingSectorNodesEntry nodeEntry = sector.Nodes[nodeDataEntry.NodeIndex];
            Logger.Info(nodeEntry.Type);
            if (nodeEntry.Type.Contains("Mesh"))
            {
                Logger.Info(nodeEntry.MeshDepotPath ?? "No mesh path");
                meshCount++;
                if (!meshTypes.Contains(nodeEntry.Type))
                {
                    meshTypes.Add(nodeEntry.Type);
                }
            }
        }
        Logger.Info($"Found {meshCount.ToString()} in {sectorPath}");
        Logger.Info($"Avaliable mesh types: {string.Join(", ", meshTypes)}");
        
        return (true, "", null);
    }

    public async Task<(bool success, string error)> Process()
    {
        var stopwatch = Stopwatch.StartNew();
        Logger.Info("Validating inputs...");
        
        if (!ValidationService.ValidateInput(_settings.GameDirectory, _settings.OutputFilename))
        {
            stopwatch.Stop();
            Logger.Error($"Process failed after {FormatElapsedTime(stopwatch.Elapsed)}");
            return (false, "Validation failed");
        }

        var (success, error, version) = await _wolvenkitAPIService.GetWolvenkitAPIScriptVersion();
        if (!success || string.IsNullOrEmpty(version))
        {
            stopwatch.Stop();
            Logger.Error($"Failed to get VS2077 WScript version");
            Logger.Error($"Process failed after {FormatElapsedTime(stopwatch.Elapsed)}");
            return (false, error);
        }
        Logger.Success($"VS2077 WScript Version: {version}");
        
        var (success5, error5) = await _wolvenkitAPIService.RefreshSettings();
        if (!success5)
        {
            stopwatch.Stop();
            Logger.Error($"Failed to set VS2077 WScript settings: {error5}");
            Logger.Error($"Process failed after {FormatElapsedTime(stopwatch.Elapsed)}");
            return (false, error);
        }
        Logger.Success($"VS2077 WScript settings set successfully");
        

        Logger.Info("Starting Process...");
        
        string CETOuputFilepath = Path.Combine(_settings.GameDirectory, "bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "VolumetricSelection2077", "data", "selection.json");
        string CETOutputFileString = File.ReadAllText(CETOuputFilepath);
        SelectionInput? CETOutputFile = JsonConvert.DeserializeObject<SelectionInput>(CETOutputFileString);

        if (CETOutputFile == null)
        {
            stopwatch.Stop();
            Logger.Error("Failed to parse CET output file");
            Logger.Error($"Process failed after {FormatElapsedTime(stopwatch.Elapsed)}");
            return (false, "Failed to parse CET output file");
        }

        var (successGLB, errorGLB, testGLB) = await _wolvenkitAPIService.GetFileAsGlb(
            "ep1\\worlds\\03_night_city\\sectors\\_external\\proxy\\2939601539\\mon_ave_scaffolding_f.mesh");
        if (testGLB == null)
        {
            stopwatch.Stop();
            Logger.Error($"Failed to get test GLB file");
            Logger.Error($"Process failed after {FormatElapsedTime(stopwatch.Elapsed)}");
            return (false, "Failed to get test GLB file");
        }
        var parsedGlb = AbbrMeshParser.ParseFromGlb(testGLB);
        /*
        List<string> testSectors = new List<string>();
        testSectors.Add("base\\worlds\\03_night_city\\_compiled\\default\\exterior_-6_-4_0_2.streamingsector");
        
        foreach (string streamingSectorName in testSectors)
        {
            var (successGET, errrorGET, stringGET) = await _wolvenkitAPIService.GetFileAsJson(streamingSectorName);
            if (!successGET || !string.IsNullOrEmpty(errrorGET) || string.IsNullOrEmpty(stringGET))
            {
                Logger.Error($"Failed to get streamingsector {streamingSectorName}, error: {errrorGET}");
                continue;
            }
            
            AbbrSector? sectorDeserialized = AbbrSectorParser.Deserialize(stringGET);
            if (sectorDeserialized == null)
            {
                Logger.Error($"Failed to deserialize streamingsector {streamingSectorName}");
                continue;
            }
            
            var (succsessProc, errorProc, AxlRemovalSector) = ProcessStreamingsector(sectorDeserialized, streamingSectorName);
            
        }
        */
        stopwatch.Stop();
        var elapsed = stopwatch.Elapsed;
        Logger.Info($"Process completed in {FormatElapsedTime(elapsed)}.");
        return (true, string.Empty);
    }
}