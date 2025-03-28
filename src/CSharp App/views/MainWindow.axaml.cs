using Avalonia.Controls;
using VolumetricSelection2077.Views;
using VolumetricSelection2077.Services;
using System;
using System.ComponentModel;
using System.IO;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using VolumetricSelection2077.TestingStuff;
using VolumetricSelection2077.ViewModels;
using VolumetricSelection2077.ViewStructures;

namespace VolumetricSelection2077;
public partial class MainWindow : Window
{
    private readonly ProcessService _processService;
    private MainWindowViewModel _mainWindowViewModel;
    
    public MainWindow()
    {
        InitializeComponent();
        InitializeLogger();
        DataContext = new MainWindowViewModel();
        _mainWindowViewModel = DataContext as MainWindowViewModel;
        _processService = new ProcessService();
        Closed += OnMainWindowClosed;
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

    private async void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_mainWindowViewModel.IsProcessing) return;
        _mainWindowViewModel.SettingsOpen = true;
        var settingsWindow = new SettingsWindow();
        settingsWindow.Opened += (_, _) =>
        {
            var x = this.Position.X + 10;
            var y = this.Position.Y + 41;
            settingsWindow.Position = new PixelPoint(x, y);
        };
        await settingsWindow.ShowDialog(this);
        _mainWindowViewModel.SettingsOpen = false;
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
            _mainWindowViewModel.MainTaskProcessing = true;
            _mainWindowViewModel.Settings.OutputFilename = UtilService.SanitizeFilePath(_mainWindowViewModel.Settings.OutputFilename);
            OutputFilenameTextBox.Text = _mainWindowViewModel.Settings.OutputFilename;
            AddQueuedFilters();
            if (!string.IsNullOrEmpty(_mainWindowViewModel.Settings.OutputFilename))
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
            _mainWindowViewModel.MainTaskProcessing = false;
        }
    }

    private async void Benchmark_Click(object? sender, RoutedEventArgs e)
    {
        if (_mainWindowViewModel.IsProcessing) return;
        try
        {
            _mainWindowViewModel.BenchmarkProcessing = true;
            _mainWindowViewModel.Settings.OutputFilename = UtilService.SanitizeFilePath(_mainWindowViewModel.Settings.OutputFilename);
            OutputFilenameTextBox.Text = _mainWindowViewModel.Settings.OutputFilename;
            AddQueuedFilters();
            await Task.Run(() => Benchmarking.Instance.RunBenchmarks());
        }
        catch (Exception ex)
        {
            Logger.Error($"Benchmarking failed: {ex}");
        }
        _mainWindowViewModel.BenchmarkProcessing = false;
    }
    
    private void ResourceFilterTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox textBox)
        {
            string text = textBox.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                textBox.Text = string.Empty;
                _mainWindowViewModel.Settings.ResourceNameFilter.Add(text.ToLower());
                _mainWindowViewModel.Settings.SaveSettings();
            }
        }
    }
    
    private void RemoveResourceNameFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is string item)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _mainWindowViewModel.Settings.ResourceNameFilter.Remove(item.ToLower());
                _mainWindowViewModel.Settings.SaveSettings();
            });
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
                _mainWindowViewModel.Settings.DebugNameFilter.Add(text.ToLower());
                _mainWindowViewModel.Settings.SaveSettings();
            }
        }
    }
    
    private void RemoveDebugNameFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is string item)
        {
            _mainWindowViewModel.Settings.DebugNameFilter.Remove(item.ToLower());
            _mainWindowViewModel.Settings.SaveSettings();
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
            _mainWindowViewModel.Settings.FilterModeOr = !_mainWindowViewModel.Settings.FilterModeOr;
            _mainWindowViewModel.Settings.SaveSettings();
            _mainWindowViewModel.FilterModeOr = _mainWindowViewModel.Settings.FilterModeOr;
        }
    }

    private void ToggleFilterVisibility_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button)
        {
            _mainWindowViewModel.FilterSelectionVisibility = !_mainWindowViewModel.FilterSelectionVisibility;
            AddQueuedFilters();
        }
    }
    
    public void ToggleParameterVisibility_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button)
        {
            _mainWindowViewModel.ParameterSelectionVisibility = !_mainWindowViewModel.ParameterSelectionVisibility;
        }
    }
    
    private void SelectAllClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        for (int i = 0; i < _mainWindowViewModel.FilteredNodeTypeFilterItems.Count; i++)
        {
            var item = _mainWindowViewModel.FilteredNodeTypeFilterItems[i];
            var globalIndex = _mainWindowViewModel.NodeTypeFilterItems.IndexOf(item);
            _mainWindowViewModel.Settings.NodeTypeFilter[globalIndex] = true;
            item.IsChecked = true;
        }
        RefreshItems();
    }

    private void DeselectAllClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        for (int i = 0; i < _mainWindowViewModel.FilteredNodeTypeFilterItems.Count; i++)
        {
            var item = _mainWindowViewModel.FilteredNodeTypeFilterItems[i];
            var globalIndex = _mainWindowViewModel.NodeTypeFilterItems.IndexOf(item);
            _mainWindowViewModel.Settings.NodeTypeFilter[globalIndex] = false;
            item.IsChecked = false;
        }
        RefreshItems();
    }

    private void RefreshItems()
    {
        foreach (var item in _mainWindowViewModel.FilteredNodeTypeFilterItems)
        {
            item.NotifyChange(_mainWindowViewModel.Settings.NodeTypeFilter);
        }
        
        foreach (var item in _mainWindowViewModel.NodeTypeFilterItems)
        {
            item.NotifyChange(_mainWindowViewModel.Settings.NodeTypeFilter);
        }
    }
    
    private void Label_Click(object sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        var label = sender as Label;
        var item = label?.DataContext as NodeTypeFilterItem;

        if (item != null)
        {
            item.IsChecked = !item.IsChecked;
        }
    }

    private void AddQueuedFilters()
    {
        if (!string.IsNullOrEmpty(ResourceFilterTextBox.Text?.Trim()))
        {
            _mainWindowViewModel.Settings.ResourceNameFilter.Add(ResourceFilterTextBox.Text.ToLower());
            ResourceFilterTextBox.Text = string.Empty;
        }
        if (!string.IsNullOrEmpty(DebugNameFilterTextBox.Text?.Trim()))
        {
            _mainWindowViewModel.Settings.DebugNameFilter.Add(DebugNameFilterTextBox.Text.ToLower());
            DebugNameFilterTextBox.Text = string.Empty;
        }
        _mainWindowViewModel.Settings.SaveSettings();
    }
    
    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        Logger.Info($"VS2077 Version: {_mainWindowViewModel.Settings.ProgramVersion}");
        _mainWindowViewModel.IsProcessing = true;
        var validationResult = ValidationService.ValidateGamePath(_mainWindowViewModel.Settings.GameDirectory);
        if (!(validationResult == ValidationService.GamePathResult.Valid ||
              validationResult == ValidationService.GamePathResult.CetNotFound))
        {
            Logger.Error("Failed to initialize VS2077! Invalid Game Path, update it in the settings and restart the application.");
            _mainWindowViewModel.AppInitialized = false;
            _mainWindowViewModel.IsProcessing = false;
            return;
        }
        try
        {
            if (_mainWindowViewModel.Settings.DidUpdate)
            {
                var changelog = await UpdateService.GetChangelog();
                Logger.Success($"Successfully updated to {changelog.Item1}");
                Logger.Info($"Changelog:" +
                            $"\n{changelog.Item2}");
                _mainWindowViewModel.Settings.DidUpdate = false;
                _mainWindowViewModel.Settings.SaveSettings();
            }
            else
            {
                Logger.Info("Checking for Updates...");
                var updateExists = await UpdateService.CheckUpdates();
                if (updateExists.Item1)
                {
                    Logger.Warning($"Update to {updateExists.Item2} is available");
                }
                else
                {
                    Logger.Info("No updates found");
                }

                if (updateExists.Item1 && _mainWindowViewModel.Settings.AutoUpdate)
                {
                    await UpdateService.Update();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"An error occured during the update check: {ex}");
        }
        var success = await Task.Run(() =>
        {
            return GameFileService.Instance.Initialize();
        });
        if (success)
            _mainWindowViewModel.AppInitialized = true;
        _mainWindowViewModel.IsProcessing = false;
    }

    private void OnMainWindowClosed(object? sender, EventArgs e)
    {
        AddQueuedFilters();
        _mainWindowViewModel.Settings.SaveSettings();
    }
}