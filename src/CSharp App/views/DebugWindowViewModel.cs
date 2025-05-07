using Avalonia.Controls;
using VolumetricSelection2077.ViewModels;

namespace VolumetricSelection2077.views;

public class DebugWindowViewModel
{
    public MainWindowViewModel? ParentViewModel { get; }
    public bool IsProcessing { get; set; }
    public DebugWindowViewModel(Window mainWindow)
    {
        ParentViewModel = (MainWindowViewModel?)mainWindow.DataContext;
    }
}