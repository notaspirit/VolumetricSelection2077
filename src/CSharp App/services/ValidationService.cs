using System.Text.RegularExpressions;
using System.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VolumetricSelection2077.Models;
using System.Threading.Tasks;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace VolumetricSelection2077.Services
{
    public static class ValidationService
    {
        private static readonly SettingsService _settingsService = SettingsService.Instance;
        public static readonly char[] InvalidCharacters = Path.GetInvalidPathChars().Concat(new[] { '?', '*', '"', '<', '>', '|', '/' }).Distinct().ToArray();
        public enum GamePathResult
        {
            InvalidGamePath,
            CetNotFound,
            Valid
        }
        /// <summary>
        /// Checks if the given directory is a valid game install
        /// </summary>
        /// <param name="gamePath">the directory to check</param>
        /// <returns></returns>
        public static (GamePathResult, PathValidationResult) ValidateGamePath(string gamePath)
        {
            var validatePath = ValidatePath(gamePath);
            if (validatePath != PathValidationResult.ValidDirectory)
                return (GamePathResult.InvalidGamePath, validatePath);
            
            string archiveContentPath = Path.Combine(gamePath, "archive", "pc", "content");
            string archiveEp1Path = Path.Combine(gamePath, "archive", "pc", "ep1");
            string CETModPath = Path.Combine(gamePath, "bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "VolumetricSelection2077", "data");

            if (!Directory.Exists(archiveContentPath) || !Directory.Exists(archiveEp1Path))
                return (GamePathResult.InvalidGamePath, validatePath);
            if (!Directory.Exists(CETModPath))
                return (GamePathResult.CetNotFound, validatePath);
            return (GamePathResult.Valid, validatePath);
        }
        
        /// <summary>
        /// Checks if the selection file exists 
        /// </summary>
        /// <param name="gamePath">Path to the root of the game directory</param>
        /// <returns></returns>
        public static (bool, PathValidationResult) ValidateSelectionFile(string gamePath)
        {
            var vpr = ValidatePath(gamePath);
            if (vpr != PathValidationResult.ValidDirectory)
                return (false, vpr);
            string selectionFilePath = Path.Combine(gamePath, "bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "VolumetricSelection2077", "data", "selection.json");
            return (File.Exists(selectionFilePath), vpr);
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
            if (vpr != PathValidationResult.ValidDirectory)
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
        /// Checks if Cache Initialization Status matches Settings
        /// </summary>
        /// <returns></returns>
        public static bool ValidateCacheStatus()
        {
            return CacheService.Instance.IsInitialized;
        }

        /// <summary>
        /// Checks if the GameFileService is initialized
        /// </summary>
        /// <returns></returns>
        public static bool ValidateGameFileService()
        {
            return GameFileService.Instance.IsInitialized;
        }
        
        /// <summary>
        /// Checks if the given regex is valid
        /// </summary>
        /// <param name="regex"></param>
        /// <returns></returns>
        public static bool ValidateRegex(string regex)
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
        public static bool ValidateResourcePathFilter()
        {
            return SettingsService.Instance.ResourceNameFilter.Count > 0 ? SettingsService.Instance.ResourceNameFilter.All(x => ValidateRegex(x)) : true;
        }
        
        /// <summary>
        /// Checks if the all debugNameFilters are valid regex
        /// </summary>
        /// <returns></returns>
        public static bool ValidateDebugNameFilter()
        {
            return SettingsService.Instance.DebugNameFilter.Count > 0 ? SettingsService.Instance.DebugNameFilter.All(x => ValidateRegex(x)) : true;
        }
        
        public class InputValidationResult
        {
            public bool CacheStatus { get; set; }
            public bool GameFileServiceStatus { get; set; }
            public bool ValidOutputDirectory { get; set; }
            public PathValidationResult OutputDirectroyPathValidationResult { get; set; }
            public bool SelectionFileExists { get; set; }
            public PathValidationResult SelectionFilePathValidationResult { get; set; }
            public PathValidationResult OutputFileName { get; set; }
            public bool ResourceNameFilterValid { get; set; }
            public bool DebugNameFilterValid { get; set; }
            public bool VanillaSectorBBsBuild { get; set; }
            public bool ModdedSectorBBsBuild { get; set; }
        }
        
        /// <summary>
        /// Validates Cache status, GameFileService status, output directory, selectionfile and filename
        /// </summary>
        /// <param name="gamePath">Path to the root of the game directory</param>
        /// <param name="outputFilename">Output filename</param>
        /// <returns></returns>
        /// <exception cref="Exception">Fails to create directory it tried to validate</exception>
        public static InputValidationResult ValidateInput(string gamePath, string outputFilename)
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
                ModdedSectorBBsBuild = moddedSectorBBsBuild
            };
        }
        
        /// <summary>
        /// Checks if the Cache via it's metadata
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="gamePath">Path to the root of the game directory</param>
        /// <param name="minimumProgramVersion">Oldest VS2077 that the cache can be from</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Did not find game executable</exception>
        public static bool ValidateCache(CacheService.CacheDatabaseMetadata metadata, string gamePath, string minimumProgramVersion)
        {
            var gameExePath = Path.Combine(gamePath, "bin", "x64", "Cyberpunk2077.exe");
            if (!File.Exists(gameExePath))
                throw new ArgumentException("Could not find Game Executable.");
            
            var fileVerInfo = FileVersionInfo.GetVersionInfo(gameExePath);
            if (fileVerInfo.ProductVersion != metadata.GameVersion)
                return false;

            if (metadata.VS2077Version != minimumProgramVersion)
                return false;
            return true;
        }

        public enum PathValidationResult
        {
            InvalidCharacters,
            Empty,
            Relative,
            Drive,
            LeadingOrTrailingSpace,
            TooLong,
            ValidFile,
            ValidDirectory
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

            if (Path.HasExtension(path))
                return PathValidationResult.ValidFile;
            return PathValidationResult.ValidDirectory;
        }
        public static bool AreVanillaSectorBBsBuild()
        {
            return CacheService.Instance.GetMetadata().AreVanillaSectorBBsBuild;
        }
        
        /// <summary>
        /// Checks if all currently loaded modded sectors have build bounding boxes
        /// </summary>
        /// <returns>true if all modded sectors have bounding boxes or modded resources are disabled</returns>
        public static bool AreModdedSectorBBsBuild()
        {
            if (!SettingsService.Instance.SupportModdedResources)
                return true;
            
            var inArchiveSectors = GameFileService.Instance.ArchiveManager.GetModArchives()
                .SelectMany(x => x.Files.Values.Where(y => y.Extension == ".streamingsector").Select(y => y.FileName))
                .ToList();

            var cachedBBSectors = CacheService.Instance.GetAllEntries(CacheDatabases.ModdedBounds)
                .Select(x => x.Key)
                .ToList();

            return inArchiveSectors.All(sector => cachedBBSectors.Contains(sector));
        }
    }
}