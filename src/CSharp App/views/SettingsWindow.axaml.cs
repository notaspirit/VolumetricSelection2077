using Avalonia.Controls;
using VolumetricSelection2077.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.views;

namespace VolumetricSelection2077
{
    public partial class SettingsWindow : Window
    {
        private SettingsViewModel? _settingsViewModel;
        private CacheService _cacheService;
        
        private bool showedDialog;
        private bool moveCache = true;
        
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
            _settingsViewModel = DataContext as SettingsViewModel;
            _cacheService = CacheService.Instance;
            Closing += OnSettingsWindowClosing;
            Closed += OnSettingsWindowClosed;
            Opened += OnOpened;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            if (Owner is Window parentWindow)
            {
                double x = parentWindow.Position.X + (parentWindow.Bounds.Width - Bounds.Width) / 2;
                double y = parentWindow.Position.Y + (parentWindow.Bounds.Height - Bounds.Height) / 2;
            
                Position = new PixelPoint((int)x, (int)y);
            }
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

        private async void OnSettingsWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!showedDialog && (bool)_settingsViewModel?.PersistentCache.CachePathChanged)
            {
                showedDialog = true;
                e.Cancel = true;
                var result = await DialogService.ShowDialog("Cache Path Changed!",
                    "Move the current cache, or initialize a new cache at the new location (creating one if none exists)?",
                    "Move",
                    "Initialize",
                    this);
                moveCache = result == DialogService.DialogResult.LeftButton;
                Close();
            }
        }
        
        private void OnSettingsWindowClosed(object? sender, EventArgs e)
        {
            _settingsViewModel?.Settings.SaveSettings();
            if ((bool)_settingsViewModel?.PersistentCache.CachePathChanged)
            {
                bool successMove = false;
                try
                {
                    successMove = moveCache ? CacheService.Instance.Move(_settingsViewModel?.PersistentCache.InitialCachePath,
                        _settingsViewModel?.Settings.CacheDirectory) : true;
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "Failed to move cache!");
                }

                if (successMove)
                {
                    _settingsViewModel.PersistentCache.InitialCachePath = _settingsViewModel?.Settings.CacheDirectory;
                }
                else
                {
                    _settingsViewModel.Settings.CacheDirectory = _settingsViewModel?.PersistentCache.InitialCachePath;
                    _settingsViewModel?.Settings.SaveSettings();
                }
            }

            if ((bool)_settingsViewModel?.Settings.CacheEnabled)
                CacheService.Instance.Initialize();
            else
                CacheService.Instance.Dispose();
            
            if ((bool)_settingsViewModel?.PersistentCache.RequiresRestart)
                RestartApp();
        }
    };
}