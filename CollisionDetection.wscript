// @description
// This script is used to detect collisions between a selection box and a set of nodes
// It will then output the nodes that are colliding with the selection box
// Now using THREEjs
// @author spirit
// @version 0.0.1

// User Settings ----------------------------------------------------------------

// Input String from CET Mod
const InputString = '{"selectionBox": {"min": {"x":-1325.4757, "y":1232.7316, "z":111.0202, "w":1}, "max": {"x":-1357.3151, "y":1196.6312, "z":137.29634, "w":1}, "quat": {"i":0, "j":0, "k":0, "r":0}}, "sectors": ["base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-6_4_0_3.streamingsector", "base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-43_37_3_0.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-42_37_3_0.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-22_18_2_1.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-22_18_1_1.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-21_18_2_1.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-21_18_1_1.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-11_9_0_2.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\exterior_-22_16_1_0.streamingsector"]}';

// Search variable for node type, leave blank for all, supports partial string matching, seperation by spaces
// Note: This is case sensitive
const SelectVariable = "";

// For Mesh Collision Detection
// Optional, if performance is an issue, however comes with less accuracy
// Add if applicable

// Cache Mesh Builds
const cacheMeshBuilds = true;
// If caching is enabled, the meshes will be cached in a cache file and reused for collision detection

// Clean Cache after every use
const cleanCache = false;
// If enabled, the cache will be cleaned after every use, otherwise cached meshes will be reused project wide

// Detailed Logging
const detailedLogging = false;

// Changes below this point are not recommended -----------------------------------

// Imports
import * as THREE from 'three.module.min.js';
import * as Logger from 'Logger.wscript';
import * as TypeHelper from 'TypeHelper.wscript';

// Global Variables
let meshCache = [];
const modName = "CollisionDetection";
let outputJson = [];

// Functions

// Validating input box min max
function validateInputBoxMinMax(minPoint, maxPoint) {
    if (detailedLogging) {
        Logger.Info("Validating Input Box Min Max");
    }
    if (minPoint.x > maxPoint.x) {
        [minPoint.x, maxPoint.x] = [maxPoint.x, minPoint.x];
    }
    if (minPoint.y > maxPoint.y) {
        [minPoint.y, maxPoint.y] = [maxPoint.y, minPoint.y];
    }
    if (minPoint.z > maxPoint.z) {
        [minPoint.z, maxPoint.z] = [maxPoint.z, minPoint.z];
    }
    return { minPoint, maxPoint };
}

// Parsing the input string into a json object and validating it
function parseInputString() {
    Logger.Info("Parsing Input String");
    const InputJson = JSON.parse(InputString);
    if (InputJson["selectionBox"] === undefined || InputJson["sectors"] === undefined) {
        Logger.Error("Invalid input string");
        return null;
    }
    let selectionBox = validateInputBoxMinMax(InputJson["selectionBox"]["min"], InputJson["selectionBox"]["max"]);
    InputJson["selectionBox"] = {"minPoint": selectionBox.minPoint, "maxPoint": selectionBox.maxPoint, "quat": InputJson["selectionBox"]["quat"]};

    return InputJson;
}

// Input as Json Object
const InputJson = parseInputString();

// Builds a box from min max and a quat
function buildBox(minPoint, maxPoint, quat) {
    // Calculate the size of the box
    const size = new THREE.Vector3(
        Math.abs(maxPoint.x - minPoint.x),
        Math.abs(maxPoint.y - minPoint.y),
        Math.abs(maxPoint.z - minPoint.z)
    );

    // Create a BoxGeometry
    const geometry = new THREE.BoxGeometry(size.x, size.y, size.z);

    // Shift the geometry so that its origin is at the minimum point
    geometry.translate(size.x / 2, size.y / 2, size.z / 2);

    // Create a mesh (using a basic material for visibility, can be changed or removed)
    const material = new THREE.MeshBasicMaterial({ color: 0x00ff00, wireframe: true });
    const box = new THREE.Mesh(geometry, material);

    // Set the position to the minimum point
    box.position.set(minPoint.x, minPoint.y, minPoint.z);

    // Apply rotation
    box.setRotationFromQuaternion(new THREE.Quaternion(quat.i, quat.j, quat.k, quat.r));

    return box;
}

