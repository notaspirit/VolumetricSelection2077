using Avalonia.Controls;
using VolumetricSelection2077.Views;
using VolumetricSelection2077.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia.Styling;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Microsoft.VisualBasic.CompilerServices;
using VolumetricSelection2077.Resources;
using VolumetricSelection2077.TestingStuff;

namespace VolumetricSelection2077;
public partial class MainWindow : Window
{

    private string _resourcePathWatermark = " Resource Path Filters";
    private string _debugNameWatermark = " Debug Name Filters";
    public SettingsService _settings { get;  }
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

    public ObservableCollection<string> ResourceNameFilters { get; set; }
    public ObservableCollection<string> DebugNameFilters { get; set; }
    private int _resourcePathFilterCount;
    private int _debugNameFilterCount;
    public MainWindow()
    {
        InitializeComponent();
        _settings = SettingsService.Instance;
        _processService = new ProcessService();
        DataContext = this;
        _settings = SettingsService.Instance;
        InitializeLogger();
        ResourceNameFilters = new(_settings.ResourceNameFilter);
        DebugNameFilters = new (_settings.DebugNameFilter);
        _resourcePathFilterCount = ResourceNameFilters.Count;
        _debugNameFilterCount = DebugNameFilters.Count;
        DebugNameFilterTextBox.Watermark = _debugNameFilterCount + _debugNameWatermark;
        ResourceFilterTextBox.Watermark = _resourcePathFilterCount + _resourcePathWatermark;
    }

    public int ResourcePathFilterCount
    {
        get => _resourcePathFilterCount;
        set
        {
            if (_resourcePathFilterCount != value)
            {
                _resourcePathFilterCount = value;
                OnPropertyChanged(nameof(ResourcePathFilterCount));
            }
        }
    }
    
    public int DebugNameFilterCount
    {
        get => _debugNameFilterCount;
        set
        {
            if (_debugNameFilterCount != value)
            {
                _debugNameFilterCount = value;
                OnPropertyChanged(nameof(DebugNameFilterCount));
            }
        }
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
            await Task.Run(() => Benchmarking.Instance.RunBenchmarks());
        }
        catch (Exception ex)
        {
            Logger.Error($"Benchmarking failed: {ex}");
        }
        IsProcessing = false;
    }
    
    private void ResourceFilterTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox textBox)
        {
            string text = textBox.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                ResourceNameFilters.Add(text.ToLower());
                textBox.Text = string.Empty;
                _settings.ResourceNameFilter.Add(text.ToLower());
                _settings.SaveSettings();
                ResourcePathFilterCount = _settings.ResourceNameFilter.Count;
                ResourceFilterTextBox.Watermark = _resourcePathFilterCount + _resourcePathWatermark;
                
            }
        }
    }
    
    private void RemoveResourceNameFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is string item)
        {
            ResourceNameFilters.Remove(item.ToLower());
            _settings.ResourceNameFilter.Remove(item.ToLower());
            _settings.SaveSettings();
            ResourcePathFilterCount = _settings.ResourceNameFilter.Count;
            ResourceFilterTextBox.Watermark = _resourcePathFilterCount + _resourcePathWatermark;
        }
    }
    private void ResourceFilterTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            FlyoutBase.ShowAttachedFlyout(textBox);
        }
    }
    
    private void DebugNameFilterTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox textBox)
        {
            string text = textBox.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                DebugNameFilters.Add(text.ToLower());
                textBox.Text = string.Empty;
                _settings.DebugNameFilter.Add(text.ToLower());
                _settings.SaveSettings();
                DebugNameFilterCount = _settings.DebugNameFilter.Count;
                DebugNameFilterTextBox.Watermark = _debugNameFilterCount + _debugNameWatermark;
            }
        }
    }
    
    private void RemoveDebugNameFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is string item)
        {
            DebugNameFilters.Remove(item.ToLower());
            _settings.DebugNameFilter.Remove(item.ToLower());
            _settings.SaveSettings();
            DebugNameFilterCount = _settings.DebugNameFilter.Count;
            DebugNameFilterTextBox.Watermark = _debugNameFilterCount + _debugNameWatermark;
        }
    }
    private void DebugNameFilterTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            FlyoutBase.ShowAttachedFlyout(textBox);
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
        
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}