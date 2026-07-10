using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Interactivity;
using DynamicData;
using Serilog.Events;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.Views;

public partial class LogViewer : UserControl
{
    private ScrollViewer? _scrollViewer;
    private readonly List<LogMessage> _pendingMessages = new();
    private readonly DispatcherTimer _batchTimer;
    private readonly object _logLock = new();
    private const int MaxLogMessages = 10000;
    private readonly SettingsService _settings;
    
    public ObservableCollection<LogMessage> LogMessages { get; } = new();
    public bool AutoScroll
    {
        get => _settings.Data.AutoScrollLogViewer;
        set
        {
            _settings.Data.AutoScrollLogViewer = value;
            _settings.SaveSettings();
        }
    }

    public LogViewer()
    {
        DataContext = this;
        
        _settings = SettingsService.Instance;

        _batchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _batchTimer.Tick += (_, _) => ProcessPendingMessages();
        _batchTimer.Start();
        InitializeComponent();
    }

    public void AddLogMessage(string message, LogEventLevel level)
    {
        lock (_logLock)
        {
            _pendingMessages.Add(new LogMessage(message, level));
        }
    }
    
    public void ClearLog()
    {
        Dispatcher.UIThread.Post(() =>
        {
            LogMessages.Clear();
        });
    }
    
    private void ClearLog_Click(object? sender, RoutedEventArgs e)
    {
        ClearLog();
    }
    
    private void OpenLogFolder_Click(object? sender, RoutedEventArgs e)
    {
        OsUtilsService.OpenFolder(_settings.LogDirectory);
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _scrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
    }
    
    private void ProcessPendingMessages()
    {
        List<LogMessage> messagesToAdd;

        lock (_logLock)
        {
            if (_pendingMessages.Count == 0) return;
            messagesToAdd = new List<LogMessage>(_pendingMessages);
            _pendingMessages.Clear();
        }

        Dispatcher.UIThread.Post(() =>
        {
            LogMessages.AddRange(messagesToAdd);
            
            while (LogMessages.Count > MaxLogMessages)
            {
                LogMessages.RemoveAt(0);
            }
            if (AutoScroll)
                _scrollViewer?.ScrollToEnd();
        });
    }
}
