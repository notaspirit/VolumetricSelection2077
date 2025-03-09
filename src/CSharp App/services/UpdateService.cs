using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Octokit;
namespace VolumetricSelection2077.Services;

public class UpdateService
{
    private static int[] ParseVersion(string version)
    {
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
                    case "pr":
                        output[3] = 3;
                        break;
                    default:
                        throw new Exception($"Unknown remote version {Regex.Replace(rawDash[1], @"\d", "")}");
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
    public static async Task Update()
    {
        var client = new GitHubClient(new ProductHeaderValue("VolumetricSelection2077"));
        var release = await client.Repository.Release.GetLatest("notaspirit", "VolumetricSelection2077");
        string? downloadUrl = null;
        foreach (var asset in release.Assets)
        {
            if (asset.Name.Contains("portable"))
            {
                downloadUrl = asset.BrowserDownloadUrl;
            }
        }

        if (downloadUrl == null)
        {
            throw new Exception($"Did not find portable release asset for {release.Name}");
        }

        string rootTempPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VolumetricSelection2077", "temp");
        Directory.CreateDirectory(rootTempPath);
        string downloadPath = Path.Combine(rootTempPath, "latest-release.zip");
        string unzipPath = Path.Combine(rootTempPath, "unzip");
        Directory.CreateDirectory(unzipPath);
        
        try
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "VolumetricSelection2077");

            var response = await httpClient.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();
            await File.WriteAllBytesAsync(downloadPath, await response.Content.ReadAsByteArrayAsync());
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to download latest release", ex);
        }
        
        try
        {
            ZipFile.ExtractToDirectory(downloadPath, unzipPath, true);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to unzip latest release", ex);
        }
        
        var exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "VolumetricSelection2077.exe");
        var scriptPath = Path.Combine(rootTempPath, "update.ps1");
        var vbsScriptPath = Path.Combine(rootTempPath, "update.vbs");
        Logger.Info(exePath);
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
}