using System;
using System.Collections.Generic;

namespace VolumetricSelection2077.Services
{
    public class UtilService
    {
        public string BuildORRegex(List<string> patterns)
        {
            return string.Join("|", patterns);
        }
        public string EscapeSlashes(string input)
        {
            return input.Replace("\\", "\\\\");
        }
        
        public string FormatElapsedTime(TimeSpan elapsed)
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
    }
}