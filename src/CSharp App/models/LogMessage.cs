using Serilog.Events;

namespace VolumetricSelection2077.Models
{
    public class LogMessage
    {
        public string Text { get; set; }
        public LogEventLevel Level { get; set; }

        public LogMessage(string text, LogEventLevel level)
        {
            Text = text;
            Level = level;
        }
    }
}