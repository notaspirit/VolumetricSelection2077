using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using VolumetricSelection2077.Core.Converters.Complex;
using VolumetricSelection2077.App.Enums;
using VolumetricSelection2077.App.Models;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Core.Models.WorldBuilder.Favorites;
using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.App.Services;

public class PostProcessingService
{
    private readonly SettingsService _settingsService;
    private readonly IAxlRemovalToWorldBuilderConverter _removalToWorldBuilderConverter;

    private readonly string? _outputFileNameOverride;
    private readonly bool _createBackup;
    
    public PostProcessingService(SettingsService settingsService,
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
        switch (_settingsService.Data.SaveFileFormat)
        {
            case SaveFileFormat.ArchiveXLJson:
            case SaveFileFormat.ArchiveXLYaml:
                return SaveAsRemoval(removalFile);
            case SaveFileFormat.WorldBuilder:
                return SaveAsPrefab(removalFile);
        }

        var res =  new Result
        {
            Status = ResultStatus.Error
        };
        res.AddMessage($"Unknown SaveFileFormat: {_settingsService.Data.SaveFileFormat}");
        return res;
    }

    private Result SaveAsPrefab(AxlRemovalFile file)
    {
        var path = _outputFileNameOverride ?? _settingsService.Data.SaveFileLocation switch
        {
            SaveFileLocation.GameDirectory => Path.Join(_settingsService.Data.GameDirectory, "bin", "x64", "plugins",
                "cyber_engine_tweaks", "mods", "entSpawner", "data", "favorite", "GeneratedByVS2077.json"),
            SaveFileLocation.OutputDirectory => Path.Join(_settingsService.Data.OutputDirectory, "GeneratedByVS2077.json"),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        var favGroupRes = _removalToWorldBuilderConverter.Convert(file, _settingsService.Data.OutputFilename, new WBConverterConfig
        {
            DestructibleMeshTreatment = _settingsService.Data.DestructibleMeshTreatment
        });

        var result = new Result();

        switch (favGroupRes.Status)
        {
            case ResultStatus.Error:
                result.Status = ResultStatus.Error;
                result.AddMessage($"Failed to build world builder favorite group with error {favGroupRes.Message}");
                return result;
            case ResultStatus.Warning:
                result.Status = ResultStatus.Warning;
                result.AddMessage($"Build world builder favorite group with warning {favGroupRes.Message}");
                break;
            case ResultStatus.Ok:
            default:
                break;
        }

        if (favGroupRes.Data == null)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage("Failed to build world builder favorite");
            return result;
        }

        if (favGroupRes.Data.EmbeddedFiles.Count > 0)
        {
            result.AddMessage($"World builder group depends on {favGroupRes.Data.EmbeddedFiles.Count} embedded files in the following sectors:");
            result.AddMessage(Utils.FormatDictionaryForDisplay(favGroupRes.Data.EmbeddedFiles));
        }

        var fav = new Favorite()
        {
            Data = (Positionable)favGroupRes.Data.RootElement,
            Name = _settingsService.Data.OutputFilename
        };
        
        var savePrefabRes = WorldBuilderFavoriteTools.SavePrefab(fav, _settingsService.Data.PostProcessingMode, path);
        switch (savePrefabRes.Status)
        {
            case ResultStatus.Error:
                result.Status = ResultStatus.Error;
                result.AddMessage($"Failed to save world builder favorite group with error {favGroupRes.Message}");
                return result;
            case ResultStatus.Warning:
                result.Status = ResultStatus.Warning;
                result.AddMessage($"Saved world builder favorite group {savePrefabRes.Data} at {path} with warning {favGroupRes.Message}");
                break;
            case ResultStatus.Ok:
                if (savePrefabRes.Message != string.Empty)
                    result.AddMessage(savePrefabRes.Message);
                result.AddMessage($"Saved world builder favorite group {savePrefabRes.Data} at {path}");
                break;
            default:
                break;
        }
        WriteBackupFile(path);
        return result;
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

        var result = new Result();
        
        var saveRemovalRes = ArchiveXlRemovalTools.SaveRemoval(file, _settingsService.Data.PostProcessingMode, mode, path);
        switch (saveRemovalRes.Status)
        {
            case ResultStatus.Error:
                result.Status = ResultStatus.Error;
                result.AddMessage($"Failed to save removal with error {saveRemovalRes.Message}");
                return result;
            case ResultStatus.Warning:
                result.Status = ResultStatus.Warning;
                result.AddMessage($"Saved removal at {saveRemovalRes.Data} with warning {saveRemovalRes.Message}");
                break;
            case ResultStatus.Ok:
                if (saveRemovalRes.Message != string.Empty)
                    result.AddMessage(saveRemovalRes.Message);
                result.AddMessage($"Saved removal at {saveRemovalRes.Data}");
                break;
            default:
                break;
        }
        WriteBackupFile(path);
        return result;
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