using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DynamicData;
using VolumetricSelection2077.App.Models;
using VolumetricSelection2077.App.Services;
using VolumetricSelection2077.App.ViewModels;

namespace VolumetricSelection2077.App.Views;

public partial class Dialog : Window
{
    private DialogWindowViewModel? _dialogWindowViewModel;
    private bool _wasButtonClicked = false;
    public int DialogResult { get; set; } = -1;
    public Dialog(string title, string message, DialogButton[] buttons)
    {
        InitializeComponent();
        Closing += (_, args) => { args.Cancel = !_wasButtonClicked; };

        DataContext = new DialogWindowViewModel(title, message, buttons);
        _dialogWindowViewModel = DataContext as DialogWindowViewModel;
    }
    
    private void DynamicButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is DialogButton dialogButton)
        {
            DialogResult = _dialogWindowViewModel?.ButtonContents.IndexOf(dialogButton) ?? -1;
            _wasButtonClicked = true;
            Close();
        }
    }
}