using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.Resources;
using VolumetricSelection2077.ViewStructures;

namespace VolumetricSelection2077.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    { 
        private string _searchQuery;
        private ObservableCollection<NodeTypeFilterItem> _nodeTypeFilterItems;
        private ObservableCollection<NodeTypeFilterItem> _filteredNodeTypeFilterItems;
        private int _checkedCount;
        public Descriptions Descriptions { get; } 
        public SettingsService Settings { get; }
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

        public bool NukeOccludersBoolSettings
        {
            get => Settings.NukeOccluders;
            set
            {
                if (value != Settings.NukeOccluders)
                {
                    Settings.NukeOccluders = value;
                    OnPropertyChanged(nameof(NukeOccludersBoolSettings));
                }
            }
        }
        
        public SettingsViewModel() 
        { 
            Descriptions = new Descriptions(); 
            Settings = SettingsService.Instance;
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
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private void OnNodeTypeFilterItemChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NodeTypeFilterItem.IsChecked))
            {
                CheckedCount = NodeTypeFilterItems.Count(item => item.IsChecked);
            }
        }
   }
}