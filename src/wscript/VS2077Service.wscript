// @author spirt (sprt_)
// @version 1.0.0
// @description 
// This script acts as a service for VolumetricSelection2077 exposing wolvenkit functionality via a json file.
// This script will create files in your project when fullfilling requests, make sure to run it in a safe environment.
// Wolvenkit 8.15.1-nightly.2025-01-07 or newer is required.



// For Reference
// unique id is the unix epoch 
// outPath is empty on request, this service then fills that in.
const exampleJson = {
    "requests": {
        173624291245: {
            requestType: "json",
            fileName: "base/somefile/path.whatever",
            outPath: "base/somefile/path.json",
            isFulfilled: false,
            isProcessed: false,
            errorMessage: ""
        },
        17362424214: {
            requestType: "glb",
            fileName: "base/somefile/path.whatever",
            outPath: "base/somefile/path.glb",
            isFulfilled: false,
            isProcessed: false,
            errorMessage: ""
        },
        1736242249: {
            requestType: "hash",
            sectorHash: "somehash",
            actorHash: "somehash",
            outPath: "geometryHash/somefile/path.json",
            isFulfilled: false,
            isProcessed: false,
            errorMessage: ""
        },
        219340214: {
            requestType: "refreshSettings",
            isFulfilled: false,
            isProcessed: false,
            errorMessage: ""
        }
    }
};


// Settings

class Settings {
    constructor() {
        this.version = "1.0.0";
        this.refreshDelay = 1 * 1000;
        this.maxIdleCycles = 300;
        this.maxRequestVerificationAttempts = 60;
        this.apiFilename = "VS2077\\requests.json";
        this.debugLogging = false;
    }

    limitObject() {
        const selfObject = {
            version: this.version,
            refreshDelay: this.refreshDelay,
            maxIdleCycles: this.maxIdleCycles,
            maxRequestVerificationAttempts: this.maxRequestVerificationAttempts,
            debugLogging: this.debugLogging
        }
        return selfObject;
    }
}

let settings = new Settings();

// Validation Class
class fileToValidate {
    constructor(id) {
        this.id = id;
        this.attempts = 0;
    }
}

const exportSettings = {
    "Common": {},
    "MorphTarget": {
        "Binary": true
    },
    "Mesh": {
        "MeshExporter": "Default", // Default, Experimental, REDmod
        "ExportType": "MeshOnly", // MeshOnly, WithRig, Multimesh
        "LodFilter": false,
        "Binary": true,
        "WithMaterials": false,
        "ImageType": "png"
    }
}
// Imports
import * as Logger from "Logger.wscript";

// Global variables
let idleCycles = 0;
let filesToVerify = [];

// functions

function refreshSettings() {
    let apiSettings = getRequests();

    if (apiSettings.settings == null) {
        apiSettings.settings = settings.limitObject();
        debugLog("No settings found, using default settings", "info");
    }

    for (const key in settings.limitObject()) {
        if (apiSettings.settings[key] == null) {
            apiSettings.settings[key] = settings[key];
            debugLog("No " + key + " found, using default value", "info");
        } else {
            settings[key] = apiSettings.settings[key];
            debugLog("Found " + key + " with value " + settings[key], "info");
        }
    }

    apiSettings.settings.version = settings.version;

    wkit.SaveToRaw(settings.apiFilename, JSON.stringify(apiSettings, null, 4));
}

function debugLog(message, type) {
    if (settings.debugLogging) {
        switch (type) {
            case "info":
                Logger.Info(message);
                break;
            case "warning":
                Logger.Warning(message);
                break;
            case "error":
                Logger.Error(message);
                break;
            default:
                Logger.Debug(message);
                break;
        }
    }
}

// ---@param string path
// ---@param string newExptension
// ---@return string path
function replaceExtension(path, newExptension) {
    return path.replace(/\.[^/.]+$/, newExptension);
}

