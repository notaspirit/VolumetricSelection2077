using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.Views;

public class SettingsViewPersistentCache
{
    private readonly SettingsService _settings;

    public bool InitialModdedResourceValue { get; }
    public string InitialGamePath { get; }
    public string InitialCachePath { get; set; }

    public bool CachePathChanged
    {
        get => InitialCachePath != _settings.Data.CacheDirectory;
    }
    
    public bool RequiresRestart
    {
        get => (InitialModdedResourceValue != _settings.Data.SupportModdedResources) || (InitialGamePath != _settings.Data.GameDirectory);
    }
    
    public SettingsViewPersistentCache(SettingsService settings)
    {
       _settings = settings;
       InitialModdedResourceValue = _settings.Data.SupportModdedResources;
       InitialGamePath = _settings.Data.GameDirectory;
       InitialCachePath = _settings.Data.CacheDirectory;
    }
}