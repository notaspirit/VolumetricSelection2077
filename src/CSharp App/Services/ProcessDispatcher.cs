using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Parsers;

namespace VolumetricSelection2077.Services;

public class ProcessDispatcher
{
    private readonly ProcessService _processService;
    private readonly ValidationService _validationService;
    private readonly DialogService _dialogService;
    private readonly BoundingBoxBuilderService _boundingBoxBuilderService;
    private readonly SettingsService _settings;
    private readonly CacheService _cacheService;
    private readonly Progress _progress;
    
    public ProcessDispatcher(DialogService dialogService)
    {
        _processService = new ProcessService();
        _validationService = new ValidationService();
        _dialogService = dialogService;
        _boundingBoxBuilderService = new BoundingBoxBuilderService();
        _settings = SettingsService.Instance;
        _cacheService = CacheService.Instance;
        _progress = Progress.Instance;
    }

    public async Task<(bool success, string error)> StartProcess(string? customRemovalFile = null,
        string? customRemovalDirectory = null)
    {
        Logger.Info("Validating inputs...");

        try
        {
            var validationResult = _validationService.ValidateInput(_settings.GameDirectory, _settings.OutputFilename);
            if (!await EvaluateInputValidation(validationResult))
                return (false, "Invalid Input");
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, fileOnly: true);
            return (false, ex.Message + " : Failed to validate inputs");
        }
        
        Logger.Info("Starting Process...");
        
        _progress.Reset();
        _progress.SetWeight(0.05f, 0.9f, 0.05f);
        
        _progress.AddTarget(2, Progress.ProgressSections.Startup);
        
        bool customRemovalFileProvided = customRemovalFile != null;
        bool customRemovalDirectoryProvided = customRemovalDirectory != null;
        if (customRemovalFileProvided != customRemovalDirectoryProvided)
        {
            throw new ArgumentException("Both file path and output directory must be provided for a custom process!");
        }

        if (!File.Exists(customRemovalFile) && (customRemovalDirectoryProvided || customRemovalDirectory != null))
        {
            throw new ArgumentException($"Provided file ({customRemovalFile}) doesn't exist!");
        }

        SelectionInput? CETOutputFile;
        
        if (customRemovalDirectory == null)
        {
            string CETOuputFilepath;
            if (string.IsNullOrWhiteSpace(_settings.CustomSelectionFilePath))
                CETOuputFilepath= Path.Combine(_settings.GameDirectory, "bin", "x64", "plugins", "cyber_engine_tweaks",
                "mods", "VolumetricSelection2077", "data", "selection.json");
            else
                CETOuputFilepath= Path.Combine(_settings.CustomSelectionFilePath, "bin", "x64", "plugins", "cyber_engine_tweaks",
                    "mods", "VolumetricSelection2077", "data", "selection.json");
            
            string CETOutputFileString = File.ReadAllText(CETOuputFilepath);
            ( var successSP, var errorSP, CETOutputFile) = SelectionParser.ParseSelection(CETOutputFileString);

            if (CETOutputFile == null || successSP == false)
            {
                return (false, $"Failed to parse CET output file with error: {errorSP}");
            }
        }
        else
        {
            string customOutputFileString = File.ReadAllText(customRemovalFile);
            (var successSP, var errorSP, CETOutputFile) = SelectionParser.ParseSelection(customOutputFileString);
            if (CETOutputFile == null || successSP == false)
            {
                return (false, $"Failed to parse CET output file with error: {errorSP}");
            }
        }
        
