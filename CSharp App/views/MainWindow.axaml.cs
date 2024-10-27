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

        var logFileName = Path.Combine(logDirectory, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        string outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        var logViewer = this.FindControl<LogViewer>("LogViewer");
        if (logViewer == null)
        {
            throw new InvalidOperationException("LogViewer control not found");
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logFileName, outputTemplate: outputTemplate)
            .WriteTo.Console(outputTemplate: outputTemplate)
            .WriteTo.Sink(new LogViewerSink(logViewer))
            .CreateLogger();

        Log.Information("Application starting...");
    }

    private void SettingsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.ShowDialog(this);
    }
}