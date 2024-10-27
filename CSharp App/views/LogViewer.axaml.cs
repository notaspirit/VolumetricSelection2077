using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;

namespace VolumetricSelection2077.Views
{
    public partial class LogViewer : UserControl
    {
        public ObservableCollection<string> LogMessages { get; } = new();

        public LogViewer()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void AddLogMessage(string message)
        {
            Dispatcher.UIThread.Post(() => LogMessages.Add(message));
        }
    }
}