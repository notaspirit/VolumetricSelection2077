using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using DynamicData;
using VolumetricSelection2077.Resources;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.ViewStructures;

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
                OnPropertyChanged(nameof(NukeOccluderBoolSettingsAggressiveVisibility));
                Settings.SaveSettings();
            }
        }
        public MainWindowViewModel()
        {
            Settings = SettingsService.Instance;
            _resourceNameFilter = Settings.ResourceNameFilter;
            _resourceNameFilter.CollectionChanged += ResourceNameFilter_CollectionChanged;
            Settings.DebugNameFilter.CollectionChanged += DebugNameFilter_CollectionChanged;
            _nodeTypeFilterItems = new();
            for (int i = 0; i < NodeTypeProcessingOptions.NodeTypeOptions.Length; i++)
            {
                var item = new NodeTypeFilterItem(NodeTypeProcessingOptions.NodeTypeOptions[i], i, Settings.NodeTypeFilter);
                item.PropertyChanged += OnNodeTypeFilterItemChanged;
                NodeTypeFilterItems.Add(item);
            }
            _filteredNodeTypeFilterItems = _nodeTypeFilterItems;
            CheckedCount = NodeTypeFilterItems.Count(item => item.IsChecked);
        }
        private void ResourceNameFilter_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ResourcePathFilterCount));
        }

        private void DebugNameFilter_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(DebugNameFilterCount));
        }
        
        private string _searchQuery;
        private ObservableCollection<NodeTypeFilterItem> _nodeTypeFilterItems;
        private ObservableCollection<NodeTypeFilterItem> _filteredNodeTypeFilterItems;
        private int _checkedCount;
        public ObservableCollection<NodeTypeFilterItem> NodeTypeFilterItems
        {
            get => _nodeTypeFilterItems;
            set
            {
                if (_nodeTypeFilterItems == value) return;
                _nodeTypeFilterItems = value;
                OnPropertyChanged(nameof(NodeTypeFilterItems));
                CheckedCount = NodeTypeFilterItems.Count(item => item.IsChecked);
            }
        }

        public ObservableCollection<NodeTypeFilterItem> FilteredNodeTypeFilterItems
        {
            get => _filteredNodeTypeFilterItems;
            set
            {
                if (_filteredNodeTypeFilterItems == value) return;
                _filteredNodeTypeFilterItems = value;
                OnPropertyChanged(nameof(FilteredNodeTypeFilterItems));
                CheckedCount = NodeTypeFilterItems.Count(item => item.IsChecked);
            }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery == value) return;
                _searchQuery = value;
                OnPropertyChanged(nameof(SearchQuery));
                FilterItems();
            }
        }
        
        public int CheckedCount
        {
            get => _checkedCount;
            set
            {
                if (_checkedCount != value)
                {
                    _checkedCount = value;
                    OnPropertyChanged(nameof(CheckedCount)); // Notify the UI that CheckedCount has changed
                }
            }
        }
        public int TotalCount => NodeTypeFilterItems.Count();
        
        private void FilterItems()
        {
            if (string.IsNullOrWhiteSpace(_searchQuery))
            {
                FilteredNodeTypeFilterItems = new ObservableCollection<NodeTypeFilterItem>(_nodeTypeFilterItems);
            }
            else
            {
                var filtered = _nodeTypeFilterItems.Where(item => item.Label.ToLower().Contains(_searchQuery.ToLower())).ToList();
                FilteredNodeTypeFilterItems = new ObservableCollection<NodeTypeFilterItem>(filtered);
            }
        }

        public bool NukeOccludersBoolSettings
        {
            get => Settings.NukeOccluders;
            set
            {
                if (value != Settings.NukeOccluders)
                {
                    Settings.NukeOccluders = value;
                    OnPropertyChanged(nameof(NukeOccludersBoolSettings));
                    OnPropertyChanged(nameof(NukeOccluderBoolSettingsAggressiveVisibility));
                }
            }
        }

        public bool NukeOccluderBoolSettingsAggressiveVisibility
        {
            get => (Settings.NukeOccluders && FilterSelectionVisibility);
            set
            {
                if (value != Settings.NukeOccluders)
                {
                    Settings.NukeOccluders = value;
                    OnPropertyChanged(nameof(NukeOccludersBoolSettings));
                }
            }
        }
        private void OnNodeTypeFilterItemChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NodeTypeFilterItem.IsChecked))
            {
                CheckedCount = NodeTypeFilterItems.Count(item => item.IsChecked);
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
