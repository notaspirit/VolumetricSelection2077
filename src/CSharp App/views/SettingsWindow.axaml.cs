using Avalonia.Controls;
using VolumetricSelection2077.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Interactivity;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077
{
    public partial class SettingsWindow : Window
    {
        private SettingsViewModel? _settingsViewModel;
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
            _settingsViewModel = DataContext as SettingsViewModel;
            Closed += OnSettingsWindowClosed;
        }

        private void RestartApp()
        {
            var exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "VolumetricSelection2077.exe");
            Process.Start(exePath);
            Environment.Exit(0);
        }
        
        private void OnSettingsWindowClosed(object? sender, EventArgs e)
        {
            _settingsViewModel?.Settings.SaveSettings();

            if ((bool)_settingsViewModel?.PersistentCache.CacheChanged)
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
                    _settingsViewModel.PersistentCache.InitialCachePath = _settingsViewModel?.Settings.CacheDirectory;
                else
                {
                    _settingsViewModel.Settings.CacheDirectory = _settingsViewModel?.PersistentCache.InitialCachePath;
                    _settingsViewModel?.Settings.SaveSettings();
                }

            }
            
            if ((bool)_settingsViewModel?.PersistentCache.RequiresRestart)
                RestartApp();
        }
    };
}