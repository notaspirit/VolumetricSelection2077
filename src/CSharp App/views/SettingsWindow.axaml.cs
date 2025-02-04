using Avalonia.Controls;
using VolumetricSelection2077.ViewModels;
using System;
using VolumetricSelection2077.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.VisualTree;
using Avalonia.Input;

namespace VolumetricSelection2077
{
    public partial class SettingsWindow : Window
    {

        private SettingsService _settings;
        public SettingsWindow()
        {
            _settings = SettingsService.Instance;
            InitializeComponent();
            DataContext = new SettingsViewModel();
            this.Closed += new EventHandler(OnSettingsWindowClosed);
        }
        private void OnSettingsWindowClosed(object? sender, EventArgs e)
        {
            var viewModel = DataContext as SettingsViewModel;
            viewModel?.Settings.SaveSettings(); // Save settings when window closes
        }
    };
}