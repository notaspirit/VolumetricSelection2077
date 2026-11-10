
using PeNet.Header.Resource;

namespace VolumetricSelection2077.Core.Services
{
    public static class Utils
    {
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
            if (!Directory.Exists(path))
                throw new ArgumentException($"Path does not exist or is not Directory.");
            if (Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories).Any())
                return false;
            return true;
        }
        
        
        /// <summary>
        /// Gets the Product Version in an OS independent manner
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string? GetExeVersion(string path)
        {
            StringTable[]? stringFileInfo = new PeNet.PeFile(path).Resources?.VsVersionInfo?.StringFileInfo.StringTable;
            if (stringFileInfo == null)
                return null;
            
            foreach (var version in stringFileInfo)
            {
                if (version.ProductVersion != null)
                    return version.ProductVersion;
            }

            return null;
        }
    }
}