// Builds the selection box from the input json
function buildSelectionBox() {
    Logger.Info("Building Selection Box");
    const selectionBox = InputJson.selectionBox;
    const boxMesh = buildBox(selectionBox.minPoint, selectionBox.maxPoint, selectionBox.quat);
    return boxMesh;
}

const selectionBox = buildSelectionBox();


// Gets all relevant node info out of "node"
function getNodeInfo(nodeInstance, nodeIndex) {
    if (detailedLogging) {
        Logger.Info(`Getting Node Info for ${nodeIndex}`);
    }
    let nodeInfo = {}; 
    nodeInfo["index"] = nodeIndex;
    nodeInfo["type"] = nodeInstance["Data"]["$type"];
    try {
        const depoPathMeshJS = nodeInstance["Data"]["mesh"]["DepotPath"];
        for (let key in depoPathMeshJS) {
            if (key.includes("value")) {
                nodeInfo["mesh"] = depoPathMeshJS[key];
            }
        }
    } catch (error) {
        nodeInfo["mesh"] = null;
    }
    try {
        const depoPathEntJS = nodeInstance["Data"]["entityTemplate"]["DepotPath"];
        for (let key in depoPathEntJS) {
            if (key.includes("value")) {
                nodeInfo["entTemplate"] = depoPathEntJS[key];
            }
        }
    } catch (error) {
        nodeInfo["entTemplate"] = null;
    }
    try {
        nodeInfo["actors"] = nodeInstance["Data"]["compiledData"]["Data"]["Actors"];
    } catch (error) {
        nodeInfo["actors"] = null;
    }
    return nodeInfo;
}

// generates the AXL file and saves it to resources
function buildAXLFileOutput(InputJson) {
    Logger.Info("Building AXL File Output");
    let outputString = "streaming:\n  sectors:\n";
    for (let item of InputJson) {
        outputString += `    - path: ${item.sector.name}\n      expectedNodes: ${item.sector.expectedNodes}\n      nodeDeletions:\n`;
        for (let node of item.sector.nodes) {
            outputString += `        - index: ${node.AXLindex}\n          type: ${node.type}\n`;
        }
    }
    wkit.SaveToResources(`${modName}.xl`, outputString);
    Logger.Success(`${modName}.xl saved to resources!`);
}

// MainLoop
Logger.Info("Starting Main Loop");
for (const sectorPath of InputJson.sectors) {
    Logger.Info(`Processing Sector: ${sectorPath}`);

    // Getting the sector file and parsing it into a json object
    const sectorGameFile = wkit.GetFileFromArchive(sectorPath, OpenAs.GameFile);
    const sectorJson = TypeHelper.JsonParse(wkit.GameFileToJson(sectorGameFile));

    // Getting the node data and nodes
    const nodeData = sectorJson["Data"]["RootChunk"]["nodeData"]["Data"];
    const nodes = sectorJson["Data"]["RootChunk"]["nodes"];

    // Finds all nodes that match the select variable in nodes and extracts relevant data
    let matchingNodes = [];
    for (let nodeIndex in nodes) {
        const nodeType = nodes[nodeIndex]["Data"]["$type"];
        for (let select of SelectVariable.split(" ")) {
            if (nodeType !== null && nodeType.includes(select)) {
                matchingNodes.push(getNodeInfo(nodes[nodeIndex], nodeIndex));
            }
        }
    }

    // Gets all NodeData indexes that match the node type and checks those nodes against the selection box
    let nodeDataIndexes = [];
    for (let nodeDataIndex in nodeData) {
        for (let nodeIndex of matchingNodes) {
            if (nodeData[nodeDataIndex]["NodeIndex"] == nodeIndex["index"]) {
                // Implement collision detection here
                }
            }
        }
    if (nodeDataIndexes.length > 0) {
        outputJson.push({"sector": {"name": sectorPath, "expectedNodes": nodeData.length, "nodes": nodeDataIndexes}});
    }   
}

// Saves the mesh cache to resources or resets it
if (cleanCache) {
    wkit.SaveToResources(`${modName}Cache.json`, "");
    Logger.Success(`${modName}Cache.json reset.`);
} else {
    wkit.SaveToResources(`${modName}Cache.json`, JSON.stringify(meshCache));
    Logger.Success(`${modName}Cache.json saved to resources.`);
}

buildAXLFileOutput(outputJson);





