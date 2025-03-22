using System;
using System.Data;
using System.Security.Authentication;
using VolumetricSelection2077.Services;
using WolvenKit.Modkit.Exceptions;

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

        try
        {
            throw new AuthenticationException("kekking is not allowed!");
        }
        catch (Exception ex)
        {
            Logger.Exception(ex);
        }
        
        try
        {
            throw new EvaluateException("no. Just no.");
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "yeah just don't.");
        }
    }
}