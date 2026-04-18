using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using VolumetricSelection2077.Resources;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.Views;
using VolumetricSelection2077.ViewStructures;
using VolumetricSelection2077.Enums;

namespace VolumetricSelection2077.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private bool _isProcessing;
        private bool _isMainTaskProcessing;
        private bool _isAppInitialized;
        private bool _isBenchmarkProcessing;
        private bool _isSettingsOpen;
        private string _searchQuery;
        private ObservableCollection<NodeTypeFilterItem> _nodeTypeFilterItems;
        private ObservableCollection<NodeTypeFilterItem> _filteredNodeTypeFilterItems;
        private int _checkedCount;
        private readonly ObservableCollection<string> _resourceNameFilter;

        [ObservableProperty]
        private SettingsService _settings;
        public Bitmap VS2077Icon { get; set; }
        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged(nameof(IsProcessing));
                OnPropertyChanged(nameof(ButtonsAvailable));
                OnPropertyChanged(nameof(MainTaskAvailable));
            }
        }
        public DebugWindow? DebugWindowInstance { get; set; }
        public bool DebugWindowButtonEnabled => DebugWindowInstance == null;
        public bool DebugWindowButtonSpinnerEnabled => DebugWindowInstance != null;
        public bool ButtonsAvailable => !_isProcessing;

        public bool MainTaskProcessing
        {
            get => _isMainTaskProcessing;
            set
            {
                _isMainTaskProcessing = value;
                IsProcessing = _isMainTaskProcessing;
                OnPropertyChanged(nameof(MainTaskProcessing));
            }
        }

        public bool AppInitialized
        {
            get => _isAppInitialized;
            set
            {
                _isAppInitialized = value;
                OnPropertyChanged(nameof(AppInitialized));
            }
        }

        public bool MainTaskAvailable => AppInitialized && ButtonsAvailable;

        public bool BenchmarkProcessing
        {
            get => _isBenchmarkProcessing;
            set
            {
                _isBenchmarkProcessing = value;
                IsProcessing = _isBenchmarkProcessing;
                OnPropertyChanged(nameof(BenchmarkProcessing));
            }
        }
        
        public bool SettingsOpen
        {
            get => _isSettingsOpen;
            set
            {
                _isSettingsOpen = value;
                IsProcessing = _isSettingsOpen;
                OnPropertyChanged(nameof(SettingsOpen));
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

        public string FilterSectionButtonLabel => Labels.FilterCollapseButton +
                                                  $" [ {(Settings.DebugNameFilter.Count == 0 ? 0 : 1) 
                                                        + (Settings.ResourceNameFilter.Count == 0 ? 0 : 1) 
                                                        + (Settings.NodeTypeFilter.Cast<bool>().Count( b => b) == 122 ? 0 : 1)} / 3 ]";
        public bool IsFilterSelectionVisible
        {
            get => Settings.IsFiltersMWVisible;
            set
            {
                Settings.IsFiltersMWVisible = value;
                OnPropertyChanged(nameof(IsFilterSelectionVisible));
                Settings.SaveSettings();
            }
        }
        
        public ObservableCollection<SaveFileMode> SaveFileModes { get; set; }

        public SaveFileMode SelectedSaveFileMode
        {
            get => Settings.SaveMode;
            set
            {
                Settings.SaveMode = value;
                OnPropertyChanged(nameof(SelectedSaveFileMode));
                Settings.SaveSettings();
            }
        }
        
        public ObservableCollection<SaveFileFormat> SaveFileFormats { get; set; }

        public SaveFileFormat SelectedSaveFileFormat
        {
            get => Settings.SaveFileFormat;
            set
            {
                Settings.SaveFileFormat = value;
                OnPropertyChanged(nameof(SelectedSaveFileFormat));
                Settings.SaveSettings();
            }
        }
        
        public ObservableCollection<SaveFileLocation> SaveFileLocations { get; set; }

        public SaveFileLocation SelectedSaveFileLocation
        {
            get => Settings.SaveFileLocation;
            set
            {
                Settings.SaveFileLocation = value;
                OnPropertyChanged(nameof(SelectedSaveFileLocation));
                Settings.SaveSettings();
            }
        }
        
        public ObservableCollection<DestructibleMeshTreatment> DestructibleMeshTreatments { get; set; }

        public DestructibleMeshTreatment SelectedDestructibleMeshTreatment
        {
            get => Settings.DestructibleMeshTreatment;
            set
            {
                Settings.DestructibleMeshTreatment = value;
                OnPropertyChanged(nameof(SelectedDestructibleMeshTreatment));
                Settings.SaveSettings();
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
                OnPropertyChanged(nameof(FilterSectionButtonLabel));
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
                if (_checkedCount == value)
                    return;
                _checkedCount = value;
                OnPropertyChanged(nameof(CheckedCount));
                OnPropertyChanged(nameof(FilterSectionButtonLabel));
            }
        }
        public bool IsParameterSelectionVisible
        {
            get => Settings.IsParametersMWVisible;
            set
            {
                Settings.IsParametersMWVisible = value;
                OnPropertyChanged(nameof(IsParameterSelectionVisible));
                Settings.SaveSettings();
            }
        }
        
        public ObservableCollection<NodeTypeFilterItem> NodeTypeFilterItems
        {
            get => _nodeTypeFilterItems;
            set
            {
                if (_nodeTypeFilterItems == value) return;
                _nodeTypeFilterItems = value;
                OnPropertyChanged(nameof(NodeTypeFilterItems));
                OnPropertyChanged(nameof(FilterSectionButtonLabel));
                CheckedCount = NodeTypeFilterItems.Count(item => item.IsChecked);
            }
        }
        
        public int TotalCount => NodeTypeFilterItems.Count;
        
        public MainWindowViewModel()
        {
            Settings = SettingsService.Instance;
            _resourceNameFilter = Settings.ResourceNameFilter;
            _resourceNameFilter.CollectionChanged += ResourceNameFilter_CollectionChanged;
            Settings.DebugNameFilter.CollectionChanged += DebugNameFilter_CollectionChanged;
            _nodeTypeFilterItems = new();
            var enumValues = Enum.GetValues(typeof(NodeTypeProcessingOptions));
            for (int i = 0; i < enumValues.Length; i++)
            {
                var item = new NodeTypeFilterItem(enumValues.GetValue(i)?.ToString() ?? " ", i, Settings.NodeTypeFilter);
                item.PropertyChanged += OnNodeTypeFilterItemChanged;
                NodeTypeFilterItems.Add(item);
            }
            _filteredNodeTypeFilterItems = _nodeTypeFilterItems;
            CheckedCount = NodeTypeFilterItems.Count(item => item.IsChecked);
            _searchQuery = "";
            
            SaveFileModes = new ObservableCollection<SaveFileMode>(Enum.GetValues(typeof(SaveFileMode)).Cast<SaveFileMode>());
            SaveFileFormats = new ObservableCollection<SaveFileFormat>(Enum.GetValues(typeof(SaveFileFormat)).Cast<SaveFileFormat>());
            SaveFileLocations = new ObservableCollection<SaveFileLocation>(Enum.GetValues(typeof(SaveFileLocation)).Cast<SaveFileLocation>());
            DestructibleMeshTreatments = new ObservableCollection<DestructibleMeshTreatment>(Enum.GetValues(typeof(DestructibleMeshTreatment)).Cast<DestructibleMeshTreatment>());
            
            try
            {
                 VS2077Icon = new Bitmap(Path.Combine(AppContext.BaseDirectory, "assets",
                   "VolumetricSelection2077MSStyle.png"));
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Failed to load VS2077 Icon!");
                VS2077Icon = new WriteableBitmap(new PixelSize(1,1), new Vector(1,1), PixelFormat.Bgra8888, AlphaFormat.Premul);
            }

        }
        
        private void ResourceNameFilter_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ResourcePathFilterCount));
            OnPropertyChanged(nameof(FilterSectionButtonLabel));
        }

        private void DebugNameFilter_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(DebugNameFilterCount));
            OnPropertyChanged(nameof(FilterSectionButtonLabel));
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
        
        public void DebugWindowInstanceChanged()
        {
            OnPropertyChanged(nameof(DebugWindowButtonEnabled));
            OnPropertyChanged(nameof(DebugWindowInstance));
            OnPropertyChanged(nameof(DebugWindowButtonSpinnerEnabled));
        }
        
        private void OnNodeTypeFilterItemChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(NodeTypeFilterItem.IsChecked))
                return;
            CheckedCount = NodeTypeFilterItems.Count(item => item.IsChecked);
            if (sender is not NodeTypeFilterItem item1)
                return;
            var globalIndex = NodeTypeFilterItems.IndexOf(item1);
            Settings.NodeTypeFilter[globalIndex] = item1.IsChecked;
        }
    }
}
