namespace VolumetricSelection2077.Enums;

public struct InputValidationResult
{
    public bool CacheStatus { get; set; }
    public bool GameFileServiceStatus { get; set; }
    public bool ValidOutputDirectory { get; set; }
    public PathValidationResult OutputDirectroyPathValidationResult { get; set; }
    public bool SelectionFileExists { get; set; }
    public PathValidationResult SelectionFilePathValidationResult { get; set; }
    public PathValidationResult OutputFileName { get; set; }
    public bool ResourceNameFilterValid { get; set; }
    public bool DebugNameFilterValid { get; set; }
    public bool VanillaSectorBBsBuild { get; set; }
    public bool ModdedSectorBBsBuild { get; set; }
    public bool SubtractionTargetExists { get; set; }
}