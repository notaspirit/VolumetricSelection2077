using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Interactivity;
using DynamicData;
using Serilog.Events;
using VolumetricSelection2077.Models;

namespace VolumetricSelection2077.Views
{
    public partial class LogViewer : UserControl
    {
        private ScrollViewer? _scrollViewer;
        public ObservableCollection<LogMessage> LogMessages { get; } = new();
        private List<LogMessage> _pendingMessages = new();
        private DispatcherTimer _batchTimer;
        private readonly object _logLock = new();
        private const int MaxLogMessages = 10000;
        
        public LogViewer()
        {
            InitializeComponent();
            DataContext = this;
            
            _batchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _batchTimer.Tick += (_, _) => ProcessPendingMessages();
            _batchTimer.Start();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            _scrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
        }

        public void AddLogMessage(string message, LogEventLevel level)
        {
            lock (_logLock)
            {
                _pendingMessages.Add(new LogMessage(message, level));
            }
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
                
                _scrollViewer?.ScrollToEnd();
            });
        }
        
        public void ClearLog()
        {
            Dispatcher.UIThread.Post(() =>
            {
                LogMessages.Clear();
            });
        }
    }
}