using Serilog;
using Serilog.Core;
using System;
using System.IO;
using System.Collections.Generic;

namespace VolumetricSelection2077.Services
{
    public static class Logger
    {
        private static ILogger? _logger;
        private static readonly List<ILogEventSink> _sinks = new();
        private static SettingsService _settingsService;
        
        public static void Initialize(string logDirectory)
        {
            var logFileName = Path.Combine(logDirectory, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            // string outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message:lj}{NewLine}{Exception}";
            string outputTemplate = "{Message:lj}\n";

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFileName, outputTemplate: outputTemplate)
                .WriteTo.Console(outputTemplate: outputTemplate);

            foreach (var sink in _sinks)
            {
                loggerConfig = loggerConfig.WriteTo.Sink(sink);
            }

            _logger = loggerConfig.CreateLogger();
            _settingsService = SettingsService.Instance;
        }

        public static void AddSink(ILogEventSink sink)
        {
            _sinks.Add(sink);
            if (_logger != null)
            {
                _logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Sink(sink)
                    .WriteTo.Logger(_logger)
                    .CreateLogger();
            }
        }

        private static string FormatMessage(string message, string level)
            => $"[{level}] {message}";

        public static void Info(string message) 
            => _logger?.Information(FormatMessage(message, "Info   "));

        public static void Warning(string message) 
            => _logger?.Warning(FormatMessage(message, "Warning"));

        public static void Error(string message) 
            => _logger?.Error(FormatMessage(message, "Error  "));

        public static void Debug(string message)
        {
            if (true)
            {
                _logger?.Debug(message);
                return;
            }
            if (_settingsService.DebugMode)
            {
                _logger?.Debug(FormatMessage(message, "Debug  "));
            }
        }

        public static void Success(string message) 
            => _logger?.Fatal(FormatMessage(message, "Success"));
    }
}