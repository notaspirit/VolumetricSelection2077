using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.Views;

public class SettingsViewPersistentCache
{
    private readonly SettingsService _settingsService;

    public bool InitialModdedResourceValue { get; }
    public string InitialGamePath { get; }
    public string InitialCachePath { get; set; }

    public bool CachePathChanged
    {
        get => InitialCachePath != _settingsService.Data.CacheDirectory;
    }
    
    public bool RequiresRestart
    {
        get => (InitialModdedResourceValue != _settingsService.Data.SupportModdedResources) || (InitialGamePath != _settingsService.Data.GameDirectory);
    }
    
    public SettingsViewPersistentCache(SettingsService settingsService)
    {
       _settingsService = settingsService;
       InitialModdedResourceValue = _settingsService.Data.SupportModdedResources;
       InitialGamePath = _settingsService.Data.GameDirectory;
       InitialCachePath = _settingsService.Data.CacheDirectory;
    }
}