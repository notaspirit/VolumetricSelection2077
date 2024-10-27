using Serilog.Core;
using Serilog.Events;
using Avalonia.Threading;
using VolumetricSelection2077.Views;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System.IO;

public class LogViewerSink : ILogEventSink
{
    private readonly LogViewer _logViewer;
    private readonly ITextFormatter _formatter;

    public LogViewerSink(LogViewer logViewer, string outputTemplate)
    {
        _logViewer = logViewer;
        _formatter = new MessageTemplateTextFormatter(outputTemplate);
    }

    public void Emit(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        var message = writer.ToString().TrimEnd();
        
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _logViewer.AddLogMessage(message, logEvent.Level);
        });
    }
}