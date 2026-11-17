using VolumetricSelection2077.App.Services;

namespace VolumetricSelection2077.App.Views;

public class SettingsViewPersistentCache
{
    private readonly SettingsService _settingsService;
    public string InitialGamePath { get; }
    public string InitialCachePath { get; set; }

    public bool CachePathChanged
    {
        get => InitialCachePath != _settingsService.Data.CacheDirectory;
    }
    
    public bool RequiresRestart
    {
        get => InitialGamePath != _settingsService.Data.GameDirectory;
    }
    
    public SettingsViewPersistentCache(SettingsService settingsService)
    {
       _settingsService = settingsService;
       InitialGamePath = _settingsService.Data.GameDirectory;
       InitialCachePath = _settingsService.Data.CacheDirectory;
    }
}