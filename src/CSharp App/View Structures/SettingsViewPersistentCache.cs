using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.Views;

public class SettingsViewPersistentCache
{
    private static SettingsViewPersistentCache? _instance;
    private static readonly object _lock = new object();
    private readonly SettingsService _settings;
    public static SettingsViewPersistentCache Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new SettingsViewPersistentCache();
                }
                return _instance;
            }
        }
    }

    private bool InitialModdedResourceValue { get; }
    private string InitialGamePath { get; }
    public string InitialCachePath { get; set; }

    public bool CachePathChanged
    {
        get => InitialCachePath != _settings.CacheDirectory;
    }
    
    public bool RequiresRestart
    {
        get => (InitialModdedResourceValue != _settings.SupportModdedResources) || (InitialGamePath != _settings.GameDirectory);
    }
    
    private SettingsViewPersistentCache()
    {
       _settings = SettingsService.Instance;
       InitialModdedResourceValue = _settings.SupportModdedResources;
       InitialGamePath = _settings.GameDirectory;
       InitialCachePath = _settings.CacheDirectory;
    }
}