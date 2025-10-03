using Serilog;
using Serilog.Core;
using System;
using System.IO;
using System.Collections.Generic;
using VolumetricSelection2077.Enums;

namespace VolumetricSelection2077.Services
{
    public static class Logger
    {
        private static ILogger? _uiLogger;
        private static ILogger? _fileLogger;
        private static readonly List<ILogEventSink> _sinks = new();
        private static SettingsService _settingsService;
        
        /// <summary>
        /// Initializes the logger service
        /// </summary>
        /// <param name="logDirectory">The directory to create the log file in</param>
        /// <exception cref="ArgumentException">The provided path is not a valid absolute path or not a directory</exception>
        /// <exception cref="IOException"></exception>
        public static void Initialize(string logDirectory)
        {
            var vR = ValidationService.ValidatePath(logDirectory);
            if (vR != PathValidationResult.Valid)
                throw new ArgumentException($"Invalid log directory: {logDirectory}, {vR}");

            Directory.CreateDirectory(logDirectory);
            
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

        /// <summary>
        /// Adds a sink to the UI Logger
        /// </summary>
        /// <param name="sink">The sink to add</param>
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
        
        /// <summary>
        /// Formats the message and level, does not account for date and time
        /// </summary>
        /// <param name="message">The message body</param>
        /// <param name="level">The log level (with proper spacing)</param>
        /// <returns>Formatted string</returns>
        private static string FormatMessage(string message, string level)
            => $"[{level}] {message}";
        
        /// <summary>
        /// Logs an info message 
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="fileOnly">Only log to the file, but not the Ui</param>
        public static void Info(string message, bool fileOnly = false)
        {
            if (!fileOnly)
                _uiLogger?.Information(FormatMessage(message, "Info   "));
            _fileLogger?.Information(FormatMessage(message, "Info   "));
        }

        /// <summary>
        /// Logs a warning message 
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="fileOnly">Only log to the file, but not the Ui</param>
        public static void Warning(string message, bool fileOnly = false) 
        {
            if (!fileOnly)
                _uiLogger?.Warning(FormatMessage(message, "Warning"));
            _fileLogger?.Warning(FormatMessage(message, "Warning"));
        }
        
        /// <summary>
        /// Logs an error message 
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="fileOnly">Only log to the file, but not the Ui</param>
        public static void Error(string message, bool fileOnly = false) 
        {
            if (!fileOnly)
                _uiLogger?.Error(FormatMessage(message, "Error  "));
            _fileLogger?.Error(FormatMessage(message, "Error  "));
        }
        
        /// <summary>
        /// Logs a debug message only if debug mode is enabled
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="fileOnly">Only log to the file, but not the Ui</param>
        public static void Debug(string message, bool fileOnly = false)
        {
            if (_settingsService.DebugMode)
            {
                if (!fileOnly)
                    _uiLogger?.Debug(FormatMessage(message, "Debug  "));
                _fileLogger?.Debug(FormatMessage(message, "Debug  "));
            }
        }
        
        /// <summary>
        /// Logs a success message 
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="fileOnly">Only log to the file, but not the Ui</param>
        public static void Success(string message, bool fileOnly = false)
        {
            if (!fileOnly)
                _uiLogger?.Fatal(FormatMessage(message, "Success"));
            _fileLogger?.Fatal(FormatMessage(message, "Success"));
        }
        
        /// <summary>
        /// Logs an exception message and custom message to ui, logs entire exception to file
        /// </summary>
        /// <param name="exception">the exception to be logged</param>
        /// <param name="message">additional message</param>
        /// <param name="fileOnly">Only log to the file, but not the Ui</param>
        public static void Exception(Exception exception, string? message = null, bool fileOnly = false)
        {
            string errorMessage = exception.Message + (message == null ? "" : $" : {message}");
            if (!fileOnly)
                _uiLogger?.Error(FormatMessage(errorMessage  + " For more info see log file.", "Error  "));
            string fullErrorMessage = errorMessage + Environment.NewLine + exception;
            _fileLogger?.Error(FormatMessage(fullErrorMessage, "Error  "));
        }
    }
}