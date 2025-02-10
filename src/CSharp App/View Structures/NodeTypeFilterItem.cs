using System.Collections;
using System.ComponentModel;

namespace VolumetricSelection2077.ViewStructures;

public class NodeTypeFilterItem : INotifyPropertyChanged
{
    private BitArray _bitArray;
    private readonly int _index;

    public string Label { get; }

    public bool IsChecked
    {
        get => _bitArray[_index];
        set
        {
            if (_bitArray[_index] != value)
            {
                _bitArray[_index] = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            }
        }
    }

    public NodeTypeFilterItem(string label, int index, BitArray bitArray)
    {
        Label = label;
        _index = index;
        _bitArray = bitArray;
    }
    
    public void NotifyChange(BitArray newBitArray)
    {
        _bitArray = newBitArray;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

