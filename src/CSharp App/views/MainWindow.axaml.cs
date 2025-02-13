using Avalonia.Controls;
using VolumetricSelection2077.Views;
using VolumetricSelection2077.Services;
using System;
using System.IO;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia.Styling;
using Avalonia;
using Microsoft.VisualBasic.CompilerServices;
using VolumetricSelection2077.TestingStuff;

namespace VolumetricSelection2077;
public partial class MainWindow : Window
{
    private readonly SettingsService _settings;
    private readonly ProcessService _processService;
        private bool _isProcessing;

    // Define the AvaloniaProperty for IsProcessing
    public static readonly StyledProperty<bool> IsProcessingProperty =
        AvaloniaProperty.Register<MainWindow, bool>(nameof(IsProcessing));

    // Property wrapper
    public bool IsProcessing
    {
        get => GetValue(IsProcessingProperty);
        set => SetValue(IsProcessingProperty, value);
    }

    public MainWindow()
    {
        InitializeComponent();
        _settings = SettingsService.Instance;
        _processService = new ProcessService();
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
        if (IsProcessing) return;
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            IsProcessing = true;
            var (success, error) = await Task.Run(() =>
            { 
                return _processService.MainProcessTask();
            });
            if (!success)
            {
                Logger.Error($"Process failed: {error}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Critical error: {ex}");
            IsProcessing = false;
        }
        finally
        {
            stopwatch.Stop();
            string formattedTime = UtilService.FormatElapsedTime(stopwatch.Elapsed);
            Logger.Info($"Process finished after: {formattedTime}");
            IsProcessing = false;
        }
    }

    private async void Benchmark_Click(object? sender, RoutedEventArgs e)
    {
        if (IsProcessing) return;
        try
        {
            IsProcessing = true;
            await Task.Run(() => Benchmarking.RunBenchmarks());
        }
        catch (Exception ex)
        {
            Logger.Error($"Benchmarking failed: {ex}");
        }
        IsProcessing = false;
    }
}