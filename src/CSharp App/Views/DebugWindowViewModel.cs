using Avalonia.Controls;
using VolumetricSelection2077.ViewModels;

namespace VolumetricSelection2077.Views;

public class DebugWindowViewModel
{
    public MainWindowViewModel? ParentViewModel { get; }
    public bool IsProcessing { get; set; }
    public DebugWindowViewModel(Window mainWindow)
    {
        ParentViewModel = (MainWindowViewModel?)mainWindow.DataContext;
    }
}