using Avalonia.Controls;
using VolumetricSelection2077.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Interactivity;
using VolumetricSelection2077.ViewStructures;
using YamlDotNet.Serialization;

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
        
        private void RestartApplication_Click(object? sender, RoutedEventArgs e)
        {
            _settingsViewModel?.Settings.SaveSettings();
            var exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "VolumetricSelection2077.exe");
            Process.Start(exePath);
            Environment.Exit(0);
        }
        
        private void OnSettingsWindowClosed(object? sender, EventArgs e)
        {
            _settingsViewModel?.Settings.SaveSettings(); // Save settings when window closes
        }
        
    };
}