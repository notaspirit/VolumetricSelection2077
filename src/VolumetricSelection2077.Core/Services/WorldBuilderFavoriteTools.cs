using Newtonsoft.Json;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Json.Helpers;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Models.WorldBuilder.Favorites;

namespace VolumetricSelection2077.Core.Services;

public static class WorldBuilderFavoriteTools
{
    public static Result<Favorite> ProcessFavorite(Favorite favoriteA, FilePostProcessingMode mode, Favorite? favoriteB = null)
    {
        var result = new Result<Favorite>();

        switch (mode)
        {
            case FilePostProcessingMode.Extend:
                if (favoriteB == null)
                {
                    result.Status = ResultStatus.Error;
                    result.AddMessage("Extend processing mode was selected but no file to extend with was provided");
                    return result;
                }

                var (extendedRem, newElementsCount) = WorldBuilderMergingService.Merge(favoriteA, favoriteB);
                result.Data = extendedRem;
                result.AddMessage(newElementsCount > 0 ? $"Extended file with {newElementsCount} new elements" : "No new changes");
                return result;
            case FilePostProcessingMode.Subtract:
                if (favoriteB == null)
                {
                    result.Status = ResultStatus.Error;
                    result.AddMessage("Subtract processing mode was selected but no file to subtract from was provided");
                    return result;
                }

                var (subtractedRem, subtractedElementsCount) = WorldBuilderMergingService.Subtract(favoriteA, favoriteB);
                result.Data = subtractedRem;
                result.AddMessage(subtractedElementsCount > 0 ? $"Subtracted {subtractedElementsCount} elements from file" : "No changes");
                return result;
            case FilePostProcessingMode.Overwrite:
            case FilePostProcessingMode.New:
                result.Data = favoriteA;
                return result;
            default:
                result.Status = ResultStatus.Error;
                result.AddMessage("Invalid file processing mode");
                return result;
        }
    }
    
    public static Result<string> SavePrefab(Favorite favorite, FilePostProcessingMode mode, string path)
    {
        var result = new Result<string>();

        Result<Favorite> processRes;
        FavoritesRoot? froot = new FavoritesRoot();
        if (File.Exists(path))
        {
            try
            {
                froot = JsonConvert.DeserializeObject<FavoritesRoot>(File.ReadAllText(path), JsonSerializerPresets.WorldBuilder);
            }
            catch (JsonException jex)
            {
                result.Status = ResultStatus.Error;
                result.AddMessage($"Failed to parse existing favorites file with error {jex.Message}");
                return result;
            }
        }
        
        if (froot is null)
        {
            result.Status = ResultStatus.Error;
            result.AddMessage("Failed to parse existing favorites file");
            return result;
        }
        
        if (mode is FilePostProcessingMode.Subtract or FilePostProcessingMode.Extend)
        {
            var existingFavorite = froot.Favorites.Find(x => x.Name == favorite.Name);
            if (existingFavorite is null)
            {
                result.Status = ResultStatus.Error;
                result.AddMessage("Failed to find existing favorite");
                return result;
            }
            
            processRes = ProcessFavorite(existingFavorite, mode, favorite);
        }
        else
        {
            processRes = ProcessFavorite(favorite, mode);
        }
        
        switch (processRes.Status)
        {
            case ResultStatus.Error:
                result.Status = ResultStatus.Error;
                result.AddMessage($"Failed to process removal with error {processRes.Message}");
                return result;
            case ResultStatus.Warning:
                result.Status = ResultStatus.Warning;
                result.AddMessage($"Processed removal with warning {processRes.Message}");
                break;
            case ResultStatus.Ok:
            default:
                break;
        }
        
        var newName = mode == FilePostProcessingMode.New 
            ? GetNewFavoriteName(favorite.Name, froot)
            : favorite.Name;

        processRes.Data!.Name = newName;
        result.Data = newName;
        
        froot.Favorites.Add(processRes.Data);
        
        var serialized = JsonConvert.SerializeObject(froot, JsonSerializerPresets.WorldBuilder);
        File.WriteAllText(path, serialized);
                
        return result;
    }

    private static string GetNewFavoriteName(string name, FavoritesRoot favoritesRoot)
    {
        if (favoritesRoot.Favorites.All(f => f.Name != name))
            return name;
        
        var totalCount = 1;
        foreach (var favorite in favoritesRoot.Favorites)
        {
            if (!favorite.Name.StartsWith(name))
                continue;
            if (!Int32.TryParse(favorite.Name.Split("+").Last().Split(".").First(), out int count))
                continue;
            if (count >= totalCount) 
                totalCount = count + 1;
        }
        return $"{name.Split("+").First()}+{totalCount}";
    }
}