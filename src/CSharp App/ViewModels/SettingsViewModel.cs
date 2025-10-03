using System;
using System.ComponentModel;
using System.IO;
using Avalonia.Media.Imaging;
using VolumetricSelection2077.Resources;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.Views;

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
                OnPropertyChanged(nameof(ClearVanillaBoundsCacheButtonLabel));
                OnPropertyChanged(nameof(ClearModdedBoundsCacheButtonLabel));
            }
        }
        
        public string ClearVanillaCacheButtonLabel => Labels.ClearVanillaCache + $" [ {CacheStats.VanillaEntries} files | {CacheStats.EstVanillaSize.GetFormattedSize()} ]";
        
        public string ClearModdedCacheButtonLabel => Labels.ClearModdedCache + $" [ {CacheStats.ModdedEntries} files | {CacheStats.EstModdedSize.GetFormattedSize()} ]";
        
        public string ClearVanillaBoundsCacheButtonLabel => Labels.ClearVanillaBoundsCache + $" [ {CacheStats.VanillaBoundsEntries} files | {CacheStats.EstVanillaBoundsSize.GetFormattedSize()} ]";
        
        public string ClearModdedBoundsCacheButtonLabel => Labels.ClearModdedBoundsCache + $" [ {CacheStats.ModdedBoundsEntries} files | {CacheStats.EstModdedBoundsSize.GetFormattedSize()} ]";
        
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
        
        public Bitmap SettingsIcon { get; set; }
        
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
                Logger.Exception(ex, $"Failed to load Cache!");
                CacheStats = new CacheService.CacheStats();
            }

            try
            {
                SettingsIcon = new Bitmap(Path.Combine(AppContext.BaseDirectory, "assets", "SettingsMSStyle.png"));
            }
            catch(Exception ex)
            {
                Logger.Exception(ex, $"Failed to load Settings Icon!");
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
   }
}