using System.Text.RegularExpressions;
using System.IO;
using System;
using System.Diagnostics;
using VolumetricSelection2077.Models;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VolumetricSelection2077.Services
{
    public static class ValidationService
    {
        private static readonly SettingsService _settingsService = SettingsService.Instance;
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
        public static bool ValidateGamePath(string gamePath)
        {
            if (string.IsNullOrWhiteSpace(gamePath))
            {
                Logger.Error("Game path is not set in settings");
                return false;
            }            
            string archiveContentPath = Path.Combine(gamePath, "archive", "pc", "content");
            string archiveEp1Path = Path.Combine(gamePath, "archive", "pc", "ep1");
            string CETModPath = Path.Combine(gamePath, "bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "VolumetricSelection2077", "data");

            if (!Directory.Exists(archiveContentPath) || !Directory.Exists(archiveEp1Path))
            {
                Logger.Error("Archive folder not found, game path is invalid");
                return false;
            }
            if (!Directory.Exists(CETModPath))
            {
                Logger.Error("CET mod is not installed");
                return false;
            }

            Logger.Success("Game path is valid");
            return true;
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
                Logger.Info("Output directory is not set in settings, using default");
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
            var syncValidations = ValidateGamePath(gamePath) && 
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
    }
}