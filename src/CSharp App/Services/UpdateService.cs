using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Octokit;
using VolumetricSelection2077.Enums;

namespace VolumetricSelection2077.Services;

public class UpdateService
{
    /// <summary>
    /// Gets the most recent version and changelog
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ApiException">Fails to get remove info</exception>
    public static async Task<(string, string)> GetChangelog()
    {
        var client = new GitHubClient(new ProductHeaderValue("VolumetricSelection2077"));
        var release = await client.Repository.Release.GetLatest("notaspirit", "VolumetricSelection2077");
        return (release.TagName.Replace("v", ""), release.Body);
    }
    
    /// <summary>
    /// Checks if a newer version is available
    /// </summary>
    /// <returns>true and new version string if a newer remote version is found</returns>
    /// <exception cref="ApiException">Fails to get remote repositiory</exception>
    /// <exception cref="ArgumentException">local or remote version is empty</exception>
    /// <exception cref="Exception">failure to parse version strings</exception>
    public static async Task<(bool, string?)> CheckUpdates()
    {
        var client = new GitHubClient(new ProductHeaderValue("VolumetricSelection2077"));
        var release = await client.Repository.Release.GetLatest("notaspirit", "VolumetricSelection2077");
        var remoteVersion = release.TagName;
        var localVersion = SettingsService.Instance.ProgramVersion;

        var remoteVersionArray = ParseVersion(remoteVersion);
        var localVersionArray = ParseVersion(localVersion);

        for (int i = 0; i < localVersionArray.Length; i++)
        {
            if (remoteVersionArray[i] > localVersionArray[i])
            {
                return (true, remoteVersion.Replace("v", ""));
            }

            if (remoteVersionArray[i] < localVersionArray[i])
            {
                return (false, null);
            }
        }
        return (false, null);
    }
    /// <summary>
    /// Downloads and installs most recent version for app and cet, restarts client and sets flag to show changelog on next startup 
    /// </summary>
    /// <exception cref="Exception">See exception message for more info</exception>
    public static async Task Update()
    {
        
        string? downloadUrlApp = null;
        string? downloadUrlCet = null;
        
        try
        {
            var client = new GitHubClient(new ProductHeaderValue("VolumetricSelection2077"));
            var release = await client.Repository.Release.GetLatest("notaspirit", "VolumetricSelection2077");
            foreach (var asset in release.Assets)
            {
                if (asset.Name.Contains("portable"))
                {
                    downloadUrlApp = asset.BrowserDownloadUrl;
                }

                if (asset.Name.Contains("cet"))
                {
                    downloadUrlCet = asset.BrowserDownloadUrl;
                }
            }

            if (downloadUrlApp == null)
            {
                throw new Exception($"Did not find portable release asset for {release.Name}");
            }

            if (downloadUrlCet == null)
            {
                throw new Exception($"Did not find cet release asset for {release.Name}");
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Failed find remote update location or could not find all required assets", e);
        }

        

        string rootTempPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VolumetricSelection2077", "temp");
        Directory.CreateDirectory(rootTempPath);
        string downloadPathApp = Path.Combine(rootTempPath, "latest-release-app.zip");
        string downloadPathCet = Path.Combine(rootTempPath, "latest-release-cet.zip");
        string unzipPath = Path.Combine(rootTempPath, "unzip");
        string unzipPathCetTemp = Path.Combine(rootTempPath, "unzip-cet");
        Directory.CreateDirectory(unzipPath);
        
        try
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "VolumetricSelection2077");

            var responseApp = await httpClient.GetAsync(downloadUrlApp);
            responseApp.EnsureSuccessStatusCode();
            await File.WriteAllBytesAsync(downloadPathApp, await responseApp.Content.ReadAsByteArrayAsync());
            
            var responseCet = await httpClient.GetAsync(downloadUrlCet);
            responseCet.EnsureSuccessStatusCode();
            await File.WriteAllBytesAsync(downloadPathCet, await responseCet.Content.ReadAsByteArrayAsync());
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to download latest release", ex);
        }
        
