using System;
using System.ComponentModel;
using System.IO;
using Avalonia.Media.Imaging;
using VolumetricSelection2077.Resources;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.views;
using YamlDotNet.Core.Tokens;

namespace VolumetricSelection2077.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    { 
        public SettingsService Settings { get; set; }

        public SettingsViewPersistentCache PersistentCache { get; }

        private CacheService.CacheStats _cacheStats;
        public CacheService.CacheStats CacheStats
        {
            get => _cacheStats;
            set
            {
                _cacheStats = value;
                OnPropertyChanged(nameof(CacheStats));
                OnPropertyChanged(nameof(ClearVanillaCacheButtonLabel));
                OnPropertyChanged(nameof(ClearModdedCacheButtonLabel));
            }
        }
        
        public string ClearVanillaCacheButtonLabel => Labels.ClearVanillaCache + $" [ {CacheStats.VanillaEntries} files | {CacheStats.EstVanillaSize:F2} GB ]";
        
        public string ClearModdedCacheButtonLabel => Labels.ClearModdedCache + $" [ {CacheStats.ModdedEntries} files | {CacheStats.EstModdedSize:F2} GB ]";
        
        public bool CacheEnabled 
        {
            get => Settings.CacheEnabled;
            set
            {
                Settings.CacheEnabled = value;
                OnPropertyChanged(nameof(CacheEnabled));
            }
        }
        
        public bool AutoUpdateEnabled 
        {
            get => Settings.AutoUpdate;
            set
            {
                Settings.AutoUpdate = value;
                OnPropertyChanged(nameof(AutoUpdateEnabled));
            }
        }
        
        public Bitmap SettingsIcon { get; set; } = new Bitmap(Path.Combine(AppContext.BaseDirectory, "assets", "SettingsMSStyle.png"));
        
        public SettingsViewModel() 
        { 
            Settings = SettingsService.Instance;
            PersistentCache = SettingsViewPersistentCache.Instance;
            try
            {
                if (CacheService.Instance.IsInitialized)
                    CacheStats = CacheService.Instance.GetStats();
                else
                    CacheStats = new CacheService.CacheStats();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load Cache {ex}");
                CacheStats = new CacheService.CacheStats();
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
   }
}