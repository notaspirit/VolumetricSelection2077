using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Resources;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.Views;

namespace VolumetricSelection2077.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    { 
        private CacheStats _cacheStats;
        private bool _cacheWorking;
        
        public List<Enums.ExperimentalSettingsEnum.ProxyMeshTreatment> ProxyMeshTreatmentOptions { get; set; }

        [ObservableProperty] 
        private SettingsService _settings;
        public SettingsViewPersistentCache PersistentCache { get; }
        public CacheStats CacheStats
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
        
        public bool CacheWorking
        {
            get => _cacheWorking;
            set
            {
                _cacheWorking = value;
                OnPropertyChanged(nameof(CacheWorking));
                OnPropertyChanged(nameof(CacheButtonsAvailable));
            }
        }
        
        public bool CacheButtonsAvailable => !CacheWorking;
        public Bitmap SettingsIcon { get; set; }
        
        public SettingsViewModel() 
        { 
            Settings = SettingsService.Instance;
            PersistentCache = SettingsViewPersistentCache.Instance;
            try
            {
                _cacheStats = CacheService.Instance.IsInitialized ? CacheService.Instance.GetStats() : new CacheStats();
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"Failed to load Cache!");
                _cacheStats = new CacheStats();
            }

            try
            {
                SettingsIcon = new Bitmap(Path.Combine(AppContext.BaseDirectory, "assets", "SettingsMSStyle.png"));
            }
            catch(Exception ex)
            {
                Logger.Exception(ex, $"Failed to load Settings Icon!");
                SettingsIcon = new WriteableBitmap(new PixelSize(1,1), new Vector(1,1), PixelFormat.Bgra8888, AlphaFormat.Premul);
            }

            ProxyMeshTreatmentOptions = new(Enum.GetValues(typeof(Enums.ExperimentalSettingsEnum.ProxyMeshTreatment))
                .Cast<Enums.ExperimentalSettingsEnum.ProxyMeshTreatment>());
        }
   }
}