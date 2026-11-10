namespace VolumetricSelection2077.Core.Models;

public class VSConsoleLogger : IVSLogger
{
    private static string FormatMessage(string message, string level)
        => $"[{level}] {message}";
    
    public void Success(string message, bool fileOnly = false)
    {
        if (!fileOnly)
            Console.WriteLine(FormatMessage(message, "Success"));
    }

    public void Error(string message, bool fileOnly = false)
    {
        if (!fileOnly)
            Console.WriteLine(FormatMessage(message, "Error  "));
    }

    public void Exception(Exception ex, string? message = null, bool fileOnly = false)
    {
        if (!fileOnly)
            Console.WriteLine(FormatMessage($"{ex.Message}\n{message}", "Error  "));
    }

    public void Info(string message, bool fileOnly = false)
    {
        if (!fileOnly)
            Console.WriteLine(FormatMessage(message, "Info   "));
    }

    public void Debug(string message, bool fileOnly = false)
    {
        if (!fileOnly)
            Console.WriteLine(FormatMessage(message, "Debug  "));
    }

    public void Warning(string message, bool fileOnly = false)
    {
        if (!fileOnly)
            Console.WriteLine(FormatMessage(message, "Warning"));
    }
}