using Avalonia.Controls;
using VolumetricSelection2077.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using VolumetricSelection2077.Models;
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

        private const double ResizeAfterBytes = 1024 * 1024 * 1024; // 1GB
        
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
        
        /// <summary>
        /// Checks if there is enough space on the drive and if the change is significant enough to resize the database
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">if CacheDatabases.All is passed</exception>
        private bool ShouldResize(CacheDatabases db)
        {
            FileSize sizeToRemove;
            FileSize totalSize = new FileSize(_settingsViewModel.CacheStats.EstVanillaSize.Bytes + 
                                              _settingsViewModel.CacheStats.EstModdedSize.Bytes +
                                              _settingsViewModel.CacheStats.EstVanillaBoundsSize.Bytes +
                                              _settingsViewModel.CacheStats.EstModdedBoundsSize.Bytes);
            
            switch (db)
            {
                case CacheDatabases.Vanilla:
                    sizeToRemove = _settingsViewModel.CacheStats.EstVanillaSize;
                    break;
                case CacheDatabases.Modded:
                    sizeToRemove = _settingsViewModel.CacheStats.EstModdedSize;
                    break;
                case CacheDatabases.VanillaBounds:
                    sizeToRemove = _settingsViewModel.CacheStats.EstVanillaBoundsSize;
                    break;
                case CacheDatabases.ModdedBounds:
                    sizeToRemove = _settingsViewModel.CacheStats.EstModdedBoundsSize;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(db), db, null);
            }
            
            var cacheDriveInfo = new DriveInfo(_settingsViewModel.Settings.CacheDirectory);
            var freeSpace = (ulong)cacheDriveInfo.AvailableFreeSpace;
            
            return sizeToRemove.Bytes > ResizeAfterBytes && freeSpace > totalSize.Bytes - sizeToRemove.Bytes;
        }
        
        private async void ClearVanillaCache_Click(object sender, RoutedEventArgs e)
        {
            if(!_cacheService.IsInitialized) return;
            await Task.Run(() => _cacheService.ClearDatabase(CacheDatabases.Vanilla, ShouldResize(CacheDatabases.Vanilla)));
            UpdateCacheStats();
        }
        
        private async void ClearModdedCache_Click(object sender, RoutedEventArgs e)
        {
            if(!_cacheService.IsInitialized) return;
            await Task.Run(() => _cacheService.ClearDatabase(CacheDatabases.Modded, ShouldResize(CacheDatabases.Modded)));;
            UpdateCacheStats();
        }
        
        private async void ClearVanillaBoundsCache_Click(object sender, RoutedEventArgs e)
        {
            if(!_cacheService.IsInitialized) return;
            await Task.Run(() => _cacheService.ClearDatabase(CacheDatabases.VanillaBounds, ShouldResize(CacheDatabases.VanillaBounds)));
            UpdateCacheStats();
        }
        
        private async void ClearModdedBoundsCache_Click(object sender, RoutedEventArgs e)
        {
            if(!_cacheService.IsInitialized) return;
            await Task.Run(() => _cacheService.ClearDatabase(CacheDatabases.ModdedBounds, ShouldResize(CacheDatabases.ModdedBounds)));
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
                    new [] { "Move", "Initialize" },
                    this);
                moveCache = result == 0;
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
                
                CacheService.Instance.Dispose().Wait();
            }
            
            if ((bool)_settingsViewModel?.PersistentCache.RequiresRestart)
                RestartApp();
            CacheService.Instance.Initialize();
        }
    };
}