using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using VolumetricSelection2077.Extensions;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.TestingStuff;
using VolumetricSelection2077.ViewModels;

namespace VolumetricSelection2077.views;

public partial class DebugWindow : Window
{
    private readonly MainWindowViewModel? _mainWindowViewModel;
    private TrackedDispatchTimer _dispatcherTimer;
    private MainWindow? mainWindow;
    public DebugWindow(Window parent)
    {
        InitializeComponent();
        DataContext = new DebugWindowViewModel(parent);
        var debugWindowViewModel = DataContext as DebugWindowViewModel;
        _mainWindowViewModel = debugWindowViewModel?.ParentViewModel;
        
        mainWindow = parent as MainWindow;
        
        _dispatcherTimer = new TrackedDispatchTimer() { Interval = TimeSpan.FromSeconds(1) };
        _dispatcherTimer.Tick += (s, e) => mainWindow.ProgressTextBlock.Text = $"{UtilService.FormatElapsedTimeMMSS(_dispatcherTimer.Elapsed)}";

        Opened += OnOpened;
    }
    
    private async void Benchmark_Click(object? sender, RoutedEventArgs e)
    {
        if (_mainWindowViewModel.IsProcessing) return;
        try
        {
            _mainWindowViewModel.BenchmarkProcessing = true;
            _dispatcherTimer.Start();
            _mainWindowViewModel.Settings.OutputFilename =
                UtilService.SanitizeFilePath(_mainWindowViewModel.Settings.OutputFilename);
            mainWindow.OutputFilenameTextBox.Text = _mainWindowViewModel.Settings.OutputFilename;
            mainWindow.AddQueuedFilters();
            await Task.Run(() => Benchmarking.Instance.RunBenchmarks());
        }
        catch (Exception ex)
        {
            Logger.Error($"Benchmarking failed: {ex}");
        }
        finally
        {
            _dispatcherTimer.Stop();
            string formattedTime = UtilService.FormatElapsedTime(_dispatcherTimer.Elapsed);
            Logger.Info($"Benchmarking finished after: {formattedTime}");
            _mainWindowViewModel.BenchmarkProcessing = false;
        }
    }
    
    private void OnOpened(object? sender, EventArgs e)
    {
        double x = mainWindow.Position.X + (mainWindow.Bounds.Width - Bounds.Width) / 2;
        double y = mainWindow.Position.Y + (mainWindow.Bounds.Height - Bounds.Height) / 2;
        
        Position = new PixelPoint((int)x, (int)y);
    }
}