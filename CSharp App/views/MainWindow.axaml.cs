using Avalonia.Controls;
using VolumetricSelection2077.Views;
using VolumetricSelection2077.Services;
using System;
using System.IO;
using Avalonia.Interactivity;

namespace VolumetricSelection2077;
public partial class MainWindow : Window
{
    private readonly SettingsService _settings;
    private readonly GameFileService _gameFileService;
    public MainWindow()
    {
        InitializeComponent();
        _settings = SettingsService.Instance;
        _gameFileService = new GameFileService();
        DataContext = _settings;
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
        Logger.AddSink(new LogViewerSink(logViewer, "[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message:lj}{NewLine}{Exception}"));
    }

    private void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.ShowDialog(this);
    }

    private void OutputFilename_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == "Text")
        {
            _settings.SaveSettings();
        }
    }

    private void ClearLogButton_Click(object? sender, RoutedEventArgs e)
    {
        var logViewer = this.FindControl<LogViewer>("LogViewer");
        logViewer?.ClearLog();
    }

    private async void FindSelectedButton_Click(object? sender, RoutedEventArgs e)
    {
        // Add await here because ValidateInput is async
        if (!await ValidationService.ValidateInput(_settings.GameDirectory, _settings.OutputFilename))
        {
            return;
        }
        new WolvenkitCLIService().SetSettings();
        Logger.Info("Starting process...");
        // _gameFileService.buildFileMap();
        _gameFileService.GetFiles();
    }
}