        try
        {
            string unzipPathCet;
            if (string.IsNullOrEmpty(SettingsService.Instance.CETInstallLocation))
            {
                if (ValidationService.ValidateGamePath(SettingsService.Instance.GameDirectory).Item1 != GamePathValidationResult.Valid)
                {
                    throw new Exception("Could not find valid target location to install VS2077 CET. Please set the game path or custom directory in the settings and restart the application to try again.");
                }
                unzipPathCet = SettingsService.Instance.GameDirectory;
            }
            else
            {
                unzipPathCet = SettingsService.Instance.CETInstallLocation;
            }
            Directory.CreateDirectory(unzipPath);
            ZipFile.ExtractToDirectory(downloadPathApp, unzipPath, true);
            ZipFile.ExtractToDirectory(downloadPathCet, unzipPathCetTemp, true);
            MoveDirectoryWithOverwrite(Path.Join(unzipPathCetTemp, "bin"), Path.Join(unzipPathCet, "bin"));
            try
            {
                MoveDirectoryWithOverwrite(Path.Join(unzipPathCetTemp, "archive", "pc", "mod"),
                    Path.Join(unzipPathCet, "archive", "pc", "mod"));
            }
            catch
            {
                MoveDirectoryWithOverwrite(Path.Join(unzipPathCetTemp, "archive", "pc", "mod"),
                    Path.Join(unzipPathCet, "archive", "pc", "hot"));
                SettingsService.Instance.GameRunningDuringUpdate = true;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to unzip latest release", ex);
        }
        
        var exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "VolumetricSelection2077.exe");
        var scriptPath = Path.Combine(rootTempPath, "update.ps1");
        var vbsScriptPath = Path.Combine(rootTempPath, "update.vbs");
        File.WriteAllText(scriptPath, $@"
$exePath = ""{exePath}""
$unzipPath = ""{unzipPath}""
$appBaseDir = ""{AppContext.BaseDirectory}""
$rootTempPath = ""{rootTempPath}""

while (Get-Process -Name (Split-Path $exePath -LeafBase) -ErrorAction SilentlyContinue) {{
    Start-Sleep -Seconds 1
}}

Remove-Item -Path $appBaseDir -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item -Path ""$unzipPath\*"" -Destination $appBaseDir -Recurse -Force

Start-Process -FilePath $exePath

Remove-Item -Path $rootTempPath -Recurse -Force -ErrorAction SilentlyContinue

");
        
        File.WriteAllText(vbsScriptPath, $@"Set objShell = CreateObject(""WScript.Shell"")
objShell.Run ""powershell.exe -ExecutionPolicy Bypass -File """"{scriptPath}"""""", 0, False
");
        Process.Start(new ProcessStartInfo
        {
            FileName = "wscript.exe",
            Arguments = $"\"{vbsScriptPath}\"",
            UseShellExecute = true
        });
        
        SettingsService.Instance.DidUpdate = true;
        SettingsService.Instance.SaveSettings();
        Environment.Exit(0);
    }
    
    private static void MoveDirectoryWithOverwrite(string source, string destination)
    {
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException($"Source directory '{source}' does not exist or could not be found.");
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var destFile = Path.Combine(destination, Path.GetFileName(file));
            
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            File.Move(file, destFile, true);
        }
        Directory.Delete(source, true);
    }
    
    /// <summary>
    /// Parses the version string into an array of int 
    /// </summary>
    /// <param name="version"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">The type of the version does not match known types, or empty string provided</exception>
    /// <exception cref="Exception">Fails to parse parts to int or fails to replace using regex</exception>
    private static int[] ParseVersion(string version)
    {
        if (string.IsNullOrEmpty(version))
            throw new ArgumentException($"Invalid version input: {version}");
        try
        {
            var rawDot = version.Replace("v", "").Split('.');
            var rawDash = version.Replace("v", "").Split('-');
            int[] output = new int[5];
            output[0] = int.Parse(rawDot[0]);
            output[1] = int.Parse(rawDot[1]);
            output[2] = int.Parse(rawDot[2].Split("-")[0]);
            if (rawDash.Length == 1)
            {
                output[3] = 1000;
                output[4] = 1000;
            }
            else
            {
                switch (Regex.Replace(rawDash[1], @"\d", ""))
                {
                    case "alpha":
                        output[3] = 1;
                        break;
                    case "beta":
                        output[3] = 2;
                        break;
                    case "rc":
                        output[3] = 3;
                        break;
                    default:
                        throw new ArgumentException($"Unknown version type: {Regex.Replace(rawDash[1], @"\d", "")}");
                }
                output[4] = int.Parse(Regex.Replace(rawDash[1], @"\D", ""));
            }
            return output;
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to parse version: {version}", e);
        }
    }
}