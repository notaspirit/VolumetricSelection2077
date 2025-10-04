using System.Text.RegularExpressions;
using System.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VolumetricSelection2077.Models;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Json;
using VolumetricSelection2077.Json.Converters;
using VolumetricSelection2077.Json.Helpers;
using VolumetricSelection2077.Models.WorldBuilder.Favorites;
using VolumetricSelection2077.Resources;
using YamlDotNet.Serialization;

namespace VolumetricSelection2077.Services
{
    public class ValidationService
    {
        private readonly SettingsService _settingsService;
        private readonly CacheService _cacheService;
        private readonly GameFileService _gameFileService;
        private static readonly char[] InvalidCharacters = Path.GetInvalidPathChars().Concat(new[] { '?', '*', '"', '<', '>', '|', '/' }).Distinct().ToArray();

        public ValidationService()
        {
            _settingsService = SettingsService.Instance;
            _cacheService = CacheService.Instance;
            _gameFileService = GameFileService.Instance;
        }
        
        /// <summary>
        /// Validates Cache status, GameFileService status, output directory, selectionfile and filename
        /// </summary>
        /// <param name="gamePath">Path to the root of the game directory</param>
        /// <param name="outputFilename">Output filename</param>
        /// <returns></returns>
        /// <exception cref="Exception">Fails to create directory it tried to validate</exception>
        public InputValidationResult ValidateInput(string gamePath, string outputFilename)
        {
            bool cacheStatus = ValidateCacheStatus();
            bool gfsStatus = ValidateGameFileService();
            var outDirVR = ValidateAndCreateDirectory(_settingsService.OutputDirectory);
            var selFileVR = ValidateSelectionFile(gamePath);
            var validFileName = string.IsNullOrEmpty(outputFilename) ? PathValidationResult.Empty : ValidatePath(@"E:\" + outputFilename + ".xl");
            var resourceNameFilterValid = ValidateResourcePathFilter();
            var debugNameFilterValid = ValidateDebugNameFilter();
            var vanillaSectorBBsBuild = AreVanillaSectorBBsBuild();
            var moddedSectorBBsBuild = AreModdedSectorBBsBuild();
            var subtractionTargetExists = SubtractionTargetExists();
            
            return new InputValidationResult()
            {
                CacheStatus = cacheStatus,
                GameFileServiceStatus = gfsStatus,
                ValidOutputDirectory = outDirVR.Item1,
                OutputDirectroyPathValidationResult = outDirVR.Item2,
                SelectionFileExists = selFileVR.Item1,
                SelectionFilePathValidationResult = selFileVR.Item2,
                OutputFileName = validFileName,
                ResourceNameFilterValid = resourceNameFilterValid,
                DebugNameFilterValid = debugNameFilterValid,
                VanillaSectorBBsBuild = vanillaSectorBBsBuild,
                ModdedSectorBBsBuild = moddedSectorBBsBuild,
                SubtractionTargetExists = subtractionTargetExists
            };
        }
        
        /// <summary>
        /// Checks if the given directory is a valid game install
        /// </summary>
        /// <param name="gamePath">the directory to check</param>
        /// <returns></returns>
        public static (GamePathValidationResult, PathValidationResult) ValidateGamePath(string gamePath)
        {
            var validatePath = ValidatePath(gamePath);
            if (validatePath != PathValidationResult.Valid)
                return (GamePathValidationResult.InvalidGamePath, validatePath);
            
            string archiveContentPath = Path.Combine(gamePath, "archive", "pc", "content");
            string archiveEp1Path = Path.Combine(gamePath, "archive", "pc", "ep1");
            string CETModPath = Path.Combine(gamePath, "bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "VolumetricSelection2077", "data");

            if (!Directory.Exists(archiveContentPath) || !Directory.Exists(archiveEp1Path))
                return (GamePathValidationResult.InvalidGamePath, validatePath);
            if (!Directory.Exists(CETModPath))
                return (GamePathValidationResult.CetNotFound, validatePath);
            return (GamePathValidationResult.Valid, validatePath);
        }
        
        /// <summary>
        /// Checks if the directory is a valid path and creates it if it doesn't exist
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        /// <exception cref="Exception">Fails to create directory</exception>
        public static (bool, PathValidationResult) ValidateAndCreateDirectory(string directory)
        {
            var vpr = ValidatePath(directory);
            if (vpr != PathValidationResult.Valid)
                return (false, vpr);
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Failed to create output directory");
                return (false, vpr);
            }
            return (true, vpr);
        }
        
        /// <summary>
        /// Checks if the Cache via it's metadata
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="gamePath">Path to the root of the game directory</param>
        /// <param name="minimumProgramVersion">Oldest VS2077 that the cache can be from</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Did not find game executable</exception>
        public static bool ValidateCache(CacheDatabaseMetadata metadata, string gamePath, string minimumProgramVersion)
        {
            var gameExePath = Path.Combine(gamePath, "bin", "x64", "Cyberpunk2077.exe");
            if (!File.Exists(gameExePath))
                throw new ArgumentException("Could not find Game Executable.");
            
            var fileVerInfo = FileVersionInfo.GetVersionInfo(gameExePath);
            if (fileVerInfo.ProductVersion != metadata.GameVersion)
                return false;

            return metadata.VS2077Version == minimumProgramVersion;
        }

        /// <summary>
        ///  Checks if a given path is valid, not if it exists
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <returns>Enum describing validation outcome</returns>
        public static PathValidationResult ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return PathValidationResult.Empty;
            
            if (path.IndexOfAny(InvalidCharacters) != -1) return PathValidationResult.InvalidCharacters;

            if (!Path.IsPathFullyQualified(path)) return PathValidationResult.Relative;
            
            if (Path.GetPathRoot(path)?.Equals(path, StringComparison.OrdinalIgnoreCase) == true) 
                return PathValidationResult.Drive;
            
            if (path.Length >= 260) return PathValidationResult.TooLong;
            
            var parts = path.Split(Path.DirectorySeparatorChar);

            if (parts.Any(part => part != part.Trim()))
                return PathValidationResult.LeadingOrTrailingSpace;
            
            return PathValidationResult.Valid;
        }

