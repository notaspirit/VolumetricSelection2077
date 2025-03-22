using VolumetricSelection2077.Services;
namespace VolumetricSelection2077.TestingStuff;

public class TestLoggerRefactor
{
    public static void Run()
    {
        Logger.Info("Regular Info message");
        Logger.Warning("Regular Warning message");
        Logger.Error("Regular Error message");
        Logger.Success("Regular Success message");
        Logger.Debug("Regular Debug message");
        
        Logger.Info("file only Info message", true);
        Logger.Warning("file only  Warning message", true);
        Logger.Error("file only  Error message", true);
        Logger.Success("file only  Success message", true);
        Logger.Debug("file only  Debug message", true);
    }
}