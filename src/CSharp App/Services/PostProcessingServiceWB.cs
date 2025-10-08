using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Json.Helpers;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Models.WorldBuilder.Favorites;

namespace VolumetricSelection2077.Services;

public partial class PostProcessingService
{
    /// <summary>
    /// Saves the AxlRemovalFile as a world builder prefab
    /// </summary>
    /// <param name="axlRemovalFile"></param>
    private void SaveAsPrefab(AxlRemovalFile axlRemovalFile)
    {
        string favoritesPath;
        switch (_settingsService.SaveFileLocation)
        {
            case SaveFileLocation.GameDirectory:
                favoritesPath = Path.Join(_settingsService.GameDirectory, "bin", "x64", "plugins",
                    "cyber_engine_tweaks", "mods", "entSpawner", "data", "favorite", "GeneratedByVS2077.json");
                break;
            case SaveFileLocation.OutputDirectory:
                favoritesPath = Path.Join(_settingsService.OutputDirectory, "GeneratedByVS2077.json");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        var logMessage = "";
        
        
        var favRoot = new FavoritesRoot
        {
            Name = "GeneratedByVS2077",
            Favorites = new()
        };

        var convertedData = _removalToWorldBuilderConverter.Convert(axlRemovalFile, _settingsService.OutputFilename);
        Logger.Success($"Found {convertedData.Children.OfType<PositionableGroup>().Sum(g => g.Children.Count)} WorldBuilder elements");
        if (!File.Exists(favoritesPath))
        {
            if (_settingsService.SaveMode == SaveFileMode.Subtract)
            {
                Logger.Error($"No Existing File to remove from found at {favoritesPath}");
                return;
            }
            
            favRoot.Favorites.Add(new Favorite
            {
                Name =  _settingsService.OutputFilename,
                Data = (Positionable)convertedData
            });
            logMessage = $"Created prefab {_settingsService.OutputFilename} at {favoritesPath}";

        }
        else
        {
            var existingFavorites = JsonConvert.DeserializeObject<FavoritesRoot>(File.ReadAllText(favoritesPath), JsonSerializerPresets.WorldBuilder);
            
            switch (_settingsService.SaveMode)
            {
                case SaveFileMode.Overwrite:
                    var existingOverwritePrefab = existingFavorites?.Favorites.FirstOrDefault(f => f.Name == _settingsService.OutputFilename);
                    if (existingOverwritePrefab != null)
                    {
                        existingOverwritePrefab.Data =
                            (Positionable)convertedData;
                        logMessage = $"Overwrote prefab {_settingsService.OutputFilename} at {favoritesPath}";
                    }
                    else
                        goto newPrefab;
                    break;
                case SaveFileMode.Extend:
                    var existingExtendPrefab = existingFavorites?.Favorites.FirstOrDefault(f => f.Name == _settingsService.OutputFilename);
                    if (existingExtendPrefab != null)
                    {
                        var existingElementsCount = existingExtendPrefab.Data.Children.OfType<PositionableGroup>().Sum(g => g.Children.Count);
                        existingExtendPrefab.Data = WorldBuilderMergingService.Merge(existingExtendPrefab, new Favorite
                        {
                            Name = _settingsService.OutputFilename,
                            Data = (Positionable)convertedData
                        }).Data;
                        var newElementsCount = existingExtendPrefab.Data.Children.OfType<PositionableGroup>().Sum(g => g.Children.Count);
                        logMessage = $"Extended prefab {_settingsService.OutputFilename} with {newElementsCount - existingElementsCount} new elements at {favoritesPath}";
                    }
                    else
                        goto newPrefab;
                    break;
                case SaveFileMode.New:
                    newPrefab:
                    var existingNewPrefab = existingFavorites?.Favorites.FirstOrDefault(f => f.Name == _settingsService.OutputFilename);
                    if (existingNewPrefab == null)
                    {
                        existingFavorites?.Favorites.Add(new Favorite
                        {
                            Name = _settingsService.OutputFilename,
                            Data = (Positionable)convertedData
                        });
                        logMessage = $"Created prefab {_settingsService.OutputFilename} at {favoritesPath}";
                    }
                    else
                    {
                        var newCount = existingFavorites?.Favorites.Count(f => f.Name.StartsWith(_settingsService.OutputFilename));
                        var newOutputFilename = $"{_settingsService.OutputFilename}+{newCount}";
                        existingFavorites?.Favorites.Add(new Favorite
                        {
                            Name = newOutputFilename,
                            Data = (Positionable)convertedData
                        });
                        logMessage = $"Created prefab {newOutputFilename} at {favoritesPath}";
                    }
                    break;
                case SaveFileMode.Subtract:
                    var existingSubtractPrefab = existingFavorites?.Favorites.FirstOrDefault(f => f.Name == _settingsService.OutputFilename);
                    if (existingSubtractPrefab != null)
                    {
                        var existingElementsCount = existingSubtractPrefab.Data.Children.OfType<PositionableGroup>().Sum(g => g.Children.Count);
                        existingSubtractPrefab.Data = WorldBuilderMergingService.Subtract(existingSubtractPrefab, new Favorite
                        {
                            Name = _settingsService.OutputFilename,
                            Data = (Positionable)convertedData
                        }).Data;
                        var newElementsCount = existingSubtractPrefab.Data.Children.OfType<PositionableGroup>().Sum(g => g.Children.Count);
                        logMessage = $"Removed {(newElementsCount - existingElementsCount) * -1} elements from prefab {_settingsService.OutputFilename} at {favoritesPath}";
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
}