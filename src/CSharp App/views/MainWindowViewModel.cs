using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using DynamicData;
using VolumetricSelection2077.Resources;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public SettingsService Settings { get; set; }
        private bool _isProcesing { get; set; }
        
        public bool IsProcessing
        {
            get => _isProcesing;
            set
            {
                _isProcesing = value;
                OnPropertyChanged(nameof(IsProcessing));
            }
        }

        public int ResourcePathFilterCount => Settings.ResourceNameFilter.Count;
        public int DebugNameFilterCount => Settings.DebugNameFilter.Count;

        public bool FilterModeOr
        {
            get => Settings.FilterModeOr;
            set
            {
                Settings.FilterModeOr = value;
                OnPropertyChanged(nameof(FilterModeText));
                OnPropertyChanged(nameof(FilterModeOr));
            }
        }
        
        public string FilterModeText => FilterModeOr ? "Or" : "And";

        private ObservableCollection<string> _resourceNameFilter;

        public string OutputFilename
        {
            get => Settings.OutputFilename;
            set
            {
                Settings.OutputFilename = value;
                OnPropertyChanged(nameof(OutputFilename));
                Settings.SaveSettings();
            }
        }

        public string FilterSectionButtonLabel => Labels.FilterCollapseButton + (FilterSelectionVisibility ? " \u02c5" : " \u02c4");
        public bool FilterSelectionVisibility
        {
            get => Settings.IsFiltersMWVisible;
            set
            {
                Settings.IsFiltersMWVisible = value;
                OnPropertyChanged(nameof(FilterSelectionVisibility));
                OnPropertyChanged(nameof(FilterSectionButtonLabel));
                Settings.SaveSettings();
            }
        }
        public MainWindowViewModel()
        {
            Settings = SettingsService.Instance;
            _resourceNameFilter = Settings.ResourceNameFilter;
            _resourceNameFilter.CollectionChanged += ResourceNameFilter_CollectionChanged;
            Settings.DebugNameFilter.CollectionChanged += DebugNameFilter_CollectionChanged;
        }
        private void ResourceNameFilter_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ResourcePathFilterCount));
        }

        private void DebugNameFilter_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(DebugNameFilterCount));
        }
        
        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