        try
        {
            _cacheService.StartListening();
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "Failed to start listening to write requests in cache service!", fileOnly: true);
            return (false, "Failed to start listening to write requests in cache service!");
        }

        _progress.AddCurrent(1, Progress.ProgressSections.Startup);
        
        return await _processService.MainProcessTask(CETOutputFile);
    }
    
     /// <summary>
    /// Logs and Evaluates ValidationServiceResult
    /// </summary>
    /// <param name="vr"></param>
    /// <returns>true if all are valid, false if at least one is invalid</returns>
    private async Task<bool> EvaluateInputValidation(Enums.InputValidationResult vr)
    {
        int invalidCount = 0;
        bool invalidRegex = false;
        if (vr.OutputFileName == Enums.PathValidationResult.Valid)
            Logger.Success("Filename                 : OK");
        else
        {
            Logger.Error($"Filename                 : {vr.OutputFileName}");
            invalidCount++;
        }
        
        if (vr.CacheStatus)
            Logger.Success("Cache status             : OK");
        else
        {
            Logger.Error("Cache status             : Cache state does not match expected");
            invalidCount++;
        }
        
        if (vr.GameFileServiceStatus)
            Logger.Success("Game file service status : OK");
        else
        {
            Logger.Error("Game file service status : Not initialized");
            invalidCount++;
        }

        if (_settings.SaveFileLocation == SaveFileLocation.OutputDirectory)
        {
            if (vr.ValidOutputDirectory)
                Logger.Success("Output directory         : OK");
            else
            {
                Logger.Error($"Output directory         : {vr.OutputDirectroyPathValidationResult}");
                invalidCount++;
            }
        }

        if (vr.SelectionFileExists)
            Logger.Success("Selection File           : OK");
        else
        {
            string invalidReason = vr.SelectionFilePathValidationResult == Enums.PathValidationResult.Valid ? "Not found" : $"Invalid file path {vr.SelectionFilePathValidationResult}";
            Logger.Error($"Selection File           : {invalidReason}");
            invalidCount++;
        }

        if (vr.ResourceNameFilterValid)
            Logger.Success("Resource Name Filter     : OK");
        else
        {
            Logger.Error("Resource Name Filter     : Invalid Regex");
            invalidCount++;
            invalidRegex = true;
        }
        
        if (vr.DebugNameFilterValid)
            Logger.Success("Debug Name Filter        : OK");
        else
        {
            Logger.Error("Debug Name Filter        : Invalid Regex");
            invalidCount++;
            invalidRegex = true;
        }

        if (_settings.SaveMode == Enums.SaveFileMode.Subtract)
        {
            if (vr.SubtractionTargetExists)
            {
                Logger.Success("Subtraction Target       : OK");
            }
            else
            {
                Logger.Error("Subtraction Target       : Not found");
                invalidCount++;
            }
        }
        
        if (vr.VanillaSectorBBsBuild)
            Logger.Success("Vanilla Sector BBs       : OK");
        else
        {
            var dialogResult = await _dialogService.ShowDialog("Vanilla Sector Bounds not found!", "Vanilla Sector Bounds are not built, do you want to build them now (this will take a while) or fetch prebuild ones from remote?", 
                [new DialogButton("Fetch Remote", Enums.DialogButtonStyling.Primary),
                    new DialogButton("Build", Enums.DialogButtonStyling.Secondary),
                    new DialogButton("Cancel", Enums.DialogButtonStyling.Destructive)]);
            switch (dialogResult)
            {
                case 0:
                    RetryFetchingRemote:
                    Logger.Info("Fetching Sector Bounds from remote...");
                    var result = await FetchRemoteSectorBBs();
                    if (result)
                        Logger.Success("Vanilla Sector BBs       : OK");
                    else
                    {
                        var failedToFetchRemoteDialogResult = await _dialogService.ShowDialog("Failed to fetch remote sector bounds!", "Failed to fetch remote sector bounds, do you want to retry or build them now (this will take a while)?", 
                        [new DialogButton("Retry", Enums.DialogButtonStyling.Primary),
                            new DialogButton("Build", Enums.DialogButtonStyling.Secondary),
                            new DialogButton("Cancel", Enums.DialogButtonStyling.Destructive)]);
                        switch (failedToFetchRemoteDialogResult)
                        {
                            case 0:
                                goto RetryFetchingRemote;
                            case 1:
                                goto BuildSectorBBs;
                            case 2:
                                Logger.Error("Vanilla Sector BBs       : User Canceled");
                                invalidCount++;
                                break;
                        }
                    }
                    break;
                case 1:
                    BuildSectorBBs:
                    Logger.Info("Building Sector Bounds...");
                    try
                    {
                        await _boundingBoxBuilderService.BuildBounds(BuildBoundsMode.Vanilla);
                        Logger.Success("Vanilla Sector BBs       : OK");
                    }
                    catch (Exception e)
                    {
                        Logger.Exception(e, $"Failed to build sector bounds with error {e.Message}");
                        var failedToBuildSectorBoundsDialog = await _dialogService.ShowDialog("Failed to build sector bounds!", "Failed to build sector bounds, do you want to retry or fetch them from remote?", 
                        [new DialogButton("Retry", Enums.DialogButtonStyling.Primary),
                            new DialogButton("Fetch Remote", Enums.DialogButtonStyling.Secondary),
                            new DialogButton("Cancel", Enums.DialogButtonStyling.Destructive)]);
                        switch (failedToBuildSectorBoundsDialog)
                        {
                            case 0:
                                goto BuildSectorBBs;
                            case 1:
                                goto RetryFetchingRemote;
                            case 2:
                                Logger.Error("Vanilla Sector BBs       : User Canceled");
                                invalidCount++;
                                break;
                        }
                    }
                    break;
                case 2:
                    Logger.Error("Vanilla Sector BBs       : User Canceled");
                    invalidCount++;
                    break;
            }
        }
        
        if (vr.ModdedSectorBBsBuild)
            Logger.Success("Modded Sector BBs        : OK");
        else
        {
            var dialogResult = await _dialogService.ShowDialog("Modded Sector Bounds not found!", "Not all modded sectors have a build bounding box!", 
            [new DialogButton("Build Missing", Enums.DialogButtonStyling.Primary),
                new DialogButton("Rebuild All", Enums.DialogButtonStyling.Secondary),
                new DialogButton("Ignore", Enums.DialogButtonStyling.Secondary),
                new DialogButton("Cancel", Enums.DialogButtonStyling.Destructive)]);
            switch (dialogResult)
            {
                case 0:
                    await _boundingBoxBuilderService.BuildBounds(
                        BuildBoundsMode.MissingModded);
                    break;
                case 1:
                    await _boundingBoxBuilderService.BuildBounds(
                        BuildBoundsMode.RebuildModded);
                    break;
                case 2:
                    Logger.Warning("Modded Sector BBs        : User Ignored");
                    break;
                case 3:
                    Logger.Error("Modded Sector BBs        : User Canceled");
                    invalidCount++;
                    break;
            }
        }
        
        if (invalidCount == 0)
        {
            return true;
        }

        if (invalidRegex)
        {
            Logger.Info(@"If you were not trying to use regex ensure that you have escaped all special characters, most commonly '\' and '.' (should be escaped as '\\' and '\.')");
        }
        
        return false;
    }
     
    /// <summary>
    /// Fetches remote sector bounds from VS2077 Resource repo 
    /// </summary>
    /// <returns>true if successful</returns>
    private async Task<bool> FetchRemoteSectorBBs()
    {
        var cacheMetadata = _cacheService.GetMetadata();
        string fileUrl = $"https://github.com/notaspirit/VolumetricSelection2077Resources/raw/refs/heads/main/SectorBounds/{cacheMetadata.GameVersion}-{cacheMetadata.VS2077Version}.bin";
        string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VolumetricSelection2077", "temp", $"{cacheMetadata.GameVersion}-{cacheMetadata.VS2077Version}.bin");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        using (HttpClient client = new HttpClient())
        {
            try
            {
                Logger.Info($"Fetching {fileUrl}...");
                byte[] fileData = await client.GetByteArrayAsync(fileUrl);

                await File.WriteAllBytesAsync(filePath, fileData);
            }
            catch (HttpRequestException e)
            {
                Logger.Exception(e, $"Failed to fetch remote sector bounds! {e.Message}");
                return false;
            }
        }
        
        _cacheService.LoadSectorBBFromFile(filePath);
        File.Delete(filePath);
        return true;
    }
}