        private bool SubtractionTargetExists()
        {
            switch (_settingsService.SaveFileFormat)
            {
                case SaveFileFormat.ArchiveXLYaml:
                case SaveFileFormat.ArchiveXLJson:
                    string outputFilePath;
                    switch (_settingsService.SaveFileLocation)
                    {
                        case SaveFileLocation.GameDirectory:
                            outputFilePath = Path.Join(_settingsService.GameDirectory, "archive", "pc", "mod", _settingsService.OutputFilename) + ".xl";
                            break;
                        case SaveFileLocation.OutputDirectory:
                            outputFilePath = Path.Join(_settingsService.OutputDirectory, _settingsService.OutputFilename) + ".xl";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    return File.Exists(outputFilePath);
                case SaveFileFormat.WorldBuilder:
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
                    if (!File.Exists(favoritesPath))
                        return false;

                    var favRoot = JsonConvert.DeserializeObject<FavoritesRoot>(File.ReadAllText(favoritesPath),
                        JsonSerializerPresets.WorldBuilder);
                    return favRoot?.Favorites.Any(x => x.Name == SettingsService.Instance.OutputFilename) ?? false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private bool AreVanillaSectorBBsBuild()
        {
            return _cacheService.GetMetadata().AreVanillaSectorBBsBuild;
        }
        
        /// <summary>
        /// Checks if all currently loaded modded sectors have build bounding boxes
        /// </summary>
        /// <returns>true if all modded sectors have bounding boxes or modded resources are disabled</returns>
        private bool AreModdedSectorBBsBuild()
        {
            if (!_settingsService.SupportModdedResources)
                return true;
            
            var inArchiveSectors = _gameFileService.ArchiveManager.GetModArchives()
                .SelectMany(x => x.Files.Values.Where(y => y.Extension == ".streamingsector").Select(y => y.FileName))
                .ToList();

            var cachedBBSectors = _cacheService.GetAllEntries(CacheDatabases.ModdedBounds)
                .Select(x => x.Key)
                .ToList();

            return inArchiveSectors.All(sector => cachedBBSectors.Contains(sector));
        }
        
        /// <summary>
        /// Checks if the selection file exists 
        /// </summary>
        /// <param name="gamePath">Path to the root of the game directory</param>
        /// <returns></returns>
        private (bool, PathValidationResult) ValidateSelectionFile(string gamePath)
        {
            var vpr = ValidatePath(gamePath);
            if (vpr != PathValidationResult.Valid)
                return (false, vpr);
            string selectionFilePath;
            if (string.IsNullOrWhiteSpace(_settingsService.CustomSelectionFilePath))
                selectionFilePath = Path.Combine(_settingsService.GameDirectory, "bin", "x64", "plugins", "cyber_engine_tweaks",
                    "mods", "VolumetricSelection2077", "data", "selection.json");
            else
                selectionFilePath = Path.Combine(_settingsService.CustomSelectionFilePath, "bin", "x64", "plugins", "cyber_engine_tweaks",
                    "mods", "VolumetricSelection2077", "data", "selection.json");
            return (File.Exists(selectionFilePath), vpr);
        }
        
        /// <summary>
        /// Checks if Cache Initialization Status matches Settings
        /// </summary>
        /// <returns></returns>
        private bool ValidateCacheStatus()
        {
            return _cacheService.IsInitialized;
        }

        /// <summary>
        /// Checks if the GameFileService is initialized
        /// </summary>
        /// <returns></returns>
        private bool ValidateGameFileService()
        {
            return _gameFileService.IsInitialized;
        }
        
        /// <summary>
        /// Checks if the given regex is valid
        /// </summary>
        /// <param name="regex"></param>
        /// <returns></returns>
        private static bool ValidateRegex(string regex)
        {
            if (string.IsNullOrEmpty(regex))
                return false;
            try
            {
                Regex.IsMatch("", regex);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        /// <summary>
        /// Checks if the all resourcePathFilters are valid regex
        /// </summary>
        /// <returns></returns>
        private bool ValidateResourcePathFilter()
        {
            return _settingsService.ResourceNameFilter.Count > 0 ? _settingsService.ResourceNameFilter.All(x => ValidateRegex(x)) : true;
        }
        
        /// <summary>
        /// Checks if the all debugNameFilters are valid regex
        /// </summary>
        /// <returns></returns>
        private bool ValidateDebugNameFilter()
        {
            return _settingsService.DebugNameFilter.Count > 0 ? _settingsService.DebugNameFilter.All(x => ValidateRegex(x)) : true;
        }
    }
}