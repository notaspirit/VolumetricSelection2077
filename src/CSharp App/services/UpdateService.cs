using System;
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
                output[3] = 0;
                output[4] = 0;
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
}