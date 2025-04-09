using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace VolumetricSelection2077.views;

public partial class Dialog : Window
{
    public Dialog()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closing += (_, args) => { args.Cancel = !buttonClicked; };
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
    
    public string TitleText
    {
        set => SetValue(TitleProperty, value);
    }
    
    public string Message
    {
        set => MessageTextBlock.Text = value;
    }

    public string ButtonLeftText
    {
        set => ButtonLeft.Content = value;
    }
    
    public string ButtonRightText
    {
        set => ButtonRight.Content = value;
    }

    public bool DialogResult { get; set; }
    private bool buttonClicked = false;
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        buttonClicked = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        buttonClicked = true;
        Close();
    }
}