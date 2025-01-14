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
        public string wkitAPIRefreshIntervalString
        {
            get => _settings.WolvenkitAPIRequestInterval.ToString();
            set
            {
                if (int.TryParse(value, out int interval) && interval >= 0)
                {
                    _settings.WolvenkitAPIRequestInterval = interval;
                    OnPropertyChanged();
                }
            }
        }

        public string wkitAPITimeoutString
        {
            get => _settings.WolvenkitAPIRequestTimeout.ToString();
            set
            {
                if (int.TryParse(value, out int Rtimeout) && Rtimeout >= 0)
                {
                    _settings.WolvenkitAPIRequestTimeout = Rtimeout;
                    OnPropertyChanged();
                }
            }
        }

        public string wkitAPIInactivityTimeout
        {
            get => _settings.WolvenkitAPIInactivityTimeout.ToString();
            set
            {
                if (int.TryParse(value, out int Itimeout) && Itimeout >= 0)
                {
                    _settings.WolvenkitAPIInactivityTimeout = Itimeout;
                    OnPropertyChanged();
                }
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    };
}