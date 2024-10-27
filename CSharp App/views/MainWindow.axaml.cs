using Avalonia.Controls;
using VolumetricSelection2077.Views;
using VolumetricSelection2077.Services;
using Serilog;
using System;
using System.IO;

namespace VolumetricSelection2077;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InitializeLogger();
    }

    private void InitializeLogger()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDirectory = Path.Combine(appDataPath, "VolumetricSelection2077", "Logs");
        Directory.CreateDirectory(logDirectory);

        var logViewer = this.FindControl<LogViewer>("LogViewer");
        if (logViewer == null)
        {
            throw new InvalidOperationException("LogViewer control not found");
        }

        Logger.Initialize(logDirectory);
        Logger.AddSink(new LogViewerSink(logViewer, "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Message:lj}{NewLine}{Exception}"));
        
        Logger.Info("Application starting...");
    }

    private void SettingsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.ShowDialog(this);
    }
    private void ClearLogButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var logViewer = this.FindControl<LogViewer>("LogViewer");
        logViewer?.ClearLog();
    }
}