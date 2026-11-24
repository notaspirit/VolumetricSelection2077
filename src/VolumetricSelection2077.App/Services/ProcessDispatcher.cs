using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using VolumetricSelection2077.App.Enums;
using VolumetricSelection2077.App.Models;
using VolumetricSelection2077.App.Parsers;
using VolumetricSelection2077.Core.Components;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.App.Services;

public class ProcessDispatcher
{
    private const string QueryProgressSection = "query-progress-section";
    private const string FinalizationProgressSection = "finalization-progress-section";
    private const string BuildBoundsProgressSection = "build-bounds-progress-section";
    
    private static string RelativeSelectionFilePath => Path.Combine("bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "VolumetricSelection2077", "data", "selection.json");
    
    private readonly ISelectionService _selectionService;
    private readonly ValidationService _validationService;
    private readonly DialogService _dialogService;
    private readonly ISectorBBBuilderService _boundingBoxBuilderService;
    private readonly SettingsService _settings;
    private readonly ICacheService _cacheService;
    private readonly PostProcessingService _postProcessingService;
    private readonly HttpClient _httpClient;
    
    public ProcessDispatcher(
        ISelectionService selectionService,
        DialogService dialogService,
        ValidationService validationService,
        ISectorBBBuilderService boundingBoxBuilderService,
        SettingsService settings,
        ICacheService cacheService,
        PostProcessingService postProcessingService,
        HttpClient httpClient)
    {
        _selectionService = selectionService;
        _validationService = validationService;
        _dialogService = dialogService;
        _boundingBoxBuilderService = boundingBoxBuilderService;
        _settings = settings;
        _cacheService = cacheService;
        _postProcessingService = postProcessingService;
        _httpClient = httpClient;
    }

    public async Task<Result> StartProcess(string? customRemovalFile = null, IProgressService? progressService = null)
    {
        var result = new Result { Status = ResultStatus.Ok };
        
        Logger.Info("Validating inputs...");

        progressService?.AddSection(
            new ProgressSection(QueryProgressSection, 9)
            {
                Current = 0,
                Target = 1000
            });
        progressService?.AddSection(
            new ProgressSection(FinalizationProgressSection, 1)
            {
                Current = 0,
                Target = 1
            });
        
        try
        {
            var validationResult = _validationService.ValidateInput(_settings.Data.GameDirectory, _settings.Data.OutputFilename);
            if (!await EvaluateInputValidation(validationResult, progressService))
            {
                result.Status = ResultStatus.Error;
                result.AddMessage("Invalid inputs, see log for details");
                return result;
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, fileOnly: true);
            result.Status = ResultStatus.Error;
            result.AddMessage($"{ex.Message} : Failed to validate inputs");
            return result;
        }
        
        Logger.Info("Querying world data...");
        
        if (customRemovalFile != null && !File.Exists(customRemovalFile))
        {
            result.Status = ResultStatus.Error;
            result.AddMessage($"Custom removal file {customRemovalFile} does not exist!");
            return result;
        }
        
        var selectionFilePath = customRemovalFile;

        if (selectionFilePath == null)
        {
            if (string.IsNullOrWhiteSpace(_settings.Data.CustomSelectionFilePath))
            {
                selectionFilePath = Path.Combine(_settings.Data.GameDirectory, RelativeSelectionFilePath);
            }
            else
            {
                selectionFilePath = Path.Combine(_settings.Data.CustomSelectionFilePath, RelativeSelectionFilePath);
            }
        }

        if (!File.Exists(selectionFilePath))
        {
            result.Status = ResultStatus.Error;
            result.AddMessage($"Selection file {selectionFilePath} does not exist!");
            return result;
        }

        var query = new VSQuery();
        ConfigureQuery(query);
        
        var parsingResult = SelectionParser.ParseSelection(await File.ReadAllTextAsync(selectionFilePath), query);
        if (parsingResult.Status == ResultStatus.Error)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage($"Failed to parse selection file with error {parsingResult.Message}");
            return result;
        }
        
        try
        {
            _cacheService.StartListening();
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "Failed to start listening to write requests in cache service!", fileOnly: true);
            result.Status = ResultStatus.Error;
            result.AddMessage("Failed to start listening to write requests in cache service!");
            return result;
        }

        ProgressService? queryProgressService = null;
        if (progressService is not null)
        {
            queryProgressService = new ProgressService();
            queryProgressService.OnProgressChanged += ReportQueryProgress;
        }
        
        var queryResult = await _selectionService.QueryWorldData(query, queryProgressService);
        
        if (queryProgressService is not null) 
            queryProgressService.OnProgressChanged -= ReportQueryProgress;

        switch (queryResult.Status)
        {
            case ResultStatus.Error:
                result.Status = ResultStatus.Error;
                result.AddMessage($"Failed to query world data with error: {queryResult.Message}");
                return result;
            case ResultStatus.Warning:
                result.Status = ResultStatus.Warning;
                result.AddMessage($"Encountered warnings while querying world data: {queryResult.Message}");
                break;
            case ResultStatus.Ok:
            default:
                break;
        }

        if (queryResult.Data == null)
        {
            result.AddMessage("No world data matching filters found");
            return result;
        }
        
        _postProcessingService.Run(queryResult.Data);
        
        progressService?.AddProgress(FinalizationProgressSection, 1);
        
        return result;

