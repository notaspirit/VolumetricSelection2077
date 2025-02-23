namespace VolumetricSelection2077.Resources
{
    public class Descriptions
    {
        public string GameDirectoryTooltip { get; set; } = "Path to the game directory (contains bin, archive folder etc.)";
        public string NodeFilterTooltip { get; set; } = "Filter what nodes should be processed or skipped";
        public string SaveAsYamlToolTip { get; set; } = "Save as yaml or as json";
        public string AllowOverwriteTooltip { get; set; } = "Allow overwriting the output file if one with the same name already exists, extending file contents takes priority";
        public string ExtendExistingFileTooltip { get; set; } = "Extends the output file with the new content if it exists";
        public string NukeOccludersTooltip { get; set; } = "Only use this option if you are having issues with occluders in your removal, as this setting removes them generously.";
        public string NukeOccludersAggressivelyTooltip { get; set; } = "Removes occluders from all provided sectors, by default (off) only removoes occluders from sectors which intersect.";
    }
}