using System.Text.RegularExpressions;
using System.IO;
using System;
using System.Text.Json;
using VolumetricSelection2077.Models;
using System.Collections.Generic;

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
                    PropertyNameCaseInsensitive = true // Optional: allows for case-insensitive property matching
                };

                // Try to deserialize into our model
                var selections = JsonSerializer.Deserialize<List<Selection>>(jsonString, options);

                if (selections == null || selections.Count == 0)
                {
                    Logger.Error("Selection file is empty or invalid");
                    return false;
                }

                // Validate the first selection (assuming we only need one)
                var selection = selections[0];

                // Validate SelectionBox
                if (selection.SelectionBox == null)
                {
                    Logger.Error("Selection file missing SelectionBox");
                    return false;
                }

                // Validate Sectors
                if (selection.Sectors == null || selection.Sectors.Count == 0)
                {
                    Logger.Error("Selection file has no sectors");
                    return false;
                }

                // Validate all sector paths exist and have correct extension
                foreach (var sector in selection.Sectors)
                {
                    if (string.IsNullOrWhiteSpace(sector))
                    {
                        Logger.Error("Empty sector path found");
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
        public static bool ValidateInput(string gamePath, string outputFilename)
        {
            return ValidateGamePath(gamePath) && ValidateOutputFilename(outputFilename) && ValidateSelectionFile(gamePath);
        }
    }
}