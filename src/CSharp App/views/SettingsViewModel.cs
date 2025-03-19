using System.ComponentModel;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.views;

namespace VolumetricSelection2077.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    { 
        public SettingsService Settings { get; set; }

        public SettingsViewPersistentCache PersistentCache { get; }
        
        public SettingsViewModel() 
        { 
            Settings = SettingsService.Instance;
            PersistentCache = SettingsViewPersistentCache.Instance;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
   }
}