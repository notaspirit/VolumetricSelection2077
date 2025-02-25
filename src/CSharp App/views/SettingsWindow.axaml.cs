using Avalonia.Controls;
using VolumetricSelection2077.ViewModels;
using System;
using VolumetricSelection2077.ViewStructures;

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
        
        private void OnSettingsWindowClosed(object? sender, EventArgs e)
        {
            _settingsViewModel?.Settings.SaveSettings(); // Save settings when window closes
        }
        
    };
}