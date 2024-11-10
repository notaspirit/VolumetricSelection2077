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
        public string WolvenkitCLIPathTooltip { get; set; } = "Path to the Wolvenkit CLI executable directory";
    }
}