using Avalonia.Controls;
using VolumetricSelection2077.ViewModels;

namespace VolumetricSelection2077.views;

public class DebugWindowViewModel
{
    public MainWindowViewModel? ParentViewModel { get; }
    
    public DebugWindowViewModel(Window mainWindow)
    {
        ParentViewModel = (MainWindowViewModel?)mainWindow.DataContext;
    }
}