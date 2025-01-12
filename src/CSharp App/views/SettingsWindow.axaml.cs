using Avalonia.Controls;
using VolumetricSelection2077.ViewModels;
using System;
using Avalonia.Input;


namespace VolumetricSelection2077;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
        this.Closed += new EventHandler(OnSettingsWindowClosed);
    }
    private void OnSettingsWindowClosed(object? sender, EventArgs e)
    {
        var viewModel = DataContext as SettingsViewModel;
        viewModel?.Settings.SaveSettings(); // Save settings when window closes
    }

    private void WolvenkitRefreshDelayTextBox_PreviewTextInput(object sender, TextInputEventArgs e)
    {
        string? text = e.Text;
        if (text != null)
        {
            if (!char.IsDigit(text, text.Length - 1))
            {
                e.Handled = true;
            }
        }
    }

    private void WolvenkitTimeoutTextBox_PreviewTextInput(object sender, TextInputEventArgs e)
    {
        string? text = e.Text;
        if (text != null)
        {
            if (!char.IsDigit(text, text.Length - 1))
            {
                e.Handled = true;
            }
        }
    }
}