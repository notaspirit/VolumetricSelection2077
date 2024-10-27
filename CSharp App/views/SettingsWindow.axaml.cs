using Avalonia.Controls;

namespace VolumetricSelection2077;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = new Descriptions();
    }
}