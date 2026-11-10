namespace VolumetricSelection2077.Core.Models;

public interface IVSLogger
{
    void Success(string message, bool fileOnly = false);
    void Error(string message, bool fileOnly = false);
    void Exception(Exception ex, string? message = null, bool fileOnly = false);
    void Info(string message, bool fileOnly = false);
    void Debug(string message, bool fileOnly = false);
    void Warning(string message, bool fileOnly = false);
}