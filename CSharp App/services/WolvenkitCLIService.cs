using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using VolumetricSelection2077.Resources;
using System.Collections.Generic;
using System.Linq;
namespace VolumetricSelection2077.Services
{
    public class WolvenkitCLIService
    {
        // Validate Wolvenkit CLI path, doesn't check if it's a valid CLI, only if the file exists -> it's only used as validation when creating the wrapper
        private static bool ValidateWolvenkitCLIPath(string wolvenkitCLIPath)
        {
            //string wolvenkitCLI = Path.Combine(wolvenkitCLIPath, "WolvenKit.CLI.exe");
            if (!File.Exists(wolvenkitCLIPath))
            {
                return false;
            }
            return true;
        }

        public async void SetSettings()
        {
            string cliDirPath = SettingsService.Instance.WolvenkitCLIPath;
            if (!ValidateWolvenkitCLIPath(Path.Combine(cliDirPath, "WolvenKit.CLI.exe")))
            {
                return;
            }
            string settingsPath = Path.Combine(cliDirPath, "appsettings.json");
            await File.WriteAllTextAsync(settingsPath, new WolvenkitCLISettings().WolvenkitCLISettingsJson);
        }
        public async Task<(string output, string error)> ExecuteCommand(string arguments, int timeoutMs = 300000)
        {
            try
            {
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

                using var process = new Process { StartInfo = startInfo };
                var output = new System.Text.StringBuilder();
                var error = new System.Text.StringBuilder();

                // Set up output handling
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        output.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        error.AppendLine(e.Data);
                        Logger.Error($"CLI Error: {e.Data}");
                    }
                };

                var startTime = DateTime.Now;
                using var progressCts = new CancellationTokenSource();
                
                var progressReportingTask = Task.Run(async () =>
                {
                    while (!progressCts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(30000, progressCts.Token); // Wait 30 seconds
                        var timeSpent = DateTime.Now - startTime;
                        Logger.Info($"Time spent on process so far: {timeSpent.Minutes}m {timeSpent.Seconds}s");
                    }
                }, progressCts.Token);

                Logger.Info($"Starting CLI command: {arguments}");
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for process with timeout
                if (!await WaitForProcessAsync(process, timeoutMs))
                {
                    progressCts.Cancel();
                    try { process.Kill(); } catch { }
                    return (string.Empty, $"Process timed out after {timeoutMs/1000} seconds");
                }

                // Cancel the progress reporting
                progressCts.Cancel();

                if (process.ExitCode != 0)
                {
                    return (string.Empty, $"Process exited with code {process.ExitCode}: {error}");
                }

                var totalTime = DateTime.Now - startTime;
                Logger.Info($"Total time spent: {totalTime.Minutes}m {totalTime.Seconds}s");
                return (output.ToString(), error.ToString());
            }
            catch (Exception ex)
            {
                Logger.Error($"Error executing CLI command: {ex.Message}");
                return (string.Empty, $"Error executing CLI command: {ex.Message}");
            }
        }
        private async Task<bool> WaitForProcessAsync(Process process, int timeoutMs)
        {
            try
            {
                using var cts = new CancellationTokenSource(timeoutMs);
                await process.WaitForExitAsync(cts.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
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
        public async Task<List<string>> ListFilesInArchiveFile(string archiveFilePath, string regex)
        {
            var (output, error) = await ExecuteCommand($"archiveinfo \"{archiveFilePath}\" --regex {regex} --list");
            if (!string.IsNullOrEmpty(error))
            {
                Logger.Error($"Failed to get archive info: {error}");
                return new List<string>();
            }
            return output.Split('\n').ToList();
        }
    }
}