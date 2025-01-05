using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Serilog.Events;
using VolumetricSelection2077.Models;

namespace VolumetricSelection2077.Views
{
    public partial class LogViewer : UserControl
    {
        private ScrollViewer? _scrollViewer;
        public ObservableCollection<LogMessage> LogMessages { get; } = new();

        public LogViewer()
        {
            InitializeComponent();
            DataContext = this;
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            _scrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
        }

        public void AddLogMessage(string message, LogEventLevel level)
        {
            Dispatcher.UIThread.Post(() =>
            {
                LogMessages.Add(new LogMessage(message, level));
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