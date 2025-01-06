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

const refreshDelay = 10 * 1000;
const maxIdleCycles = 10;
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
    // Logger.Info("GetRequests called!");
    if (wkit.FileExistsInRaw(apiFilename)) {
        const requests = JSON.parse(wkit.LoadRawJsonFromProject(apiFilename, "json"));
        if (requests == null) {
            Logger.Error("Error loading requests from " + apiFilename);
            return [];
        }
        // Logger.Info("Returning requests!");
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

// ---@param string path
// ---returns {success: boolean, error: string}
function listAllFiles(path) {
    const fileList = wkit.GetArchiveFiles();
    if (fileList == null) {
        return {success: false, error: "Failed to get archive files"};
    }

    wkit.SaveToRaw(path, JSON.stringify(fileList));
    return {success: true, error: ""};
}


async function main () {
    Logger.Info("Starting Volumetric Selection 2077 Service");
    var index = 0;
    while (true) {
        Logger.Info("Running loop. . . ")
        Logger.Info("Current Idle Cycles:"  + idleCycles);

        let didProcess = false;
        let changedStatus = false;

        const requests = getRequests();

        for (var i = 0; i < requests.length; i++) {
            //Logger.Info("Found " + requests[i] + " requests");
            const request = requests[i];
            if (request.isFulfilled) {
                continue;
            }
            didProcess = true;

            switch (request.requestType) {
                case "json":
                    //Logger.Info("Found " + JSON.stringify(request));
                    const resultJson = uncookToJson(request.path);
                    if (resultJson.success) {
                        request.isFulfilled = true;
                        changedStatus = true;
                    } else {
                        Logger.Error("Failed to uncook json: " + resultJson.error);
                    }
                    break;
                case "glb":
                    //Logger.Info("Found " + JSON.stringify(request));
                    const resultGlb = uncookToGlb(request.path);
                    if (resultGlb.success) {
                        request.isFulfilled = true;
                        changedStatus = true;
                    } else {
                        Logger.Error("Failed to uncook glb: " + resultGlb.error);
                    }
                    break;
                case "hash":
                    //Logger.Info("Found " + JSON.stringify(request));
                    const resultHash = uncookHash(request.sectorHash, request.actorHash, request.path);
                    if (resultHash.success) {
                        request.isFulfilled = true;
                        changedStatus = true;
                    } else {
                        Logger.Error("Failed to uncook hash: " + resultHash.error);
                    }
                    break;
                case "list":
                    //Logger.Info("Found " + JSON.stringify(request));
                    const resultFiles = listAllFiles(request.path);
                    if (resultFiles.success) {
                        request.isFulfilled = true;
                        changedStatus = true;
                    } else {
                        Logger.Error("Failed to list all files: " + resultFiles.error);
                    }
                    break;
                default:
                    Logger.Warning("Unknown request type: " + request.requestType);
                    break;
            }
        }
        if (didProcess) {
            idleCycles = 0;
        } else {
            idleCycles++;
        }

        if (idleCycles > maxIdleCycles) { 
            const idleTime = maxIdleCycles * refreshDelay / 1000 / 60;
            Logger.Warning("Service is idling for " + idleTime.toFixed(2) + "min, shutting down! Run script again to continue...");
            break;
        }

        if (changedStatus) {
            wkit.SaveToRaw(apiFilename, JSON.stringify({requests: requests}, null, 4));
        }

        wkit.Sleep(refreshDelay);
    }
}

main();