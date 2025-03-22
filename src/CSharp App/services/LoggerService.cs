using Serilog;
using Serilog.Core;
using System;
using System.IO;
using System.Collections.Generic;

namespace VolumetricSelection2077.Services
{
    public static class Logger
    {
        private static ILogger? _uiLogger;
        private static ILogger? _fileLogger;
        private static readonly List<ILogEventSink> _sinks = new();
        private static SettingsService _settingsService;
        
        public static void Initialize(string logDirectory)
        {
            var logFileName = Path.Combine(logDirectory, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            string outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message:lj}{NewLine}{Exception}";

            var uiLoggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: outputTemplate);

            var fileLoggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFileName, outputTemplate: outputTemplate);
            
            foreach (var sink in _sinks)
            {
                uiLoggerConfig = uiLoggerConfig.WriteTo.Sink(sink);
            }

            _uiLogger = uiLoggerConfig.CreateLogger();
            _fileLogger = fileLoggerConfig.CreateLogger();
            _settingsService = SettingsService.Instance;
        }

        public static void AddSink(ILogEventSink sink)
        {
            _sinks.Add(sink);
            if (_uiLogger != null)
            {
                _uiLogger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Sink(sink)
                    .WriteTo.Logger(_uiLogger)
                    .CreateLogger();
            }
        }

        private static string FormatMessage(string message, string level)
            => $"[{level}] {message}";

        public static void Info(string message, bool fileOnly = false)
        {
            if (!fileOnly)
                _uiLogger?.Information(FormatMessage(message, "Info   "));
            _fileLogger?.Information(FormatMessage(message, "Info   "));
        }


        public static void Warning(string message, bool fileOnly = false) 
        {
            if (!fileOnly)
                _uiLogger?.Warning(FormatMessage(message, "Warning"));
            _fileLogger?.Warning(FormatMessage(message, "Warning"));
        }

        public static void Error(string message, bool fileOnly = false) 
        {
            if (!fileOnly)
                _uiLogger?.Error(FormatMessage(message, "Error  "));
            _fileLogger?.Error(FormatMessage(message, "Error  "));
        }

        public static void Debug(string message, bool fileOnly = false)
        {
            if (_settingsService.DebugMode)
            {
                if (!fileOnly)
                    _uiLogger?.Debug(FormatMessage(message, "Debug  "));
                _fileLogger?.Debug(FormatMessage(message, "Debug  "));
            }
        }

        public static void Success(string message, bool fileOnly = false)
        {
            if (!fileOnly)
                _uiLogger?.Fatal(FormatMessage(message, "Success"));
            _fileLogger?.Fatal(FormatMessage(message, "Success"));
        }
    }
}