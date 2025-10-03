using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using VolumetricSelection2077.Converters.Complex;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Json.Helpers;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Models.WorldBuilder.Favorites;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VolumetricSelection2077.Services;

public partial class PostProcessingService
{
    private readonly Progress _progress;
    private readonly SettingsService _settingsService;
    private readonly AxlRemovalToWorldBuilderConverter _removalToWorldBuilderConverter;
    
    
    public PostProcessingService()
    {
        _progress = Progress.Instance;
        _settingsService = SettingsService.Instance;
        _removalToWorldBuilderConverter = new AxlRemovalToWorldBuilderConverter();
    }
    
    /// <summary>
    /// Entry method to be called from ProcessingService
    /// </summary>
    public void Run(AxlRemovalSector?[] rawSectors)
    {
        _progress.AddTarget(2, Progress.ProgressSections.Finalization);

        var nullCheckedSectors = rawSectors.Where(s => s != null).ToList() as List<AxlRemovalSector>;
        if (nullCheckedSectors.Count == 0)
        {
            Logger.Warning("No sectors intersect, no output file generated!");
            return;
        }
        
        _progress.AddCurrent(1, Progress.ProgressSections.Finalization);
        
        var removalFile = new AxlRemovalFile
        {
            Streaming = new AxlRemovalStreaming
            {
                Sectors = nullCheckedSectors
            }
        };
        
        switch (_settingsService.SaveFileFormat)
        {
            case SaveFileFormat.ArchiveXLJson:
            case SaveFileFormat.ArchiveXLYaml:
                SaveAsRemoval(removalFile);
                break;
            case SaveFileFormat.WorldBuilder:
                SaveAsPrefab(removalFile);
                break;
        }
        
        _progress.AddCurrent(1, Progress.ProgressSections.Finalization);
    }
    
    private static string GetOutputFilename(string outputFilename)
    {
        if (ValidationService.ValidatePath(outputFilename) != PathValidationResult.Valid)
            throw new ArgumentException("Invalid output filename!");
        
        if (!File.Exists(outputFilename))
            return outputFilename;
        
        int totalCount = 1;
        string outputFilePathWithoutExtension = outputFilename.Split('.').First();
        foreach (var file in Directory.GetFiles(Path.GetDirectoryName(outputFilename), "*.*",
                     SearchOption.AllDirectories))
        {
            if (!file.StartsWith(outputFilePathWithoutExtension)) continue;
            if (!Int32.TryParse(file.Split("+").Last().Split(".").First(), out int count))
                continue;
            if (count >= totalCount) 
                totalCount = count + 1;
        }
        return $"{outputFilePathWithoutExtension.Split("+").First()}+{totalCount}.{outputFilename.Split('.').Last()}";
    }

    private void WriteBackupFile(string originalOutputFilePath, string content)
    {
        var dirName = $"{Path.GetFileNameWithoutExtension(originalOutputFilePath)}-{_settingsService.SaveMode}-{_settingsService.SaveFileFormat}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
        var dirPath = Path.Join(_settingsService.BackupDirectory, dirName);
        Directory.CreateDirectory(dirPath);
        File.WriteAllText(Path.Join(dirPath, Path.GetFileName(originalOutputFilePath)), content);

        string selectionFilePath;
        var relativePath = Path.Join("bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "VolumetricSelection2077",
            "data", "selection.json");
        if (!string.IsNullOrEmpty(_settingsService.CustomSelectionFilePath))
            selectionFilePath = Path.Join(_settingsService.CustomSelectionFilePath, relativePath);
        else 
            selectionFilePath = Path.Join(_settingsService.GameDirectory, relativePath);

        File.Copy(selectionFilePath, Path.Join(dirPath, Path.GetFileName(selectionFilePath)));
        File.WriteAllText(Path.Join(dirPath, "settings.json"), JsonConvert.SerializeObject(_settingsService, Formatting.Indented));
        var latestLogFile = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "Logs")).GetFiles("*.txt").OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
        if (latestLogFile != null)
        {
            File.Copy(latestLogFile.FullName, Path.Join(dirPath, Path.GetFileName(latestLogFile.FullName)));
        }
            
        var dirInfo = new DirectoryInfo(_settingsService.BackupDirectory);
        if (dirInfo.GetDirectories().Length <= _settingsService.MaxBackupFiles)
            return;
        
        var dirsToDelete = dirInfo.GetDirectories().OrderByDescending(d => d.LastWriteTime).Skip(_settingsService.MaxBackupFiles);
        foreach (var dir in dirsToDelete)
            dir.Delete(true);
    }
}