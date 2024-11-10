using System.Collections.Generic;
using System.Linq;

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
    }
}