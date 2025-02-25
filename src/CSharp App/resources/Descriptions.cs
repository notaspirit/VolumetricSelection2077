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
        }

        public static class Watermarks
        {
            public static string GameDirectory { get; } = "Path to game directory";
            public static string Search { get; } = "Search...";
            public static string OutputFilename { get; } = "Output Filename";
        }

        public static class Labels
        {
            public static string SelectAll { get; } = "Select All";
            public static string DeselectAll { get; } = "Deselect All";
            public static string Settings { get; } = "Settings";
            public static string VS2077 { get; } = "VolumetricSelection2077";
            public static string FindSelected　{ get; } = "Find Selected";
            public static string Benchmark { get; } = "Benchmark";
            public static string ClearLog { get; } = "Clear Log";
            public static string ResourceFilter { get; } = " Resource Path Filters";
            public static string DebugNameFilter { get; } = " Debug Name Filters";
            public static string FilterCollapseButton = "Filters";

        }
}