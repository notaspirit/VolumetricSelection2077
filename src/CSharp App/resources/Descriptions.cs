namespace VolumetricSelection2077.Resources
{
        public static class ToolTips
        {
            public static string GameDirectory { get; } = "Path to the game directory (contains bin, archive folder etc.)";
            public static string NodeFilter { get; } = "Filter what nodes should be processed or skipped";
            public static string SaveAsYaml{ get; } = "Save as yaml or as json";
            public static string AllowOverwrite { get; } = "Allow overwriting the output file if one with the same name already exists, extending file contents takes priority";
            public static string ExtendExistingFile { get; } = "Extends the output file with the new content if it exists";
            public static string NukeOccluders { get; } = "Only use this option if you are having issues with occluders in your removal, as this setting removes them generously.";
            public static string NukeOccludersAggressively { get; } = "Removes occluders from all provided sectors, by default (off) only removoes occluders from sectors which intersect.";
            public static string OutputFilename { get; } =
                "Enter the output filename, without extension, supports sub folders";

            public static string ResourceFilter { get; } =
                "Filter by resource path, supports regex and partial matching, backslashes must be escaped in paths";

            public static string DebugNameFilter { get; } =
                "Filter by debug name, supports regex and partial matching, backslashes must be escaped in paths.";
            public static string FileSaveMode { get; } = "How to handle if the filename already exists";
            public static string ModdedResources { get; } = "Index and query modded resources, can cause significant increase in loading time, processing time is unaffected. Requires program restart to take effect.";
            public static string AutoUpdate { get; } = "Auto Update on startup if update is available";
            public static string CETInstallLocation { get; } = "Folder where updates to the VS2077 CET part should be installed to, if left empty will use game directory.";
            public static string CustomOutputDirectory { get; } = $"Alternative location to save files to, can be enabled in \"Optional Parameters\", defaults to {@"%AppData%\VolumetricSelection2077\output\"}";
            public static string SaveToArchiveMods { get; } = $"Where to save the output files to, custom directory defaults to {@"%AppData%\VolumetricSelection2077\output\"}";
            public static string CacheDirectory { get; } = $"Where the cache is saved to, can grow upto 100GB in size (all cached vanilla entries total up to 7.6GB), defaults to {@"%AppData%\VolumetricSelection2077\cache\"}";
            public static string CacheEnabled { get; } = "Caches uncooked game files, significantly improving processing time";
            public static string ClearCache { get; } = "Clear cached vanilla or modded files.";
            public static string ResolveProxies { get; } = "Whether to resolve proxies and adjust their expected nodes, or to treat them as normal nodes and remove them if they intersect.";
        }

        public static class Watermarks
        {
            public static string GameDirectory { get; } = "Path to game directory";
            public static string Search { get; } = "Search...";
            public static string OutputFilename { get; } = "Output Filename";
            public static string CETInstallLocation { get;  } ="VS2077 CET installation directory";
            public static string CustomOutputDirectory { get; } = "Custom Output Directory";
            public static string CacheDirectory { get; } = "Cache Directory";
        }

        public static class Labels
        {
            public static string SelectAll { get; } = "Select All";
            public static string DeselectAll { get; } = "Deselect All";
            public static string Settings { get; } = "Settings";
            public static string VS2077 { get; } = "VolumetricSelection2077";
            public static string FindSelected　{ get; } = "Find Selected";
            public static string Benchmark { get; } = "Benchmark";
            public static string DebugMenu { get; } = "Debug Menu";
            public static string ClearLog { get; } = "Clear Log";
            public static string ResourceFilter { get; } = " Resource Path Filters";
            public static string DebugNameFilter { get; } = " Debug Name Filters";
            public static string FilterCollapseButton { get; } = "Filters";
            public static string ParametersCollapseButton { get; } = "Optional Parameters";
            public static string SaveAsYaml { get; } = "Output Format";
            public static string FileSaveMode { get; } = "File Save Mode";
            public static string SaveToArchiveMods { get; } = "Save Files To";
            public static string ClearVanillaCache { get; } = "Vanilla";
            public static string ClearModdedCache { get; } = "Modded";
            public static string ResolveProxies { get; } = "Proxy Treatment";
            public static string ClearVanillaBoundsCache { get; } = "Vanilla Bounds";
            public static string ClearModdedBoundsCache { get; } = "Modded Bounds";
        }
}