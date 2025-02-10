using Avalonia.Controls;
using VolumetricSelection2077.ViewModels;
using System;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
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