using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DynamicData;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.ViewModels;

namespace VolumetricSelection2077.Views;

public partial class Dialog : Window
{
    private DialogWindowViewModel? _dialogWindowViewModel;
    
    public Dialog(string title, string message, DialogButton[] buttons)
    {
        InitializeComponent();
        Closing += (_, args) => { args.Cancel = !buttonClicked; };

        DataContext = new DialogWindowViewModel(title, message, buttons);
        _dialogWindowViewModel = DataContext as DialogWindowViewModel;
    }

    public int DialogResult { get; set; } = -1;
    private bool buttonClicked = false;
    private void DynamicButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is DialogButton dialogButton)
        {
            DialogResult = _dialogWindowViewModel?.ButtonContents.IndexOf(dialogButton) ?? -1;
            buttonClicked = true;
            Close();
        }
    }
}