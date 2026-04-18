using Avalonia.Controls;
using VolumetricSelection2077.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _settingsViewModel;
        private readonly CacheService _cacheService;
        
        private bool _showedDialog;
        private bool _moveCache = true;
        
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
            _settingsViewModel = (DataContext as SettingsViewModel)!;
            _cacheService = CacheService.Instance;
            Closing += OnSettingsWindowClosing;
            Closed += OnSettingsWindowClosed;
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
            _settingsViewModel.CacheWorking = true;
            await Task.Run(() => _cacheService.ClearDatabase(CacheDatabases.Vanilla));
            _settingsViewModel.CacheWorking = false;
            UpdateCacheStats();
        }
        
        private async void ClearModdedCache_Click(object sender, RoutedEventArgs e)
        {
            if(!_cacheService.IsInitialized) return;
            _settingsViewModel.CacheWorking = true;
            await Task.Run(() => _cacheService.ClearDatabase(CacheDatabases.Modded));
            _settingsViewModel.CacheWorking = false;
            UpdateCacheStats();
        }
        
        private async void ClearVanillaBoundsCache_Click(object sender, RoutedEventArgs e)
        {
            if(!_cacheService.IsInitialized) return;
            _settingsViewModel.CacheWorking = true;
            await Task.Run(() => _cacheService.ClearDatabase(CacheDatabases.VanillaBounds));
            _settingsViewModel.CacheWorking = false;
            UpdateCacheStats();
        }
        
        private async void ClearModdedBoundsCache_Click(object sender, RoutedEventArgs e)
        {
            if(!_cacheService.IsInitialized) return;
            _settingsViewModel.CacheWorking = true;
            await Task.Run(() => _cacheService.ClearDatabase(CacheDatabases.ModdedBounds));
            _settingsViewModel.CacheWorking = false;
            UpdateCacheStats();
        }

        private void OpenBackupDir_Click(object sender, RoutedEventArgs e)
        {
            OsUtilsService.OpenFolder(_settingsViewModel.Settings.BackupDirectory);
        }
        
        private void OpenCustomSelectionDir_Click(object sender, RoutedEventArgs e)
        {
            OsUtilsService.OpenFolder(_settingsViewModel.Settings.CustomSelectionFilePath);
        }
        
        private void OpenCacheDir_Click(object sender, RoutedEventArgs e)
        {
            OsUtilsService.OpenFolder(_settingsViewModel.Settings.CacheDirectory);
        }
        
        private void OpenOutputDir_Click(object sender, RoutedEventArgs e)
        {
            OsUtilsService.OpenFolder(_settingsViewModel.Settings.OutputDirectory);
        }

        private void OpenCETDir_Click(object sender, RoutedEventArgs e)
        {
            OsUtilsService.OpenFolder(_settingsViewModel.Settings.CETInstallLocation);
        }
        
        private void OpenGameDir_Click(object sender, RoutedEventArgs e)
        {
            OsUtilsService.OpenFolder(_settingsViewModel.Settings.GameDirectory);
        }

        private async void OnSettingsWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_showedDialog || !_settingsViewModel.PersistentCache.CachePathChanged)
                return;
            _showedDialog = true;
            e.Cancel = true;
            var result = await DialogService.ShowDialog("Cache Path Changed!",
                "Move the current cache, or initialize a new cache at the new location (creating one if none exists)?",
                new [] { new DialogButton("Move", DialogButtonStyling.Primary), new DialogButton("Initialize", DialogButtonStyling.Secondary) },
                this);
            _moveCache = result == 0;
            Close();
        }
        
        private void OnSettingsWindowClosed(object? sender, EventArgs e)
        {
            _settingsViewModel.Settings.SaveSettings();
            if (_settingsViewModel.PersistentCache.CachePathChanged)
            {
                bool successMove = false;
                try
                {
                    successMove = !_moveCache || CacheService.Instance.Move(_settingsViewModel.PersistentCache.InitialCachePath,
                        _settingsViewModel.Settings.CacheDirectory);
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "Failed to move cache!");
                }

                if (successMove)
                {
                    _settingsViewModel.PersistentCache.InitialCachePath = _settingsViewModel.Settings.CacheDirectory;
                }
                else
                {
                    _settingsViewModel.Settings.CacheDirectory = _settingsViewModel.PersistentCache.InitialCachePath;
                    _settingsViewModel.Settings.SaveSettings();
                }
                
                CacheService.Instance.Dispose().Wait();
            }
            
            if (_settingsViewModel.PersistentCache.RequiresRestart)
                OsUtilsService.RestartApp();
            CacheService.Instance.Initialize();
        }

        private void ClearKnownBadResources_Click(object? sender, RoutedEventArgs e)
        {
            CacheService.Instance.ClearKnownBadResources();
            _settingsViewModel.RaiseOnPropertyChanged("ClearKnownBadResourcesLabel");
        }
    };
}