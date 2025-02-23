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
        
        private void SelectAllClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_settingsViewModel == null) return;
            for (int i = 0; i < _settingsViewModel.FilteredNodeTypeFilterItems.Count; i++)
            {
                var item = _settingsViewModel.FilteredNodeTypeFilterItems[i];
                var globalIndex = _settingsViewModel.NodeTypeFilterItems.IndexOf(item);
                _settingsViewModel.Settings.NodeTypeFilter[globalIndex] = true;
                item.IsChecked = true;
            }
            RefreshItems();
        }

        private void DeselectAllClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_settingsViewModel == null) return;
            for (int i = 0; i < _settingsViewModel.FilteredNodeTypeFilterItems.Count; i++)
            {
                var item = _settingsViewModel.FilteredNodeTypeFilterItems[i];
                var globalIndex = _settingsViewModel.NodeTypeFilterItems.IndexOf(item);
                _settingsViewModel.Settings.NodeTypeFilter[globalIndex] = false;
                item.IsChecked = false;
            }
            RefreshItems();
        }

        private void RefreshItems()
        {
            if (_settingsViewModel == null) return;
            foreach (var item in _settingsViewModel.FilteredNodeTypeFilterItems)
            {
                item.NotifyChange(_settingsViewModel.Settings.NodeTypeFilter);
            }
            
            foreach (var item in _settingsViewModel.NodeTypeFilterItems)
            {
                item.NotifyChange(_settingsViewModel.Settings.NodeTypeFilter);
            }
        }
        
        private void Label_Click(object sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            var label = sender as Label;
            var item = label?.DataContext as NodeTypeFilterItem;

            if (item != null)
            {
                item.IsChecked = !item.IsChecked;
            }
        }
    };
}