using Serilog.Core;
using Serilog.Events;
using Avalonia.Threading;
using VolumetricSelection2077.Views;

namespace VolumetricSelection2077.Services
{
    public class LogViewerSink : ILogEventSink
    {
        private readonly LogViewer _logViewer;

        public LogViewerSink(LogViewer logViewer)
        {
            _logViewer = logViewer;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _logViewer.AddLogMessage(message);
            });
        }
    }
}