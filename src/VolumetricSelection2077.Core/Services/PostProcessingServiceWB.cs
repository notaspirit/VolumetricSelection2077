using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Json.Helpers;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Core.Models.WorldBuilder.Favorites;
using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.App.Services;

public partial class PostProcessingService
{
    /*
    /// <summary>
    /// Saves the AxlRemovalFile as a world builder prefab
    /// </summary>
    /// <param name="axlRemovalFile"></param>
    private void SaveAsPrefab(AxlRemovalFile axlRemovalFile, string? outputFileNameOverride = null)
    {
        string favoritesPath;

        if (outputFileNameOverride != null)
        {
            favoritesPath = outputFileNameOverride;
        }
        else
        {
            switch (_settingsService.Data.SaveFileLocation)
            {
                case SaveFileLocation.GameDirectory:
                    favoritesPath = Path.Join(_settingsService.Data.GameDirectory, "bin", "x64", "plugins",
                        "cyber_engine_tweaks", "mods", "entSpawner", "data", "favorite", "GeneratedByVS2077.json");
                    break;
                case SaveFileLocation.OutputDirectory:
                    favoritesPath = Path.Join(_settingsService.Data.OutputDirectory, "GeneratedByVS2077.json");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        var logMessage = "";
        
        
        var favRoot = new FavoritesRoot
        {
            Name = "GeneratedByVS2077",
            Favorites = new()
        };
        var cfg = new WBConverterConfig()
        {
            DestructibleMeshTreatment = _settingsService.Data.DestructibleMeshTreatment
        };
        var convertedData = _removalToWorldBuilderConverter.Convert(axlRemovalFile, _settingsService.Data.OutputFilename, cfg);
        if (convertedData.Status == ResultStatus.Warning)
            Logger.Warning($"Encountered warnings while converting to WorldBuilder: {convertedData.Message}");
        
        if (convertedData.Data.EmbeddedFiles.Count > 0)
            Logger.Warning($"The following files are embedded and must be extracted for reliable use:\n {Utils.FormatDictionaryForDisplay(convertedData.Data.EmbeddedFiles)}");
        
        Logger.Success($"Found {convertedData.Data.RootElement.Children.OfType<PositionableGroup>().Sum(g => g.Children.Count)} WorldBuilder elements");
        if (!File.Exists(favoritesPath))
        {
            if (_settingsService.Data.SaveMode == FilePostProcessingMode.Subtract)
            {
                Logger.Error($"No Existing File to remove from found at {favoritesPath}");
                return;
            }
            
            favRoot.Favorites.Add(new Favorite
            {
                Name =  _settingsService.Data.OutputFilename,
                Data = (Positionable)convertedData.Data.RootElement
            });
            logMessage = $"Created prefab {_settingsService.Data.OutputFilename} at {favoritesPath}";

        }
        else
        {
            var existingFavorites = JsonConvert.DeserializeObject<FavoritesRoot>(File.ReadAllText(favoritesPath), JsonSerializerPresets.WorldBuilder);
            
            switch (_settingsService.Data.SaveMode)
            {
                case FilePostProcessingMode.Overwrite:
                    var existingOverwritePrefab = existingFavorites?.Favorites.FirstOrDefault(f => f.Name == _settingsService.Data.OutputFilename);
                    if (existingOverwritePrefab != null)
                    {
                        existingOverwritePrefab.Data =
                            (Positionable)convertedData.Data.RootElement;
                        logMessage = $"Overwrote prefab {_settingsService.Data.OutputFilename} at {favoritesPath}";
                    }
                    else
                        goto newPrefab;
                    break;
                case FilePostProcessingMode.Extend:
                    var existingExtendPrefab = existingFavorites?.Favorites.FirstOrDefault(f => f.Name == _settingsService.Data.OutputFilename);
                    if (existingExtendPrefab != null)
                    {
                        var existingElementsCount = existingExtendPrefab.Data.Children.OfType<PositionableGroup>().Sum(g => g.Children.Count);
                        existingExtendPrefab.Data = WorldBuilderMergingService.Merge(existingExtendPrefab, new Favorite
                        {
                            Name = _settingsService.Data.OutputFilename,
                            Data = (Positionable)convertedData.Data.RootElement
                        }).Data;
                        var newElementsCount = existingExtendPrefab.Data.Children.OfType<PositionableGroup>().Sum(g => g.Children.Count);
                        logMessage = $"Extended prefab {_settingsService.Data.OutputFilename} with {newElementsCount - existingElementsCount} new elements at {favoritesPath}";
                    }
                    else
                        goto newPrefab;
                    break;
                case FilePostProcessingMode.New:
                    newPrefab:
                    var existingNewPrefab = existingFavorites?.Favorites.FirstOrDefault(f => f.Name == _settingsService.Data.OutputFilename);
                    if (existingNewPrefab == null)
                    {
                        existingFavorites?.Favorites.Add(new Favorite
                        {
                            Name = _settingsService.Data.OutputFilename,
                            Data = (Positionable)convertedData.Data.RootElement
                        });
                        logMessage = $"Created prefab {_settingsService.Data.OutputFilename} at {favoritesPath}";
                    }
                    else
                    {
                        var newCount = existingFavorites?.Favorites.Count(f => f.Name.StartsWith(_settingsService.Data.OutputFilename));
                        var newOutputFilename = $"{_settingsService.Data.OutputFilename}+{newCount}";
                        existingFavorites?.Favorites.Add(new Favorite
                        {
                            Name = newOutputFilename,
                            Data = (Positionable)convertedData.Data.RootElement
                        });
                        logMessage = $"Created prefab {newOutputFilename} at {favoritesPath}";
                    }
                    break;
                case FilePostProcessingMode.Subtract:
                    var existingSubtractPrefab = existingFavorites?.Favorites.FirstOrDefault(f => f.Name == _settingsService.Data.OutputFilename);
                    if (existingSubtractPrefab != null)
                    {
                        var existingElementsCount = existingSubtractPrefab.Data.Children.OfType<PositionableGroup>().Sum(g => g.Children.Count);
                        existingSubtractPrefab.Data = WorldBuilderMergingService.Subtract(existingSubtractPrefab, new Favorite
                        {
                            Name = _settingsService.Data.OutputFilename,
                            Data = (Positionable)convertedData.Data.RootElement
                        }).Data;
                        var newElementsCount = existingSubtractPrefab.Data.Children.OfType<PositionableGroup>().Sum(g => g.Children.Count);
                        logMessage = $"Removed {(newElementsCount - existingElementsCount) * -1} elements from prefab {_settingsService.Data.OutputFilename} at {favoritesPath}";
                    }
                    else
                    {
                        Logger.Error($"No Existing prefab to remove from found at {favoritesPath}");
                        return;
                    }
                    break;
            }
            
            if (existingFavorites != null)
                favRoot.Favorites.AddRange(existingFavorites.Favorites);
        }

        var serialized = JsonConvert.SerializeObject(favRoot, JsonSerializerPresets.WorldBuilder);
        File.WriteAllText(favoritesPath, serialized);
        Logger.Info(logMessage);
        WriteBackupFile(favoritesPath, serialized);
    }
    */
}