using Avalonia.Controls;
using VolumetricSelection2077.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Interactivity;

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
        
        private void RestartApplication_Click(object? sender, RoutedEventArgs e)
        {
            _settingsViewModel?.Settings.SaveSettings();
            RestartApp();
        }
        
        private void OnSettingsWindowClosed(object? sender, EventArgs e)
        {
            _settingsViewModel?.Settings.SaveSettings();
            if ((bool)_settingsViewModel?.Cache.RequiresRestart)
                RestartApp();
        }
    };
}