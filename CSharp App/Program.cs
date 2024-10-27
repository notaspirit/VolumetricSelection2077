using Avalonia;
using Serilog;
using System;
using System.IO;

namespace VolumetricSelection2077;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDirectory = Path.Combine(appDataPath, "VolumetricSelection2077", "Logs");
        Directory.CreateDirectory(logDirectory); // Ensure the directory exists

        var logFileName = Path.Combine(logDirectory, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
    
        string outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logFileName, outputTemplate: outputTemplate)
            .WriteTo.Console(outputTemplate: outputTemplate)
            .CreateLogger();

        try
        {
            Log.Information("Starting application");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application start-up failed");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}