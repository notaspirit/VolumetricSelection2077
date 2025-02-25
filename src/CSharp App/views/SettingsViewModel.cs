using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.Resources;
using VolumetricSelection2077.ViewStructures;

namespace VolumetricSelection2077.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    { 
        public SettingsService Settings { get; set; }
        public SettingsViewModel() 
        { 
            Settings = SettingsService.Instance;
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
   }
}