using Avalonia.Controls;
using VolumetricSelection2077.Services;
using System;
using System.IO;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Extensions;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.ViewModels;
using VolumetricSelection2077.ViewStructures;

namespace VolumetricSelection2077.Views;
public partial class MainWindow : Window
{
    private readonly ProcessDispatcher _processService;
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly ProgressBar _progressBar;
    private readonly TrackedDispatchTimer _dispatcherTimer;
    private readonly Progress _progress;
    
    public TextBlock ProgressTextBlock { get; }
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        _mainWindowViewModel = (DataContext as MainWindowViewModel)!;
        try
        {
            InitializeLogger();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        _processService = new ProcessDispatcher(new DialogService(this));
        Closed += OnMainWindowClosed;
        
        var progressBar = this.FindControl<ProgressBar>("ProgressBar");
        var progressBarBroder = this.FindControl<ProgressBar>("ProgressBarBorder");
        var progressTextBlock = this.FindControl<TextBlock>("TimerTextBlock");
        if (progressTextBlock == null || progressBar == null || progressBarBroder == null)
        {
            var error =
                $"Could not find one or more ui components: ProgressBar: {_progressBar}, TimerTextBlock: {ProgressTextBlock}, ProgressBarBorder: {progressBarBroder}";
            Logger.Error(error);
            throw new InvalidOperationException(error);
        }
        ProgressTextBlock = progressTextBlock;
        _progressBar = progressBar;
        
        _progressBar.SizeChanged += (_, _) =>
        {
            progressBarBroder.Width = ((_progressBar.Width / DesktopScaling) + 2) * DesktopScaling;
            progressBarBroder.Height = ((_progressBar.Height / DesktopScaling) + 2) * DesktopScaling;
        };
        
        _dispatcherTimer = new TrackedDispatchTimer() { Interval = TimeSpan.FromSeconds(1) };
        _dispatcherTimer.Tick += (_, _) => ProgressTextBlock.Text = $"{UtilService.FormatElapsedTimeMMSS(_dispatcherTimer.Elapsed)}";
        _progress = Progress.Instance;
        _progress.ProgressChanged += (_, i) =>
        {
            _progressBar.Value = i;
            progressBarBroder.Value = i;
        };
    }
    
    /// <summary>
    /// Initializes the Logger Service and UI Sink
    /// </summary>
    /// <exception cref="InvalidOperationException">Could not find the log viewer in the UI</exception>
    /// <exception cref="ArgumentException">Log Directory its build is invalid</exception>
    /// <exception cref="IOException"></exception>
    private void InitializeLogger()
    {
        Directory.CreateDirectory(_mainWindowViewModel.Settings.LogDirectory);

        var logViewer = this.FindControl<LogViewer>("LogViewer");
        if (logViewer == null)
        {
            throw new InvalidOperationException("LogViewer control not found");
        }

        Logger.Initialize(_mainWindowViewModel.Settings.LogDirectory);
        Logger.AddSink(new LogViewerSink(logViewer, "[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message:lj}{NewLine}{Exception}"));
    }
    
