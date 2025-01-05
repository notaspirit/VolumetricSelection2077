// @author spirit
// @version 1.0.0

// For Reference

const exampleJson = {
    "requests": [
        {
            requestType: "json",
            path: "base/somefile/path.whatever",
            isFulfilled: false
        },
        {
            requestType: "glb",
            path: "base/somefile/path.whatever",
            isFulfilled: false
        },
        {
            requestType: "hash",
            sectorHash: "somehash",
            actorHash: "somehash",
            path: "geometryHash/somefile/path.json",
            isFulfilled: false
        }

    ]
};


// Settings

const refreshDelay = 10 * 1000; // 10 seconds
const maxIdleCycles = 30; // 5 minutes
const apiFilename = "VS2077\\requests.json";

// Imports
import * as Logger from "Logger.wscript";
import * as TypeHelper from "TypeHelper.wscript";

// Global variables
let idleCycles = 0;

// functions

// ---@param string path
// ---@param string newExptension
// ---@return string path
function replaceExtension(path, newExptension) {
    return path.replace(/\.[^/.]+$/, newExptension);
}

// ---@return array<string>
function getRequests() {
    Logger.Info("GetRequests called!");
    if (wkit.FileExistsInRaw(apiFilename)) {
        const requests = JSON.parse(wkit.LoadRawJsonFromProject(apiFilename, "json"));
        if (requests == null) {
            Logger.Error("Error loading requests from " + apiFilename);
            return [];
        }
        Logger.Info("Returning requests!");
        return requests.requests;
    } else {
        return [];
    }
}

// ---@param string filename
// ---@returns {success: boolean, error: string}
function uncookToJson(filename) {
    if (!wkit.FileExists(filename)) {
        return {success: false, error: filename + "does not exist"};
    }

    var file = wkit.GetFile(filename, OpenAs.Json);
    if (file == null) {
        return {success: false, error: "Failed to open" + filename + "as json"};
    }
    wkit.SaveToRaw(replaceExtension(filename, ".json"), file);
    return {success: true, error: ""};
}

// ---@param string filename
// ---@returns {success: boolean, error: string}
function uncookToGlb(filename) {
    if (!wkit.FileExists(filename)) {
        return {success: false, error: filename + "does not exist"};
    }

    wkit.Extract(filename);

    wkit.ExportFiles([filename]);
    return {success: true, error: ""};
}

// ---@param string sectorHash
// ---@param string actorHash
// ---@param string path
// ---@returns {success: boolean, error: string}
function uncookHash(sectorHash, actorHash, path) {
    const geometryJson = wkit.ExportGeometryCacheEntry(sectorHash, actorHash);
    if (geometryJson == null) {
        return {success: false, error: "Failed to export geometry cache entry for " + "sector " + sectorHash + " actor " + actorHash};
    }

    wkit.SaveToRaw(path, geometryJson);
    return {success: true, error: ""};
}

function main () {
    Logger.Info("Starting Volumetric Selection 2077 Service");
    var index = 0;
    while (true) {
        index++;
        if (index > 10) {
            break;
        }
//        time.sleep(refreshDelay);
        const requests = getRequests();
        if (idleCycles > maxIdleCycles) { 
            const maxIdleCycles = maxIdleCycles * refreshDelay / 1000 / 60;
            Logger.Warning("Service is idling for " + toString(maxIdleCycles) + ", shutting down! Run script again to continue...");
            break;
        }
        if (requests.length == 0) {
            // idleCycles++;
            Logger.Info("No requests found.");
            continue;
        }
        Logger.Info("Request " + requests.length + " found!");
        Logger.Info(JSON.stringify(requests));

        // idleCycles = 0;

        for (var i = 0; i < requests.length; i++) {
            Logger.Info("Found " + requests[i] + " requests");
            const request = requests[i];
            if (request.isFulfilled) {
                continue;
            }

            switch (request.requestType) {
                case "json":
                    Logger.Info("Found " + JSON.stringify(request));
                    const resultJson = uncookToJson(request.path);
                    if (resultJson.success) {
                        request.isFulfilled = true;
                    } else {
                        Logger.Error("Failed to uncook json: " + resultJson.error);
                    }
                    break;
                case "glb":
                    Logger.Info("Found " + JSON.stringify(request));
                    const resultGlb = uncookToGlb(request.path);
                    if (resultGlb.success) {
                        request.isFulfilled = true;
                    } else {
                        Logger.Error("Failed to uncook glb: " + resultGlb.error);
                    }
                    break;
                case "hash":
                    Logger.Info("Found " + JSON.stringify(request));
                    const resultHash = uncookHash(request.sectorHash, request.actorHash, request.path);
                    if (resultHash.success) {
                        request.isFulfilled = true;
                    } else {
                        Logger.Error("Failed to uncook hash: " + resultHash.error);
                    }
                    break;
                default:
                    Logger.Warning("Unknown request type: " + request.requestType);
                    break;
            }
        }
        wkit.SaveToRaw(apiFilename, JSON.stringify({requests: requests}));
    }
}

main();