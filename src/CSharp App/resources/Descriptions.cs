namespace VolumetricSelection2077.Resources
{
    public class Descriptions
    {
        public string GameDirectoryTooltip { get; set; } = "Path to the game directory (contains bin, archive folder etc.)";
        public string NodeFilterTooltip { get; set; } = "Filter what nodes should be processed or skipped";
        public string SaveAsYamlToolTip { get; set; } = "Save as yaml or as json";
        public string AllowOverwriteTooltip { get; set; } = "Allow overwriting the output file if one with the same name already exists, extending file contents takes priority";
        public string ExtendExistingFileTooltip { get; set; } = "Extends the output file with the new content if it exists";
    }
}