    private async void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_mainWindowViewModel.IsProcessing) return;
        _mainWindowViewModel.SettingsOpen = true;
        var settingsWindow = new SettingsWindow();
        await settingsWindow.ShowDialog(this);
        _mainWindowViewModel.SettingsOpen = false;
    }

    private async void FindSelectedButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_mainWindowViewModel.IsProcessing) return;
        _dispatcherTimer.Start();
        try
        {
            _mainWindowViewModel.MainTaskProcessing = true;
            AddQueuedFilters();
            var (success, error) = await Task.Run(() => _processService.StartProcess());
            if (!success)
            {
                Logger.Error($"Process failed: {error}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Critical error: {ex}");
        }
        finally
        {
            _dispatcherTimer.Stop();
            var formattedTime = UtilService.FormatElapsedTime(_dispatcherTimer.Elapsed);
            Logger.Info($"Process finished after: {formattedTime}");
            _mainWindowViewModel.MainTaskProcessing = false;
        }
    }
    
    private void ResourceFilterTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox textBox)
            return;
        var text = textBox.Text?.Trim();
        if (string.IsNullOrEmpty(text))
            return;
        textBox.Text = string.Empty;
        _mainWindowViewModel.Settings.ResourceNameFilter.Add(text.ToLower());
        _mainWindowViewModel.Settings.SaveSettings();
    }
    
    private void RemoveResourceNameFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: string item })
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
        if (e.Key != Key.Enter || sender is not TextBox textBox)
            return;
        var text = textBox.Text?.Trim();
        if (string.IsNullOrEmpty(text))
            return;
        textBox.Text = string.Empty;
        _mainWindowViewModel.Settings.DebugNameFilter.Add(text.ToLower());
        _mainWindowViewModel.Settings.SaveSettings();
    }
    
    private void RemoveDebugNameFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: string item })
            return;
        _mainWindowViewModel.Settings.DebugNameFilter.Remove(item.ToLower());
        _mainWindowViewModel.Settings.SaveSettings();
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
        if (sender is not Button)
            return;
        _mainWindowViewModel.Settings.FilterModeOr = !_mainWindowViewModel.Settings.FilterModeOr;
        _mainWindowViewModel.Settings.SaveSettings();
        _mainWindowViewModel.FilterModeOr = _mainWindowViewModel.Settings.FilterModeOr;
    }

    private void ToggleFilterVisibility_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button)
            return;
        _mainWindowViewModel.FilterSelectionVisibility = !_mainWindowViewModel.FilterSelectionVisibility;
        AddQueuedFilters();
    }
    
    public void ToggleParameterVisibility_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button)
        {
            _mainWindowViewModel.ParameterSelectionVisibility = !_mainWindowViewModel.ParameterSelectionVisibility;
        }
    }
    
    private void SelectAllClick(object? sender, RoutedEventArgs e)
    {
        foreach (var item in _mainWindowViewModel.FilteredNodeTypeFilterItems)
        {
            var globalIndex = _mainWindowViewModel.NodeTypeFilterItems.IndexOf(item);
            _mainWindowViewModel.Settings.NodeTypeFilter[globalIndex] = true;
            item.IsChecked = true;
        }

        RefreshItems();
    }

    private void DeselectAllClick(object? sender, RoutedEventArgs e)
    {
        foreach (var item in _mainWindowViewModel.FilteredNodeTypeFilterItems)
        {
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
    
    private void Label_Click(object sender, PointerPressedEventArgs e)
    {
        var label = sender as Label;

        if (label?.DataContext is NodeTypeFilterItem item)
        {
            item.IsChecked = !item.IsChecked;
        }
    }
    
    /// <summary>
    /// Shows the debug window while making sure only one exists at a time
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DebugWindowButton_Click(object sender, RoutedEventArgs e)
    {
        if (_mainWindowViewModel.DebugWindowInstance != null) return;
        _mainWindowViewModel.DebugWindowInstance = new DebugWindow(this);
        _mainWindowViewModel.DebugWindowInstance.Closed += (_, _) =>
        {
            _mainWindowViewModel.DebugWindowInstance = null;
            _mainWindowViewModel.DebugWindowInstanceChanged();
        };
        _mainWindowViewModel.DebugWindowInstance.Show();
        _mainWindowViewModel.DebugWindowInstanceChanged();
    }
    
    /// <summary>
    /// Adds Filters that are currently in the text box but not yet committed
    /// </summary>
    public void AddQueuedFilters()
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
    
     /// <summary>
    /// Updates saved window position
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MainWindow_PositionChanged(object? sender, PixelPointEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var newPos = e.Point;
            if (WindowState != WindowState.Normal)
                return;
            _mainWindowViewModel.Settings.WindowRecoveryState.PosX = newPos.X;
            _mainWindowViewModel.Settings.WindowRecoveryState.PosY = newPos.Y;
        });
    }

    private bool _wasMaximized;
    /// <summary>
    /// Updates saved window size and sets the correct size after returning from maximized state
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MainWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var newSize = e.NewSize;
            if (WindowState == WindowState.Maximized)
                _wasMaximized = true;
            if (WindowState == WindowState.Normal)
            {
                if (_wasMaximized)
                {
                    try
                    {
                        Width = _mainWindowViewModel.Settings.WindowRecoveryState.PosWidth / DesktopScaling;
                        Height = _mainWindowViewModel.Settings.WindowRecoveryState.PosHeight / DesktopScaling;
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(ex, "Failed to set window size after returning from maximized state");
                    }
                    _wasMaximized = false;
                }
                else
                {
                    _mainWindowViewModel.Settings.WindowRecoveryState.PosWidth = (int)(newSize.Width * DesktopScaling);
                    _mainWindowViewModel.Settings.WindowRecoveryState.PosHeight = (int)(newSize.Height * DesktopScaling);
                }

            }
        });
    }
    /// <summary>
    /// Sets the position, size and state of the window safely
    /// </summary>
    /// <param name="wrs">Target state</param>
    /// <returns>true if successful</returns>
    private bool SetWindowState(WindowRecoveryState wrs)
    {
        try
        {
            Position = new PixelPoint(wrs.PosX,
                wrs.PosY);
            Width = wrs.PosWidth / DesktopScaling;
            Height = wrs.PosHeight / DesktopScaling;
            WindowState = (WindowState)wrs.WindowState;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "Failed to set window position, size or state, using default values.");
            return false;
        }
    }

    private string? TryGetGamePathFromWolvenKit()
    {
        var wkitConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "REDModding", "WolvenKit", "config.json");
        if (!File.Exists(wkitConfigFile))
            return null;
        var config = File.ReadAllText(wkitConfigFile);
        var json = Newtonsoft.Json.Linq.JObject.Parse(config);
        var gamepath = json["CP77ExecutablePath"]?.ToString();
        if (string.IsNullOrEmpty(gamepath))
            return null;
        gamepath = gamepath.Replace(@"\bin\x64\Cyberpunk2077.exe", "");
        return gamepath;
    }
    
    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        PositionChanged += MainWindow_PositionChanged;
        SizeChanged += MainWindow_SizeChanged;

        if (!SetWindowState(_mainWindowViewModel.Settings.WindowRecoveryState))
        {
            _mainWindowViewModel.Settings.WindowRecoveryState = new();
            _mainWindowViewModel.Settings.SaveSettings();
            SetWindowState(_mainWindowViewModel.Settings.WindowRecoveryState);
        }
        
        Logger.Info($"VS2077 Version: {_mainWindowViewModel.Settings.ProgramVersion}");
        _mainWindowViewModel.IsProcessing = true;
        var validationResult = ValidationService.ValidateGamePath(_mainWindowViewModel.Settings.GameDirectory).Item1;
        if (!(validationResult == GamePathValidationResult.Valid ||
              validationResult == GamePathValidationResult.CetNotFound))
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
                await DialogService.ShowDialog("Game Is Running",
                    "The game was running during the update, to apply changes restart the game or reload CET mods.",
                    new[]
                    {
                        new DialogButton("Ok", DialogButtonStyling.Primary)
                    }, this);
                var changelog = await UpdateService.GetChangelog();
                Logger.Success($"Successfully updated to {changelog.Item1}");
                Logger.Info($"Changelog:" +
                            $"\n{changelog.Item2}");
                _mainWindowViewModel.Settings.GameRunningDuringUpdate = false;
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
            Logger.Exception(ex, "Failed to Check for Updates.");
        }
        var success = await Task.Run(() => GameFileService.Instance.Initialize());
        
        if (success)
            _mainWindowViewModel.AppInitialized = true;
        _mainWindowViewModel.IsProcessing = false;
    }

    private void OnMainWindowClosed(object? sender, EventArgs e)
    {
        _mainWindowViewModel.Settings.WindowRecoveryState.WindowState = WindowState == WindowState.Maximized ? 2 : 0;
        AddQueuedFilters();
        _mainWindowViewModel.Settings.SaveSettings();
        _mainWindowViewModel.DebugWindowInstance?.Close();
    }
}