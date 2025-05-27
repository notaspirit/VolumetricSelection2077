using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using VolumetricSelection2077.Extensions;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.TestingStuff;
using VolumetricSelection2077.ViewModels;
using Path = System.IO.Path;

namespace VolumetricSelection2077.views;

public partial class DebugWindow : Window
{
    private readonly MainWindowViewModel? _mainWindowViewModel;
    private readonly DebugWindowViewModel? _debugWindowViewModel;
    private readonly DialogService _dialogService;
    private TrackedDispatchTimer _dispatcherTimer;
    private MainWindow? mainWindow;
    public DebugWindow(Window parent)
    {
        InitializeComponent();
        DataContext = new DebugWindowViewModel(parent);
        _debugWindowViewModel = DataContext as DebugWindowViewModel;
        _mainWindowViewModel = _debugWindowViewModel?.ParentViewModel;
        _dialogService = new DialogService(this);
        mainWindow = parent as MainWindow;
        
        _dispatcherTimer = new TrackedDispatchTimer() { Interval = TimeSpan.FromSeconds(1) };
        _dispatcherTimer.Tick += (s, e) => mainWindow.ProgressTextBlock.Text = $"{UtilService.FormatElapsedTimeMMSS(_dispatcherTimer.Elapsed)}";

        Opened += OnOpened;
        Closing += OnClosing;
    }
    
    private async void Benchmark_Click(object? sender, RoutedEventArgs e)
    {
        if (_mainWindowViewModel.IsProcessing) return;
        _debugWindowViewModel.IsProcessing = true;
        try
        {
            _mainWindowViewModel.BenchmarkProcessing = true;
            _dispatcherTimer.Start();
            _mainWindowViewModel.Settings.OutputFilename =
                UtilService.SanitizeFilePath(_mainWindowViewModel.Settings.OutputFilename);
            mainWindow.OutputFilenameTextBox.Text = _mainWindowViewModel.Settings.OutputFilename;
            mainWindow.AddQueuedFilters();
            await Task.Run(() => Benchmarking.Instance.RunBenchmarks(_dialogService));
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
            _debugWindowViewModel.IsProcessing = false;
        }
    }
    
    private void OnOpened(object? sender, EventArgs e)
    {
        double x = mainWindow.Position.X + (mainWindow.Bounds.Width - Bounds.Width) / 2;
        double y = mainWindow.Position.Y + (mainWindow.Bounds.Height - Bounds.Height) / 2;
        Position = new PixelPoint((int)x, (int)y);
    }
    
    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = _debugWindowViewModel?.IsProcessing ?? false;
    }

    private async void DialogTest_Click(object? sender, RoutedEventArgs e)
    {
        _debugWindowViewModel.IsProcessing = true;
        await TestDialogService.Run(this);
        _debugWindowViewModel.IsProcessing = false;
    }
    
    private void DumpSectorBounds_Click(object? sender, RoutedEventArgs e)
    {
        _debugWindowViewModel.IsProcessing = true;
        CacheService.Instance.DumpSectorBBToFile();
        _debugWindowViewModel.IsProcessing = false;
    }

    private void LoadSectorBounds_Click(object? sender, RoutedEventArgs e)
    {
        _debugWindowViewModel.IsProcessing = true;
        var cacheMetadata = CacheService.Instance.GetMetadata();
        CacheService.Instance.LoadSectorBBFromFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "debug",
            $"{cacheMetadata.GameVersion}-{cacheMetadata.VS2077Version}.bin"));
        _debugWindowViewModel.IsProcessing = false;
    }
    
    private async void ListNodeTypes_Click(object? sender, RoutedEventArgs e)
    {
        _debugWindowViewModel.IsProcessing = true;
        try
        {
            _dispatcherTimer.Start();
            await GetSectorStats.Run();
        }
        finally
        {
            _dispatcherTimer.Stop();
            string formattedTime = UtilService.FormatElapsedTime(_dispatcherTimer.Elapsed);
            Logger.Info($"ListNodeTypes finished after: {formattedTime}");
        }

        _debugWindowViewModel.IsProcessing = false;
    }
}