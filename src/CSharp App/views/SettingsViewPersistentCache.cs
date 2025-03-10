using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.views;

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

    public bool initialModdedResourceValue { get; private set; }

    private SettingsViewPersistentCache()
    {
       _settings = SettingsService.Instance;
       initialModdedResourceValue = _settings.SupportModdedResources;
    }
}