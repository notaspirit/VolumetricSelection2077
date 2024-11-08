namespace VolumetricSelection2077.Resources
{
    public class Descriptions
    {
        public string GameDirectoryTooltip { get; set; } = "Path to the game directory (contains bin, archive folder etc.)";
        public string CacheTooltip { get; set; } = "Toggle all caching options, it is recommended to keep it enabled as it saves time on processing, however it may require a lot of disk space";
        public string CacheSectorDataTooltip { get; set; } = "Cache sector data";
        public string CacheMeshDataTooltip { get; set; } = "Cache mesh data";
        public string CacheGeometryCacheTooltip { get; set; } = "Cache geometry cache file";
        public string ClearCacheTooltip { get; set; } = "Clear the cache";
        public string CacheDirectoryTooltip { get; set; } = "Path to the cache directory (defaults to %AppData%/VolumetricSelection2077/cache)";
        public string SaveToArchiveModsTooltip { get; set; } = "Ignores the output directory and saves the '.xl' file to {game directory}/archive/pc/mod";
        public string OutputDirectoryTooltip { get; set; } = "Path to the output directory (defaults to %AppData%/VolumetricSelection2077/output)";
        public string WolvenkitCLIPathTooltip { get; set; } = "Path to the Wolvenkit CLI executable directory";
    }
}