// ---@param string id
function queueFileForVerification(id) {
    filesToVerify.push(new fileToValidate(id));
}

function verifyOutputFiles(requests) {
    debugLog("Verifying output files", "info");
    let didProcess = false;
    for (var i = 0; i < filesToVerify.length; i++) {
        const requestID = filesToVerify[i].id;
        const file = requests[requestID];
        if (wkit.FileExistsInRaw(file.outPath)) {
            debugLog("Verified file: " + file.outPath + " setting request as fullfilled and removing from queue", "info");
            filesToVerify.splice(i, 1);
            file.isFulfilled = true;

        } else {
            Logger.Debug("Failed to verify file: " + file.outPath + (settings.maxRequestVerificationAttempts - filesToVerify[i].attempts) + " attempts remaining");
            filesToVerify[i].attempts++;
            if (filesToVerify[i].attempts > settings.maxRequestVerificationAttempts) {
                Logger.Error("Failed to verify file: " + file.outPath);
                filesToVerify.splice(i, 1);
                file.errorMessage = "Failed to verify output file existence";
            }
        }
        didProcess = true;
    }
    return {didProcess: didProcess, filesToVerify: filesToVerify, requests: requests};
}
// ---@return JsonArray
function getRequests() {
    if (wkit.FileExistsInRaw(settings.apiFilename)) {
        const file = wkit.LoadRawJsonFromProject(settings.apiFilename, "json");
        if (file == null || file == "") {
            Logger.Error("Error loading requests from " + settings.apiFilename);
            return { settings: null, requests: {} };
        }
        // @ts-ignore
        let fileJSON = JSON.parse(file);
        if (fileJSON.requests == null) {
            fileJSON.requests = {};
        }
        // @ts-ignore
        return fileJSON;
    } else {
        return { settings: null, requests: {} };
    }
}

// ---@param string filename
// ---@returns {success: boolean, error: string}
function uncookToJson(filename, outPath) {
    if (!wkit.FileExists(filename)) {
        return {success: false, error: filename + "does not exist"};
    }

    var file = wkit.GetFile(filename, OpenAs.Json);
    if (file == null) {
        return {success: false, error: "Failed to open" + filename + "as json"};
    }
    wkit.SaveToRaw(outPath, JSON.stringify(file));
    return {success: true, error: ""};
}