        void ReportQueryProgress(object? sender, EventArgs args)
        {
            progressService?.SetProgress(QueryProgressSection, (uint)(queryProgressService.Progress * 10));
        }
    }

    private void ConfigureQuery(VSQuery query)
    {
        query.DataSource = _settings.Data.SupportModdedResources ? WorldDataSource.All : WorldDataSource.VanillaOnly;

        query.Filters.Add(new NodeTypeFilter
        {
            NodeTypes = _settings.Data.NodeTypeFilter
        });
        
        if (_settings.Data.DebugNameFilter.Count > 0)
        {
            var debugNameFilter = new DebugNameFilter
            {
                DebugNameRegexPatterns = _settings.Data.DebugNameFilter.ToList(),
                Required = !_settings.Data.FilterModeOr
            };
            query.Filters.Add(debugNameFilter);
        }

        if (_settings.Data.ResourceNameFilter.Count > 0)
        {
            var resourcePathFilter = new ResourcePathFilter
            {
                ResourcePathRegexPatterns = _settings.Data.ResourceNameFilter.ToList(),
                Required = !_settings.Data.FilterModeOr
            };
            query.Filters.Add(resourcePathFilter);
        }
    }

    /// <summary>
    /// Logs and Evaluates ValidationServiceResult
    /// </summary>
    /// <param name="vr"></param>
    /// <param name="parentProgress"></param>
    /// <returns>true if all are valid, false if at least one is invalid</returns>
    private async Task<bool> EvaluateInputValidation(Enums.InputValidationResult vr, IProgressService? parentProgress = null)
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
        
        if (_settings.Data.SaveFileLocation == SaveFileLocation.OutputDirectory)
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

        if (_settings.Data.SaveMode == Enums.SaveFileMode.Subtract)
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
                    if (result.Status == ResultStatus.Ok)
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
                        IProgressService? boundsProgress = null;
                        
                        if (parentProgress != null)
                        {
                            parentProgress.AddSection(new ProgressSection(BuildBoundsProgressSection, 90)
                            {
                                Current = 0,
                                Target = 1000
                            });

                            boundsProgress = new ProgressService();
                            boundsProgress.OnProgressChanged += ReportBoundsProgress;
                        }
                        
                        var boundsRes = await _boundingBoxBuilderService.BuildBounds(BuildBoundsMode.Vanilla, boundsProgress, _settings.Data.DebugMode);
                        
                        if (boundsProgress != null)
                        {
                            boundsProgress.OnProgressChanged -= ReportBoundsProgress;
                        }
                        
                        switch (boundsRes.Status)
                        {
                            case ResultStatus.Warning:
                                Logger.Warning($"Build bounds with warning: {boundsRes.Message}");
                                break;
                            case ResultStatus.Error:
                                Logger.Warning($"Failed to build bounds with error: {boundsRes.Message}");
                                invalidCount++;
                                break;
                            case ResultStatus.Ok:
                            default:
                                Logger.Success("Vanilla Sector BBs       : OK");
                                break;
                        }
                        
                        void ReportBoundsProgress(object? sender, EventArgs args)
                        {
                            parentProgress?.SetProgress(BuildBoundsProgressSection, (uint)(boundsProgress.Progress * 10));
                        }
                        
                        
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
                {
                    var boundsRes = await _boundingBoxBuilderService.BuildBounds(
                        BuildBoundsMode.MissingModded, null, true);

                    switch (boundsRes.Status)
                    {
                        case ResultStatus.Warning:
                            Logger.Warning($"Build bounds with warning: {boundsRes.Message}");
                            break;
                        case ResultStatus.Error:
                            Logger.Warning($"Failed to build bounds with error: {boundsRes.Message}");
                            invalidCount++;
                            break;
                        case ResultStatus.Ok:
                        default:
                            Logger.Success("Modded Sector BBs        : OK");
                            break;
                    }

                    break;
                }
                case 1:
                {
                    var boundsRes = await _boundingBoxBuilderService.BuildBounds(
                        BuildBoundsMode.RebuildModded, null, true);

                    switch (boundsRes.Status)
                    {
                        case ResultStatus.Warning:
                            Logger.Warning($"Build bounds with warning: {boundsRes.Message}");
                            break;
                        case ResultStatus.Error:
                            Logger.Warning($"Failed to build bounds with error: {boundsRes.Message}");
                            invalidCount++;
                            break;
                        case ResultStatus.Ok:
                        default:
                            Logger.Success("Modded Sector BBs        : OK");
                            break;
                    }

                    break;
                }
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
    private async Task<Result> FetchRemoteSectorBBs()
    {
        var result = new Result { Status = ResultStatus.Ok };
        
        var cacheMetadataResult = _cacheService.GetMetadata();
        if (cacheMetadataResult.Status == ResultStatus.Error || cacheMetadataResult.Data == null)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage($"Failed to get cache metadata with message: {cacheMetadataResult.Message}");
            return result;
        }
        
        var cacheMetadata = cacheMetadataResult.Data;
        
        var fileUrl = $"https://github.com/notaspirit/VolumetricSelection2077Resources/raw/refs/heads/main/SectorBounds/{cacheMetadata.GameVersion}-{cacheMetadata.DatabaseVersion}.bin";
        var filePath = Path.Combine(Path.GetTempPath(), $"VolumetricSelection2077-{cacheMetadata.GameVersion}-{cacheMetadata.DatabaseVersion}.bin");
        try
        {
            Logger.Info($"Fetching {fileUrl}...");
            byte[] fileData = await _httpClient.GetByteArrayAsync(fileUrl);

            await File.WriteAllBytesAsync(filePath, fileData);
        }
        catch (HttpRequestException e)
        {
            Logger.Exception(e, $"Failed to fetch remote sector bounds! {e.Message}");
            result.Status = ResultStatus.Error;
            result.AddMessage($"Failed to fetch remote sector bounds! {e.Message}");
            return result;
        }
        
        _cacheService.LoadSectorBBFromFile(filePath);
        File.Delete(filePath);
        return result;
    }
}