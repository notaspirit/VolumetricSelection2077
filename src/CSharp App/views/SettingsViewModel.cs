using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.Resources;
using VolumetricSelection2077.views;
using VolumetricSelection2077.ViewStructures;

namespace VolumetricSelection2077.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    { 
        public SettingsService Settings { get; set; }

        public SettingsViewPersistentCache Cache { get; private set; }

        public bool ModdedStatusChanged
        {
            get
            {
                return Cache.initialModdedResourceValue != Settings.SupportModdedResources;
            }
        }
        public bool ModdedResourceSupportSW
        {
            get => Settings.SupportModdedResources;
            set
            {
                Settings.SupportModdedResources = value;
                OnPropertyChanged(nameof(ModdedResourceSupportSW));
                OnPropertyChanged(nameof(ModdedStatusChanged));
            }
        }
        
        public SettingsViewModel() 
        { 
            Settings = SettingsService.Instance;
            Cache = SettingsViewPersistentCache.Instance;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
   }
}