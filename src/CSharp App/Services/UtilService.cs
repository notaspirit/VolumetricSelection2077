using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using VolumetricSelection2077.Models;
using WolvenKit.Interfaces.Extensions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VolumetricSelection2077.Services
{
    public class UtilService
    {
        public static string BuildORRegex(List<string> patterns)
        {
            return string.Join("|", patterns);
        }
        public static string EscapeSlashes(string input)
        {
            return input.Replace("\\", "\\\\");
        }
        
        /// <summary>
        /// Formats TimeSpan to H Hour M minute S.MS Second with the larger ones only being added if it is not 0
        /// </summary>
        /// <param name="elapsed"></param>
        /// <returns></returns>
        public static string FormatElapsedTime(TimeSpan elapsed)
        {
            var parts = new List<string>();
        
            if (elapsed.Hours > 0)
            {
                parts.Add($"{elapsed.Hours} hour{(elapsed.Hours == 1 ? "" : "s")}");
            }
            if (elapsed.Minutes > 0)
            {
                parts.Add($"{elapsed.Minutes} minute{(elapsed.Minutes == 1 ? "" : "s")}");
            }
            if (elapsed.Seconds > 0 || parts.Count == 0)
            {
                parts.Add($"{elapsed.Seconds}.{elapsed.Milliseconds:D3} seconds");
            }
        
            return string.Join(", ", parts);
        }
        
        /// <summary>
        /// Formats elapsed seconds to MM:SS
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string FormatElapsedTimeMMSS(TimeSpan time)
        {
            return $"{time.Minutes:D2}:{time.Seconds:D2}";
        }


        public static AxlRemovalFile? TryParseAxlRemovalFile(String input)
        {
            try
            {
                return JsonConvert.DeserializeObject<AxlRemovalFile>(input);
            }
            catch (JsonException) { }
            
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                return deserializer.Deserialize<AxlRemovalFile>(input);
            }
            catch (YamlDotNet.Core.YamlException) { }
            
            return null;
        }

        public static string SanitizeFilePath(string input)
        {
            char[] additionalInvalidChars = { '?', '*', '"', '<', '>', '|', '\\', '/' };
            var invalidCharacters = Path.GetInvalidPathChars().Concat(additionalInvalidChars).Distinct().ToArray();
            
            List<string> cleanOutput = new();
            
            var parts = input.Split(Path.DirectorySeparatorChar);
            foreach (var part in parts)
            {
                var tempPart = part.Trim();
                tempPart = new string(tempPart.Select(c => invalidCharacters.Contains(c) ? '_' : c).ToArray());
                if (!string.IsNullOrEmpty(tempPart))
                {
                    cleanOutput.Add(tempPart);
                }
                
            }
            return string.Join(Path.DirectorySeparatorChar, cleanOutput);
        }
        /// <summary>
        /// Checks if the given directory contains any files
        /// </summary>
        /// <param name="path"></param>
        /// <returns>true if no files were found</returns>
        /// <exception cref="ArgumentException">given filepath is invalid</exception>
        public static bool IsDirectoryEmpty(string path)
        {
            if (ValidationService.ValidatePath(path) != ValidationService.PathValidationResult.Valid)
                throw new ArgumentException($"Path is invalid.");
            if (!Directory.Exists(path))
                throw new ArgumentException($"Path does not exist or is not Directory.");
            if (Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories).Any())
                return false;
            return true;
        }
        
                
        /// <summary>
        /// Checks if there is enough space on the drive and if the change is significant enough to resize the database
        /// </summary>
        /// <param name="db">database to calculate for</param>
        /// <param name="stats">current cache stats</param>
        /// <param name="cacheDirectory">current cache directory</param>
        /// <param name="resizeAfterBytes">threshold for resizing</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">if CacheDatabases.All is passed</exception>
        public static bool ShouldResize(CacheDatabases db, CacheService.CacheStats stats, string cacheDirectory, ulong resizeAfterBytes = 1024 * 1024 * 1024)
        {
            FileSize sizeToRemove;
            FileSize totalSize = new FileSize(stats.EstVanillaSize.Bytes + 
                                              stats.EstModdedSize.Bytes +
                                              stats.EstVanillaBoundsSize.Bytes +
                                              stats.EstModdedBoundsSize.Bytes);
            
            switch (db)
            {
                case CacheDatabases.Vanilla:
                    sizeToRemove = stats.EstVanillaSize;
                    break;
                case CacheDatabases.Modded:
                    sizeToRemove = stats.EstModdedSize;
                    break;
                case CacheDatabases.VanillaBounds:
                    sizeToRemove = stats.EstVanillaBoundsSize;
                    break;
                case CacheDatabases.ModdedBounds:
                    sizeToRemove = stats.EstModdedBoundsSize;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(db), db, null);
            }
            
            var cacheDriveInfo = new DriveInfo(cacheDirectory);
            var freeSpace = (ulong)cacheDriveInfo.AvailableFreeSpace;
            
            return sizeToRemove.Bytes > resizeAfterBytes && freeSpace > totalSize.Bytes - sizeToRemove.Bytes;
        }

    }
}