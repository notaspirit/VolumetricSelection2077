using System.Collections.Generic;
using System.ComponentModel;

namespace VolumetricSelection2077.App.ViewStructures;

public class NodeTypeFilterItem : INotifyPropertyChanged
{
    private HashSet<string> _nodeTypeFilter;

    public string Label { get; }

    public bool IsChecked
    {
        get => _nodeTypeFilter.Contains(Label);
        set
        {
            if (IsChecked == value)
                return;

            if (value)
                _nodeTypeFilter.Add(Label);
            else
                _nodeTypeFilter.Remove(Label);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
        }
    }

    public NodeTypeFilterItem(string label, HashSet<string> nodeTypeFilter)
    {
        Label = label;
        _nodeTypeFilter = nodeTypeFilter;
    }

    public void NotifyChange(HashSet<string> newNodeTypeFilter)
    {
        _nodeTypeFilter = newNodeTypeFilter;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