// ---@param string filename
// ---@returns {success: boolean, error: string}
function uncookToGlb(filename) {
    if (!wkit.FileExists(filename)) {
        return {success: false, error: filename + "does not exist"};
    }

    wkit.Extract(filename);

    wkit.ExportFiles([filename], exportSettings);

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

    refreshSettings();

    Logger.Info("Starting Wolvenkit As A Service Script for VolumetricSelection2077 with settings:");
    Logger.Info("Version: " + settings.version);
    Logger.Info("Refresh Delay: " + (settings.refreshDelay / 1000).toFixed() + "s");
    Logger.Info("Max Idle Cycles: " + settings.maxIdleCycles + " => " + (settings.maxIdleCycles * settings.refreshDelay / 1000 / 60).toFixed(2) + "min");
    Logger.Info("Max Request Verification Attempts: " + settings.maxRequestVerificationAttempts + " => " + (settings.maxRequestVerificationAttempts * settings.refreshDelay / 1000 / 60).toFixed(2) + "min");
    Logger.Info("Debug Logging: " + settings.debugLogging);
    if (settings.debugLogging) {
        Logger.Warning("Debug logging is enabled, this should not be used outside of debugging issues as Wolvenkit doesn't handle large amounts of logs well.");
    }

    Logger.Info("Service is running . . . ");
    while (true) {
        debugLog("Checking for requests, current idle cycles: " + idleCycles + " (" + (idleCycles * settings.refreshDelay / 1000 / 60).toFixed(2) + "min)", "info");

        let didProcess = false;
        let changedStatus = false;

        let apiConfig = getRequests();
        // @ts-ignore
        let requests = apiConfig.requests;

        debugLog("Found " + Object.keys(requests).length + " requests", "info");
        for (var i = 0; i < Object.keys(requests).length; i++) {
            //Logger.Info("Found " + requests[i] + " requests");
            let request = requests[Object.keys(requests)[i]];
            if (request.isFulfilled || request.isProcessed) {
                debugLog("Skipping request " + Object.keys(requests)[i] + " as it is already fulfilled or processed", "info");
                continue;
            }
            didProcess = true;
            request.isProcessed = true;

            switch (request.requestType) {
                case "json":
                    debugLog("Found " + JSON.stringify(request), "info");
                    request.outPath = replaceExtension(request.filename, ".json");
                    const resultJson = uncookToJson(request.filename, request.outPath);
                    if (resultJson.success) {
                        debugLog("No error while uncooking json, queueing for file verification", "info");
                        queueFileForVerification(Object.keys(requests)[i]);
                    } else {
                        Logger.Error("Failed to uncook json: " + resultJson.error);
                        request.errorMessage = resultJson.error;
                    }
                    break;
                case "glb":
                    debugLog("Found " + JSON.stringify(request), "info");
                    request.outPath  = replaceExtension(request.filename, ".glb");
                    const resultGlb = uncookToGlb(request.filename);
                    if (resultGlb.success) {
                        debugLog("No error while uncooking glb, queueing for file verification", "info");
                        queueFileForVerification(Object.keys(requests)[i]);
                    } else {
                        Logger.Error("Failed to uncook glb: " + resultGlb.error);
                        request.errorMessage = resultGlb.error;
                    }
                    break;
                case "hash":
                    debugLog("Found " + JSON.stringify(request), "info");
                    request.outPath  = "geometryCache\\" + request.sectorHash.toString() + "_" + request.actorHash.toString() + ".json";
                    const resultHash = uncookHash(request.sectorHash, request.actorHash, request.outPath);
                    if (resultHash.success) {
                        debugLog("No error while processing hash, queueing for file verification", "info");
                        queueFileForVerification(Object.keys(requests)[i]);
                    } else {
                        Logger.Error("Failed to uncook hash: " + resultHash.error);
                        request.errorMessage = resultHash.error;
                    }
                    break;
                case "refreshSettings":
                    debugLog("Found " + JSON.stringify(request), "info");
                    refreshSettings();
                    changedStatus = true;
                    request.isFulfilled = true;
                    request.isProcessed = true;
                    break;
                case "ping":
                    debugLog("Found " + JSON.stringify(request), "info");
                    request.isFulfilled = true;
                    request.isProcessed = true;
                    break;
                default:
                    Logger.Warning("Unknown request type: " + request.requestType);
                    request.errorMessage = "Unknown request type: " + request.requestType
                    break;
            }
        }


        const verifyOutputFilesResult = verifyOutputFiles(requests);
        if (verifyOutputFilesResult.didProcess) {
            didProcess = true;
            changedStatus = true;
            filesToVerify = verifyOutputFilesResult.filesToVerify;
            requests = verifyOutputFilesResult.requests;
        }

        if (didProcess) {
            idleCycles = 0;
        } else {
            idleCycles++;
        }

        if (idleCycles > settings.maxIdleCycles) { 
            const idleTime = settings.maxIdleCycles * settings.refreshDelay / 1000 / 60;
            Logger.Warning("Service is idling for " + idleTime.toFixed(2) + "min, shutting down! Run script again to continue...");
            break;
        }

        if (changedStatus) {
            debugLog("Saving changed requests to " + settings.apiFilename, "info");
            // @ts-ignore
            wkit.SaveToRaw(settings.apiFilename, JSON.stringify({settings: apiConfig.settings, requests: requests}, null, 4));
        }

        wkit.Sleep(settings.refreshDelay);
    }
}



main();