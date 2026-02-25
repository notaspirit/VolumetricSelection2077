using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace VolumetricSelection2077.Services;

/// <summary>
/// Currently supports Windows and Linux, created to gather all the OS specific code in one place (except UpdateService)
/// </summary>
public class OsUtilsService
{
    public static void OpenFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        
        if (OperatingSystem.IsWindows())
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
        else if (OperatingSystem.IsLinux())
            Process.Start(new ProcessStartInfo("xdg-open", $"\"{path}\"") { UseShellExecute = true });
    }

    public static void RestartApp()
    {
        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "VolumetricSelection2077.exe")));
        }
        else if (OperatingSystem.IsLinux())
        {
            var execPath = Path.Combine(AppContext.BaseDirectory, "VolumetricSelection2077");
            var mode = File.GetUnixFileMode(execPath);
            mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            File.SetUnixFileMode(execPath, mode);

            Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"nohup '{execPath}' > /dev/null 2>&1 &\"",
                UseShellExecute = false
            });
        }
        Environment.Exit(0);
    }
}