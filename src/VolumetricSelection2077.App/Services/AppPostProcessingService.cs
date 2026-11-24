using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using VolumetricSelection2077.Core.Converters.Complex;
using VolumetricSelection2077.App.Enums;
using VolumetricSelection2077.App.Models;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.App.Services;

public class AppPostProcessingService
{
    private readonly SettingsService _settingsService;
    private readonly IAxlRemovalToWorldBuilderConverter _removalToWorldBuilderConverter;

    private readonly string? _outputFileNameOverride;
    private readonly bool _createBackup;
    
    public AppPostProcessingService(SettingsService settingsService,
        IAxlRemovalToWorldBuilderConverter removalToWorldBuilderConverter,
        string? outputFileNameOverride = null,
        bool createBackup = true)
    {
        _settingsService = settingsService;
        _removalToWorldBuilderConverter = removalToWorldBuilderConverter;
        
        _outputFileNameOverride = outputFileNameOverride;
        _createBackup = createBackup;
    }
    
    /// <summary>
    /// Entry method to be called from ProcessingService
    /// </summary>
    public Result Run(AxlRemovalFile removalFile)
    {
        var res = new Result();
        
        switch (_settingsService.Data.SaveFileFormat)
        {
            case SaveFileFormat.ArchiveXLJson:
            case SaveFileFormat.ArchiveXLYaml:
                res = SaveAsRemoval(removalFile);
                break;
            case SaveFileFormat.WorldBuilder:
                // SaveAsPrefab(removalFile, _outputFileNameOverride);
                break;
        }
        
        return res;
    }
    
    private Result SaveAsRemoval(AxlRemovalFile file)
    {
        var path = _outputFileNameOverride ?? _settingsService.Data.SaveFileLocation switch
        {
            SaveFileLocation.GameDirectory => Path.Join(_settingsService.Data.GameDirectory, "archive", "pc", "mod",
                _settingsService.Data.OutputFilename) + ".xl",
            SaveFileLocation.OutputDirectory => Path.Join(_settingsService.Data.OutputDirectory,
                _settingsService.Data.OutputFilename) + ".xl",
            _ => throw new ArgumentOutOfRangeException()
        };

        var mode = _settingsService.Data.SaveFileFormat switch
        {
            SaveFileFormat.ArchiveXLJson => AxlRemovalFormat.Json,
            SaveFileFormat.ArchiveXLYaml => AxlRemovalFormat.Yaml,
            _ => throw new ArgumentOutOfRangeException()
        };

        var res = ArchiveXlRemovalTools.SaveRemoval(file, _settingsService.Data.PostProcessingMode, mode, path);
        WriteBackupFile(path);
        return res;
    }

    private void WriteBackupFile(string originalOutputFilePath)
    {
        if (!_createBackup)
            return;
        
        var dirName = $"{Path.GetFileNameWithoutExtension(originalOutputFilePath)}-{_settingsService.Data.PostProcessingMode}-{_settingsService.Data.SaveFileFormat}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
        var dirPath = Path.Join(_settingsService.Data.BackupDirectory, dirName);
        Directory.CreateDirectory(dirPath);
        
        if (File.Exists(originalOutputFilePath))
            File.WriteAllText(Path.Join(dirPath, Path.GetFileName(originalOutputFilePath)), File.ReadAllText(originalOutputFilePath));

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