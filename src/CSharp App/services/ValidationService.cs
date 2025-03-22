using System.Text.RegularExpressions;
using System.IO;
using System;
using System.Diagnostics;
using System.Linq;
using VolumetricSelection2077.Models;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VolumetricSelection2077.Services
{
    public static class ValidationService
    {
        private static readonly SettingsService _settingsService = SettingsService.Instance;
        public static readonly char[] InvalidCharacters = Path.GetInvalidPathChars().Concat(new[] { '?', '*', '"', '<', '>', '|', '/' }).Distinct().ToArray();
        public static bool ValidateOutputFilename(string filename)
        {
            // Regular expressions for validation
            var invalidChars = new Regex(@"[<>:""/\\|?*\x00-\x1F]");
            var reservedNames = new Regex(@"^(con|prn|aux|nul|com[1-9]|lpt[1-9])$", RegexOptions.IgnoreCase);
            var trailingSpaceDot = new Regex(@"[\s.]$");

            // Check for invalid characters
            if (invalidChars.IsMatch(filename))
            {
                Logger.Error("Output filename contains invalid characters");
                return false;
            }

            // Check for reserved names
            if (reservedNames.IsMatch(filename))
            {
                Logger.Error("Output filename is a reserved name");
                return false;
            }

            // Check for empty or only spaces
            if (string.IsNullOrWhiteSpace(filename))
            {
                Logger.Error("Output filename is empty");
                return false;
            }

            // Check for Spaces
            if (filename.Contains(" "))
            {
                Logger.Error("Output filename contains spaces");
                return false;
            }

            // Check for trailing spaces or dots
            if (trailingSpaceDot.IsMatch(filename))
            {
                Logger.Error("Output filename ends with a space or dot");
                return false;
            }

            // Check for periods indicating an extension
            if (filename.Contains(".") && !filename.StartsWith(".") && !filename.EndsWith("."))
            {
                Logger.Error("Output filename has a period indicating an extension");
                return false;
            }

            Logger.Success("Output filename is valid");
            return true;
        }

        public enum GamePathResult
        {
            NotSet = 0,
            InvalidGamePath = 1,
            CetNotFound = 2,
            Valid = 3
        }
        
        public static GamePathResult ValidateGamePath(string gamePath)
        {
            if (string.IsNullOrWhiteSpace(gamePath))
                return GamePathResult.NotSet;
            
            string archiveContentPath = Path.Combine(gamePath, "archive", "pc", "content");
            string archiveEp1Path = Path.Combine(gamePath, "archive", "pc", "ep1");
            string CETModPath = Path.Combine(gamePath, "bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "VolumetricSelection2077", "data");

            if (!Directory.Exists(archiveContentPath) || !Directory.Exists(archiveEp1Path))
                return GamePathResult.InvalidGamePath;
            if (!Directory.Exists(CETModPath))
                return GamePathResult.CetNotFound;
            return GamePathResult.Valid;
        }
        
        public static bool ValidateSelectionFile(string gamePath)
        {
            string selectionFilePath = Path.Combine(gamePath, "bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "VolumetricSelection2077", "data", "selection.json");
            
            if (!File.Exists(selectionFilePath))
            {
                Logger.Error("Selection file not found");
                return false;
            }
            Logger.Success("Selection file is valid");
            return true;
        }
        public static bool ValidateOutputDirectory(string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                Logger.Warning("Output directory is not set in settings, using default");
                outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "output");
                Directory.CreateDirectory(outputDirectory);
                _settingsService.OutputDirectory = outputDirectory;
                _settingsService.SaveSettings();
            }
            return Directory.Exists(outputDirectory);
        }
        public static bool ValidateCacheDirectory(string cacheDirectory)
        {
            if (string.IsNullOrWhiteSpace(cacheDirectory))
            {
                Logger.Info("Cache directory is not set in settings, using default");
                cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077");
                Directory.CreateDirectory(cacheDirectory);
                _settingsService.CacheDirectory = cacheDirectory;
                _settingsService.SaveSettings();
            }
            return Directory.Exists(cacheDirectory);
        }
        public static bool ValidateInput(string gamePath, string outputFilename)
        {
            var syncValidations = ValidateGamePath(gamePath) == GamePathResult.Valid && 
                                ValidateSelectionFile(gamePath) &&
                                ValidateOutputDirectory(_settingsService.OutputDirectory);
                                
            return syncValidations;
        }

        public static bool ValidateCache(CacheService.CacheDatabaseMetadata metadata, string gamePath, string minimumProgramVersion)
        {
            var gameExePath = Path.Combine(gamePath, "bin", "x64", "Cyberpunk2077.exe");
            if (!File.Exists(gameExePath))
            {
                throw new Exception("Could not find Game Executable.");
            }
            var fileVerInfo = FileVersionInfo.GetVersionInfo(gameExePath);
            if (fileVerInfo.ProductVersion != metadata.GameVersion)
            {
                return false;
            }

            if (metadata.VS2077Version != minimumProgramVersion)
            {
                return false;
            }
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
    }
}