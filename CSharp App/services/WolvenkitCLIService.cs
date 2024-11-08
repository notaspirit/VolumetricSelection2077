using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;


namespace VolumetricSelection2077.Services
{
    public class WolvenkitCLIService
    {
        // Validate Wolvenkit CLI path, doesn't check if it's a valid CLI, only if the file exists -> it's only used as validation when creating the wrapper
        public static bool ValidateWolvenkitCLIPath(string wolvenkitCLIPath)
        {
            //string wolvenkitCLI = Path.Combine(wolvenkitCLIPath, "WolvenKit.CLI.exe");
            if (!File.Exists(wolvenkitCLIPath))
            {
                return false;
            }
            return true;
        }
        public async Task<(string output, string error)> ExecuteCommand(string arguments, int timeoutMs = 30000)
        {
            try {
                string cliPath = SettingsService.Instance.WolvenkitCLIPath;
                string fullPath = Path.Combine(cliPath, "WolvenKit.CLI.exe");
                
                if (!ValidateWolvenkitCLIPath(fullPath))
                {
                    return (string.Empty, "Invalid WolvenKit CLI path in settings");
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(cliPath)
                };

                using var cts = new CancellationTokenSource(timeoutMs);
                using var process = new Process { StartInfo = startInfo };

                var output = new System.Text.StringBuilder();
                var error = new System.Text.StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        output.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        error.AppendLine(e.Data);
                };

                try
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await process.WaitForExitAsync(cts.Token);

                    if (process.ExitCode != 0)
                    {
                        Logger.Error($"CLI process exited with code: {process.ExitCode}. Error: {error}");
                        return (string.Empty, $"CLI process exited with code: {process.ExitCode}. Error: {error}");
                    }

                    return (output.ToString(), error.ToString());
                }
                catch (OperationCanceledException)
                {
                    Logger.Error($"CLI command execution timed out after {timeoutMs}ms");
                    return (string.Empty, $"CLI command execution timed out after {timeoutMs}ms");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error executing CLI command: {ex.Message}");
                return (string.Empty, $"Error executing CLI command: {ex.Message}");
            }
        }
        public async Task<string> GetVersionAsync()
        {
            var (output, error) = await ExecuteCommand("--version");
            if (!string.IsNullOrEmpty(error))
            {
                Logger.Error($"Failed to get WolvenKit version: {error}");
                return string.Empty;
            }
            return output.Trim();
        }
    }
}