using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using VolumetricSelection2077.Resources;
using System.Collections.Generic;
using System.Linq;
using WolvenKit.RED4.Archive.IO;
using WolvenKit.RED4.Archive.CR2W;
using System.Text;

namespace VolumetricSelection2077.Services
{
    public class WolvenkitCLIService
    {
        private readonly CacheService _cacheService;
        private readonly SettingsService _settingsService;
        private readonly UtilService _utilService;
        public WolvenkitCLIService()
        {
            _cacheService = CacheService.Instance;
            _settingsService = SettingsService.Instance;
            _utilService = new UtilService();
        }
        
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
            string cliDirPath = _settingsService.WolvenkitCLIPath;
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
                string cliPath = _settingsService.WolvenkitCLIPath;
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
            var (output, error) = await ExecuteCommand($"archiveinfo \"{_utilService.EscapeSlashes(archiveFilePath)}\" --regex {regex} --list");
            if (!string.IsNullOrEmpty(error))
            {
                Logger.Error($"Failed to get archive info: {error}");
                return new List<string>();
            }
            return output.Split('\n').ToList();
        }
        public async Task<(bool success, string error, byte[]? file)> ExtractRawFile(string filePath)
        {
            Logger.Info($"Extracting raw file: {filePath}");
            // If not found, get the archive file index from the file map cache
            var (FMsuccess, FMoutput, FMerror) = _cacheService.GetEntry(CacheDatabase.FileMap.ToString(), filePath);
            if (!FMsuccess || FMoutput == null || !string.IsNullOrEmpty(FMerror))
            {
                Logger.Error($"Failed to get file map entry: {FMerror}");
                return (false, FMerror, null);
            }
            Logger.Info($"Found archive file index in file map cache: {BitConverter.ToInt32(FMoutput)}");
            // Get the archive file path from the file map cache using the archive file index
            var (AFsuccess, AFoutput, AFerror) = _cacheService.GetEntry(CacheDatabase.FileMap.ToString(), BitConverter.ToInt32(FMoutput).ToString());
            if (!AFsuccess || AFoutput == null || !string.IsNullOrEmpty(AFerror))
            {
                Logger.Error($"Failed to get file map entry: {AFerror}");
                return (false, AFerror, null);
            }
            string archiveFilePathRel = Encoding.UTF8.GetString(AFoutput);
            Logger.Info($"Found archive file path in file map cache: {archiveFilePathRel}");
            string archiveFilePath = Path.Combine(_settingsService.GameDirectory, archiveFilePathRel);
            // extract the file from the archive file
            var (extractOutput, extractError) = await ExecuteCommand($"extract \"{_utilService.EscapeSlashes(archiveFilePath)}\" --pattern \"{_utilService.EscapeSlashes(filePath)}\" --outpath \"{_utilService.EscapeSlashes(Path.Combine(_settingsService.CacheDirectory, "working"))}\"");
            if (!string.IsNullOrEmpty(extractError))
            {
                Logger.Error($"Failed to extract file from archive: {extractError}");
                return (false, extractError, null);
            }
            Logger.Info($"Extracted file from archive: {extractOutput}");
            // build the extracted file path
            string extractedFilePath = Path.Combine(_settingsService.CacheDirectory, "working", filePath);
            // check if the file exists
            if (!File.Exists(extractedFilePath))
            {
                Logger.Error($"Failed to find extracted file: {extractedFilePath}");
                return (false, "Failed to find extracted file", null);
            }
            Logger.Info($"Found extracted file: {extractedFilePath}");
            byte[] fileBytes;
            try 
            {
                fileBytes = File.ReadAllBytes(extractedFilePath);
                return (true, string.Empty, fileBytes);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to read extracted file: {ex.Message}");
                return (false, $"Failed to read extracted file: {ex.Message}", null);
            }
            finally 
            {
                // This will run even if reading the file fails
                if (File.Exists(extractedFilePath))
                {
                    File.Delete(extractedFilePath);
                    Logger.Info($"Deleted extracted file from disk: {extractedFilePath}");
                }
            }
        }
        public async Task<(bool success, string error, Dictionary<string, byte[]>? files)> ExtractBulkJsonFiles(List<string> filePaths)
        {
            if (filePaths == null || filePaths.Count == 0)
            {
                return (false, "No files specified for extraction", null);
            }

            // Get unique archive paths for these files
            HashSet<string> archivePaths = new HashSet<string>();
            foreach (var filePath in filePaths)
            {
                var (FMsuccess, FMoutput, FMerror) = _cacheService.GetEntry(CacheDatabase.FileMap.ToString(), filePath);
                if (!FMsuccess || FMoutput == null) continue;
                
                var archiveIndex = BitConverter.ToInt32(FMoutput);
                var (AFsuccess, AFoutput, AFerror) = _cacheService.GetEntry(CacheDatabase.FileMap.ToString(), archiveIndex.ToString());
                if (!AFsuccess || AFoutput == null) continue;

                string archivePath = Encoding.UTF8.GetString(AFoutput);
                archivePaths.Add(archivePath);
            }

            if (archivePaths.Count == 0)
            {
                return (false, "No valid archive paths found", null);
            }

            // Extract files from each archive
            Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
            List<string> extractedFilePaths = new List<string>();
            string outPath = Path.Combine(_settingsService.CacheDirectory, "working");

            foreach (var archivePath in archivePaths)
            {
                string archiveFilePathAbs = Path.Combine(_settingsService.GameDirectory, archivePath);
                var (extractOutput, extractError) = await ExecuteCommand($"uncook \"{_utilService.EscapeSlashes(archiveFilePathAbs)}\" --regex \"{_utilService.EscapeSlashes(_utilService.BuildORRegex(filePaths))}\" --outpath \"{_utilService.EscapeSlashes(outPath)}\"");
                if (!string.IsNullOrEmpty(extractError))
                {
                    Logger.Error($"Failed to extract files from archive {archivePath}: {extractError}");
                    continue;
                }
            }

            // Read and collect all extracted files
            try
            {
                foreach (var filePath in filePaths)
                {
                    string filePathAbsJson = Path.Combine(outPath, filePath + ".json");
                    string filePathAbsRaw = Path.Combine(outPath, filePath);
                    if (File.Exists(filePathAbsJson))
                    {
                        files[filePath] = File.ReadAllBytes(filePathAbsJson);
                        extractedFilePaths.Add(filePathAbsJson);
                    }
                    if (File.Exists(filePathAbsRaw))
                    {
                        extractedFilePaths.Add(filePathAbsRaw);
                    }
                }
                return (true, string.Empty, files);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to read extracted files: {ex.Message}");
                return (false, $"Failed to read extracted files: {ex.Message}", null);
            }
            finally
            {
                foreach (var filePath in extractedFilePaths)
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
        }
    }
}