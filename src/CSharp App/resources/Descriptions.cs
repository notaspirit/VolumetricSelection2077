namespace VolumetricSelection2077.Resources
{
    public class Descriptions
    {
        public string GameDirectoryTooltip { get; set; } = "Path to the game directory (contains bin, archive folder etc.)";
        public string CacheTooltip { get; set; } = "Will cache everything that is extracted from the archive, if disabled it will delete most of the cache after finishing";
        public string ClearCacheTooltip { get; set; } = "Clear the cache";
        public string CacheDirectoryTooltip { get; set; } = "Path to the cache directory (defaults to %AppData%/VolumetricSelection2077/cache)";
        public string SaveToArchiveModsTooltip { get; set; } = "Ignores the output directory and saves the '.xl' file to {game directory}/archive/pc/mod";
        public string OutputDirectoryTooltip { get; set; } = "Path to the output directory (defaults to %AppData%/VolumetricSelection2077/output)";
        public string NodeFilterTooltip { get; set; } = "Filter what nodes should be processed or skipped";
        public string SaveAsYamlToolTip { get; set; } = "Save as yaml or as json";
        public string AllowOverwriteTooltip { get; set; } = "Allow overwriting the output file if one with the same name already exists, extending file contents takes priority";
        public string ExtendExistingFileTooltip { get; set; } = "Extends the output file with the new content if it exists";
    }
}