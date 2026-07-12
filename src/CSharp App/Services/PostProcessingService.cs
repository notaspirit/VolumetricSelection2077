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

    private readonly string? _outputFileNameOverride;
    private readonly bool _createBackup;
    
    public PostProcessingService(SettingsService settingsService, Progress progress, GameFileService gfs, string? outputFileNameOverride = null, bool createBackup = true)
    {
        _progress = progress;
        _settingsService = settingsService;
        _removalToWorldBuilderConverter = new AxlRemovalToWorldBuilderConverter(gfs, settingsService);
        
        _outputFileNameOverride = outputFileNameOverride;
        _createBackup = createBackup;
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
        
        switch (_settingsService.Data.SaveFileFormat)
        {
            case SaveFileFormat.ArchiveXLJson:
            case SaveFileFormat.ArchiveXLYaml:
                SaveAsRemoval(removalFile, _outputFileNameOverride);
                break;
            case SaveFileFormat.WorldBuilder:
                SaveAsPrefab(removalFile, _outputFileNameOverride);
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
        if (!_createBackup)
            return;
        
        var dirName = $"{Path.GetFileNameWithoutExtension(originalOutputFilePath)}-{_settingsService.Data.SaveMode}-{_settingsService.Data.SaveFileFormat}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
        var dirPath = Path.Join(_settingsService.Data.BackupDirectory, dirName);
        Directory.CreateDirectory(dirPath);
        File.WriteAllText(Path.Join(dirPath, Path.GetFileName(originalOutputFilePath)), content);

        string selectionFilePath;
        var relativePath = Path.Join("bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "VolumetricSelection2077",
            "data", "selection.json");
        if (!string.IsNullOrEmpty(_settingsService.Data.CustomSelectionFilePath))
            selectionFilePath = Path.Join(_settingsService.Data.CustomSelectionFilePath, relativePath);
        else 
            selectionFilePath = Path.Join(_settingsService.Data.GameDirectory, relativePath);

        File.Copy(selectionFilePath, Path.Join(dirPath, Path.GetFileName(selectionFilePath)));
        File.WriteAllText(Path.Join(dirPath, "settings.json"), JsonConvert.SerializeObject(_settingsService.Data, Formatting.Indented));
        var latestLogFile = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "Logs")).GetFiles("*.txt").OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
        if (latestLogFile != null)
        {
            File.Copy(latestLogFile.FullName, Path.Join(dirPath, Path.GetFileName(latestLogFile.FullName)));
        }
            
        var dirInfo = new DirectoryInfo(_settingsService.Data.BackupDirectory);
        if (dirInfo.GetDirectories().Length <= _settingsService.Data.MaxBackupFiles)
            return;
        
        var dirsToDelete = dirInfo.GetDirectories().OrderByDescending(d => d.LastWriteTime).Skip(_settingsService.Data.MaxBackupFiles);
        foreach (var dir in dirsToDelete)
            dir.Delete(true);
    }
}