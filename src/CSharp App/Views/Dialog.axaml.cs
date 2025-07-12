using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DynamicData;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.ViewModels;

namespace VolumetricSelection2077.Views;

public partial class Dialog : Window
{
    private DialogWindowViewModel? _dialogWindowViewModel;
    
    public Dialog(string title, string message, string[] buttons)
    {
        InitializeComponent();
        Opened += OnOpened;
        Closing += (_, args) => { args.Cancel = !buttonClicked; };

        DataContext = new DialogWindowViewModel(title, message, buttons);
        _dialogWindowViewModel = DataContext as DialogWindowViewModel;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (Owner is Window parentWindow)
        {
            double x = parentWindow.Position.X + (parentWindow.Bounds.Width - Bounds.Width) / 2;
            double y = parentWindow.Position.Y + (parentWindow.Bounds.Height - Bounds.Height) / 2;
            
            Position = new PixelPoint((int)x, (int)y);
        }
    }

    public int DialogResult { get; set; } = -1;
    private bool buttonClicked = false;
    private void DynamicButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is string buttonText)
        {
            DialogResult = _dialogWindowViewModel?.ButtonContents.IndexOf(buttonText) ?? -1;
            buttonClicked = true;
            Close();
        }
    }
}