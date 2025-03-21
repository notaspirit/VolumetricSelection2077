using Avalonia.Controls;
using VolumetricSelection2077.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077
{
    public partial class SettingsWindow : Window
    {
        private SettingsViewModel? _settingsViewModel;
        private CacheService _cacheService;
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
            _settingsViewModel = DataContext as SettingsViewModel;
            _cacheService = CacheService.Instance;
            Closed += OnSettingsWindowClosed;
        }

        private void RestartApp()
        {
            var exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "VolumetricSelection2077.exe");
            Process.Start(exePath);
            Environment.Exit(0);
        }

        private void UpdateCacheStats()
        {
            try
            {
                _settingsViewModel.CacheStats = _cacheService.GetStats();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get stats for cache: {ex}");
            }
        }
        private async void ClearVanillaCache_Click(object sender, RoutedEventArgs e)
        {
            if(!_cacheService.IsInitialized) return;
            await Task.Run(() => _cacheService.ClearDatabase(CacheDatabases.Vanilla, true));
            UpdateCacheStats();
        }
        
        private async void ClearModdedCache_Click(object sender, RoutedEventArgs e)
        {
            if(!_cacheService.IsInitialized) return;
            await Task.Run(() => _cacheService.ClearDatabase(CacheDatabases.Modded, true));
            UpdateCacheStats();
        }
        
        private void OnSettingsWindowClosed(object? sender, EventArgs e)
        {
            _settingsViewModel?.Settings.SaveSettings();
            if ((bool)_settingsViewModel?.PersistentCache.CachePathChanged)
            {
                bool successMove = false;
                try
                {
                    successMove = CacheService.Instance.Move(_settingsViewModel?.PersistentCache.InitialCachePath,
                        _settingsViewModel?.Settings.CacheDirectory);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to move Cache {ex}");
                }

                if (successMove)
                {
                    _settingsViewModel.PersistentCache.InitialCachePath = _settingsViewModel?.Settings.CacheDirectory;
                    RestartApp();
                }
                else
                {
                    _settingsViewModel.Settings.CacheDirectory = _settingsViewModel?.PersistentCache.InitialCachePath;
                    _settingsViewModel?.Settings.SaveSettings();
                }
            }

            if ((bool)_settingsViewModel?.Settings.CacheEnabled)
                CacheService.Instance.Initialize();
            
            if ((bool)_settingsViewModel?.PersistentCache.RequiresRestart)
                RestartApp();
        }
    };
}