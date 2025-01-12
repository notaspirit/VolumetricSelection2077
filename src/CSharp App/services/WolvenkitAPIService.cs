using System;
using System.IO;
using SharpGLTF.Schema2;
using System.Linq;
using VolumetricSelection2077.Resources;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace VolumetricSelection2077.Services
{

    public class WolvenkitAPIService
    {
        private readonly SettingsService _settings;

        public WolvenkitAPIService()
        {
            _settings = SettingsService.Instance;
        }

        private bool cleanRequest(string requestID)
        {
            string commsFile = _settings.WolvenkitProjectPath + "/source/raw/VS2077/requests.json";
            if (!File.Exists(commsFile))
            {
                Logger.Error("Failed to find requests.json. Unable to clean request.");
                return false;
            }

            string jsonString = File.ReadAllText(commsFile);
            JsonObject? apiFileContent = JsonSerializer.Deserialize<JsonObject>(jsonString);
            if (apiFileContent == null || apiFileContent["requests"] == null)
            {
                Logger.Error("Failed to parse requests.json. Unable to clean request.");
                return false;
            }

            // Fixing the typo and checking for null in the dictionary
            JsonObject? requests = apiFileContent["requests"] as JsonObject;
            if (requests == null)
            {
                Logger.Error("Requests section is missing.");
                return false;
            }

            requests.Remove(requestID);
            File.WriteAllText(commsFile, JsonSerializer.Serialize(apiFileContent));
            return true;
        }

        private async Task<(bool, string, string)> makeRequst(WkitAPITypes.Types requestType, string? filename, string? sectorHash, string? actorHash)
        {
            string commsFile = _settings.WolvenkitProjectPath + "/source/raw/VS2077/requests.json";
            if (!File.Exists(commsFile))
            {
                return (false, $"Failed to find {commsFile}. Unable to make request to Wolvenkit API.", string.Empty);
            }

            string jsonString = File.ReadAllText(commsFile);
            JsonObject? apiFileContent = JsonSerializer.Deserialize<JsonObject>(jsonString);
            string requestTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            if (apiFileContent == null)
            {
                return (false, "Failed to parse requests.json. Unable to make request to Wolvenkit API.", string.Empty);
            }

            if (apiFileContent["requests"] == null)
            {
                apiFileContent["requests"] = new JsonObject();
            }

            JsonObject? requests = apiFileContent["requests"] as JsonObject;
            if (requests == null)
            {
                return (false, "Failed to access 'requests' as a JsonObject.", string.Empty);
            }

            if (requestType == WkitAPITypes.Types.Hash)
            {
                if (actorHash == null || sectorHash == null)
                {
                    return (false, "Both actorHash and sectorHash must be provided for hash request.", string.Empty);
                }

                requests[requestTime] = new JsonObject
                {
                    ["requestType"] = WkitAPITypes.Mapping[requestType],
                    ["isFulfilled"] = false,
                    ["isProcessed"] = false,
                    ["errorMessage"] = "",
                    ["sectorHash"] = sectorHash,
                    ["actorHash"] = actorHash,
                    ["outPath"] = ""
                };
            }
            else if (requestType == WkitAPITypes.Types.Ping)
            {
                requests[requestTime] = new JsonObject
                {
                    ["requestType"] = WkitAPITypes.Mapping[requestType],
                    ["isFulfilled"] = false,
                    ["isProcessed"] = false,
                    ["errorMessage"] = "",
                    ["outPath"] = ""
                };
            }
            else
            {
                if (filename == null)
                {
                    return (false, "Filename must be provided for this request type.", string.Empty);
                }
                requests[requestTime] = new JsonObject
                {
                    ["requestType"] = WkitAPITypes.Mapping[requestType],
                    ["isFulfilled"] = false,
                    ["isProcessed"] = false,
                    ["errorMessage"] = "",
                    ["filename"] = filename,
                    ["outPath"] = ""
                };
            }
            File.WriteAllText(commsFile, JsonSerializer.Serialize(apiFileContent));

            bool isProcessed = false;
            bool isFulfilled = false;
            int retries = 0;
            int requestTimeOutCycles = _settings.WolvenkitAPIRequestIntervalMilliSecondsTimeout / _settings.WolvenkitAPIRequestInterval;
            while (!isFulfilled)
            {
                await Task.Delay(_settings.WolvenkitAPIRequestInterval);
                apiFileContent = JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(commsFile));
                bool? tempIsFullfilled = apiFileContent?["requests"]?[requestTime]?["isFulfilled"]?.GetValue<bool>();
                bool? TempIsProcessed = apiFileContent?["requests"]?[requestTime]?["isProcessed"]?.GetValue<bool>();

                if (isProcessed)
                {
                    isProcessed = true;
                }
                if (isFulfilled)
                {
                    isFulfilled = true;
                }
                retries++;
                if (retries > 3 && isProcessed == false)
                {
                    cleanRequest(requestTime);
                    return (false, $"Failed to get response from Wolvenkit API script. Make sure that the script is running in the expected project.", string.Empty);
                }
                if (retries > requestTimeOutCycles  && isProcessed == true)
                {
                    cleanRequest(requestTime);
                    return (false, $"WolvenkitAPI Script timed out before fullfilling request. Make sure that the script is running in the expected project.", string.Empty);
                }
            }
 
            string? outPath = apiFileContent?["requests"]?[requestTime]?["outPath"]?.GetValue<string>();
            if (string.IsNullOrEmpty(outPath))
            {
                cleanRequest(requestTime);
                return (false, "Failed to get output path from Wolvenkit API script.", string.Empty);
            }
            cleanRequest(requestTime);
            return (true, string.Empty, outPath);
        }

        public async Task<(bool, string, string)> GetWolvenkitAPIScriptVersion()
        {
            string commsFile = _settings.WolvenkitProjectPath + "/source/raw/VS2077/requests.json";
            if (!File.Exists(commsFile))
            {
                return (false, $"Failed to find {commsFile}. Unable to get Wolvenkit API script version. Make sure that the script is running in the expected project.", string.Empty);
            }

            string jsonString = File.ReadAllText(commsFile);
            JsonObject? apiFileContent = JsonSerializer.Deserialize<JsonObject>(jsonString);
            string? version = apiFileContent?["settings"]?["version"]?.GetValue<string>();
            if (string.IsNullOrEmpty(version))
            {
                return (false, "Failed to get Wolvenkit API script version.", string.Empty);
            }
            var (success, error, outPath) = await makeRequst(WkitAPITypes.Types.Ping, null, null, null);
            if (success && outPath == "localhost" )
            {
                return (true, string.Empty, version);
            } 
            else
            {
                return (false, "Failed to ping Wolvenkit API script.", string.Empty);
            }
        }

        public async Task<(bool, string, JsonObject?)> GetFileAsJson(string filePath)
        {
            var (success, error, outPath) = await makeRequst(WkitAPITypes.Types.Json, filePath, null, null);
            if (!success)
            {
                return (false, error, null);
            }
            JsonObject? fileContent = JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(outPath));
            return (true, string.Empty, fileContent);
        }

        public async Task<(bool, string, ModelRoot?)> GetFileAsGlb(string filePath)
        {
            string validPattern = @"\.(mesh|w2mesh)$";
            if (!Regex.IsMatch(filePath, validPattern))
            {
                return (false, $"Invalid file type. Only .mesh and .w2mesh files are supported for GLB conversion.", null);
            }
            var (success, error, outPath) = await makeRequst(WkitAPITypes.Types.Glb, filePath, null, null);
            if (!success)
            {
                return (false, error, null);
            }
            ModelRoot model = ModelRoot.Load(outPath);
            return (true, string.Empty, model);
        }

        public async Task<(bool, string, JsonObject?)> GetGeometryCacheFromHash(string sectorHash, string actorHash)
        {
            var (success, error, outPath) = await makeRequst(WkitAPITypes.Types.Hash, null, sectorHash, actorHash);
            if (!success)
            {
                return (false, error, null);
            }
            JsonObject? fileContent = JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(outPath));
            return (true, string.Empty, fileContent);
        }

        public async Task<(bool, string)> RefreshSettings()
        {
            var (success, error, outPath) = await makeRequst(WkitAPITypes.Types.RefreshSettings, null, null, null);
            if (!success || outPath != "localhost")
            {
                return (false, error);
            }
            return (true, string.Empty);
        }
    }

}