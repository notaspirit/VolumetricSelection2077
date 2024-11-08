using System.Text.RegularExpressions;
using System.IO;
using System;
using System.Text.Json;
using VolumetricSelection2077.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace VolumetricSelection2077.Services
{
    public static class ValidationService
    {
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

            try
            {
                string jsonString = File.ReadAllText(selectionFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // The JSON is now an array with two elements
                var jsonDoc = JsonDocument.Parse(jsonString);
                var root = jsonDoc.RootElement;

                if (!root.EnumerateArray().Any())
                {
                    Logger.Error("Selection file is empty");
                    return false;
                }

                // First element is the box
                var boxElement = root[0];
                // Second element is the sectors array
                var sectorsElement = root[1];

                // Validate box
                if (!boxElement.TryGetProperty("vertices", out var vertices) || 
                    vertices.GetArrayLength() != 8)
                {
                    Logger.Error("Selection file has invalid vertices");
                    return false;
                }

                // Validate sectors
                var sectors = sectorsElement.EnumerateArray()
                    .Select(s => s.GetString())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                if (!sectors.Any())
                {
                    Logger.Error("Selection file has no sectors");
                    return false;
                }

                // Validate sector file extensions
                foreach (var sector in sectors)
                {
                    if (sector == null)
                    {
                        Logger.Error("Selection file has null sector");
                        return false;
                    }
                    if (!sector.EndsWith(".streamingsector", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Error($"Invalid sector file extension: {sector}");
                        return false;
                    }
                }

                Logger.Success("Selection file is valid");
                return true;
            }
            catch (JsonException ex)
            {
                Logger.Error($"Invalid JSON format: {ex.Message}");
                return false;
            }
        }
        public static async Task<bool> ValidateWolvenkitVersion()
        {
            var wolvenkitCLI = new WolvenkitCLIService();
            var version = await wolvenkitCLI.GetVersionAsync();
            if (string.IsNullOrEmpty(version))
            {
                return false;
            }
            Logger.Info($"Wolvenkit version: {version}");
            // Extract version number (everything before -nightly if it exists)
            var versionString = version.Split('-')[0];
            
            // Parse version
            if (!Version.TryParse(versionString, out Version? currentVersion))
            {
                Logger.Error($"Failed to parse WolvenKit version: {versionString}");
                return false;
            }

            var minimumVersion = new Version(8, 15, 0);
            if (currentVersion < minimumVersion)
            {
                Logger.Error($"WolvenKit version {versionString} is below minimum required version {minimumVersion}");
                return false;
            }

            Logger.Success($"WolvenKit version {versionString} meets minimum requirement {minimumVersion}");
            return true;
        }
        public static async Task<bool> ValidateInput(string gamePath, string outputFilename)
        {
            Logger.Info("Validating input...");
            var syncValidations = ValidateGamePath(gamePath) && 
                                ValidateOutputFilename(outputFilename) && 
                                ValidateSelectionFile(gamePath);
                                
            return syncValidations && await ValidateWolvenkitVersion();
        }
    }
}