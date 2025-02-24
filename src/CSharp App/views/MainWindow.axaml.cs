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
using VolumetricSelection2077.ViewModels;
using WolvenKit.Interfaces.Extensions;

namespace VolumetricSelection2077;
public partial class MainWindow : Window
{
    private SettingsService _settings;
    
    private readonly ProcessService _processService;
    private MainWindowViewModel _mainWindowViewModel;
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        _mainWindowViewModel = DataContext as MainWindowViewModel;
        InitializeLogger();
        _settings = SettingsService.Instance;
        _processService = new ProcessService();
        
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

    private void OutputFilename_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
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
        if (_mainWindowViewModel.IsProcessing) return;
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            _mainWindowViewModel.IsProcessing = true;
            _settings.OutputFilename = UtilService.SanitizeFilePath(_settings.OutputFilename);
            OutputFilenameTextBox.Text = _settings.OutputFilename;
            _settings.SaveSettings();
            if (!string.IsNullOrEmpty(_settings.OutputFilename))
            {
                var (success, error) = await Task.Run(() =>
                { 
                    return _processService.MainProcessTask();
                });
                if (!success)
                {
                    Logger.Error($"Process failed: {error}");
                }
            }
            else
            {
                Logger.Error("Output filename is empty");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Critical error: {ex}");
        }
        finally
        {
            stopwatch.Stop();
            string formattedTime = UtilService.FormatElapsedTime(stopwatch.Elapsed);
            Logger.Info($"Process finished after: {formattedTime}");
            _mainWindowViewModel.IsProcessing = false;
        }
    }

    private async void Benchmark_Click(object? sender, RoutedEventArgs e)
    {
        if (_mainWindowViewModel.IsProcessing) return;
        try
        {
            _mainWindowViewModel.IsProcessing = true;
            _settings.OutputFilename = UtilService.SanitizeFilePath(_settings.OutputFilename);
            OutputFilenameTextBox.Text = _settings.OutputFilename;
            _settings.SaveSettings();
            await Task.Run(() => Benchmarking.Instance.RunBenchmarks());
        }
        catch (Exception ex)
        {
            Logger.Error($"Benchmarking failed: {ex}");
        }
        _mainWindowViewModel.IsProcessing = false;
    }
    
    private void ResourceFilterTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox textBox)
        {
            string text = textBox.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                textBox.Text = string.Empty;
                _settings.ResourceNameFilter.Add(text.ToLower());
                _settings.SaveSettings();
            }
        }
    }
    
    private void RemoveResourceNameFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is string item)
        {
            _settings.ResourceNameFilter.Remove(item.ToLower());
            _settings.SaveSettings();
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
                textBox.Text = string.Empty;
                _settings.DebugNameFilter.Add(text.ToLower());
                _settings.SaveSettings();
            }
        }
    }
    
    private void RemoveDebugNameFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is string item)
        {
            _settings.DebugNameFilter.Remove(item.ToLower());
            _settings.SaveSettings();
        }
    }
    private void DebugNameFilterTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            FlyoutBase.ShowAttachedFlyout(textBox);
        }
    }

    private void SwitchFilterModeButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button)
        {
            _settings.FilterModeOr = !_settings.FilterModeOr;
            _settings.SaveSettings();
            _mainWindowViewModel.FilterModeOr = _settings.FilterModeOr;
        }
    }
}