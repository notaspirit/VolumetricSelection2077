// @description
// This script is used to detect collisions between a selection box and a set of nodes
// It will then output the nodes that are colliding with the selection box
// First complete refactor of the CollisionDetectionOld.wscript
// @author spirit
// @version 0.0.1

// User Settings

// Input String from CET Mod
const InputString = '{"selectionBox": {"min": {"x":-1325.4757, "y":1232.7316, "z":111.0202, "w":1}, "max": {"x":-1357.3151, "y":1196.6312, "z":137.29634, "w":1}, "quat": {"i":0, "j":0, "k":0, "r":0}}, "sectors": ["base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-6_4_0_3.streamingsector", "base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-43_37_3_0.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-42_37_3_0.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-22_18_2_1.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-22_18_1_1.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-21_18_2_1.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-21_18_1_1.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-11_9_0_2.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\exterior_-22_16_1_0.streamingsector"]}';

// Search variable for node type, leave blank for all, supports partial string matching, seperation by spaces
// Note: This is case sensitive
const SelectVariable = "";


// Do not change code below this point -----------------------------------

// For Testing Purposes only -----------------------------------------------

// turns roll pitch yaw into a quaternion
function rpyToQuat(roll, pitch, yaw) {
    // Convert degrees to radians
    roll = roll * Math.PI / 180;
    pitch = pitch * Math.PI / 180;
    yaw = yaw * Math.PI / 180;

    const cy = Math.cos(yaw * 0.5);
    const sy = Math.sin(yaw * 0.5);
    const cp = Math.cos(pitch * 0.5);
    const sp = Math.sin(pitch * 0.5);
    const cr = Math.cos(roll * 0.5);
    const sr = Math.sin(roll * 0.5);

    return {
        i: sr * cp * cy - cr * sp * sy,
        j: cr * sp * cy + sr * cp * sy,
        k: cr * cp * sy - sr * sp * cy,
        r: cr * cp * cy + sr * sp * sy
    };
}
// Defines selection box rotation and converts it to a quaternion
let testRot = {roll: 0, pitch: 0, yaw: -4.9};
// let testRot = {roll: 0, pitch: 0, yaw: 0};
// let testRot = {roll: 0, pitch: 0, yaw: -180};
let testQuat = rpyToQuat(testRot.roll, testRot.pitch, testRot.yaw);
Logger.Info(testQuat)

let globalMeshBuilds = 0;
const maxMeshBuilds = 100;


// -------------------------------------------------------------------------




// Imports
import * as Logger from 'Logger.wscript';
import * as TypeHelper from 'TypeHelper.wscript';

// Validating input box min max
function validateInputBoxMinMax(minPoint, maxPoint) {
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
    const InputJson = JSON.parse(InputString);
    if (InputJson["selectionBox"] === undefined || InputJson["sectors"] === undefined) {
        Logger.Error("Invalid input string");
        return null;
    }
    let selectionBox = validateInputBoxMinMax(InputJson["selectionBox"]["min"], InputJson["selectionBox"]["max"]);
    InputJson["selectionBox"] = {"minPoint": selectionBox.minPoint, "maxPoint": selectionBox.maxPoint, "quat": InputJson["selectionBox"]["quat"]};

    return InputJson;
}
const InputJson = parseInputString();

// build bounding box from mesh
function buildBoundingBoxFromVertices(vertices) {
    let minPoint = { x: Infinity, y: Infinity, z: Infinity };
    let maxPoint = { x: -Infinity, y: -Infinity, z: -Infinity };
    for (let vertex of vertices) {
        if (vertex.x < minPoint.x) minPoint.x = vertex.x;
        if (vertex.y < minPoint.y) minPoint.y = vertex.y;
        if (vertex.z < minPoint.z) minPoint.z = vertex.z;
        if (vertex.x > maxPoint.x) maxPoint.x = vertex.x;
        if (vertex.y > maxPoint.y) maxPoint.y = vertex.y;
        if (vertex.z > maxPoint.z) maxPoint.z = vertex.z;
    }
    return new box(minPoint, maxPoint, mesh.quat);
}


// Todo: add triangles
// mesh class
class mesh {
    constructor(vertices, indices) {
        this.vertices = vertices;
        this.indices = indices;
        this.boundingBox = buildBoundingBoxFromVertices(this.vertices);
    }
}


// Todo: add triangles
// Defines a box with a min and max point and a quaternion rotation
class box {
    constructor(uncheckedMinPoint, uncheckedMaxPoint, quat) {
        let { minPoint, maxPoint } = validateInputBoxMinMax(uncheckedMinPoint, uncheckedMaxPoint);
        this.minPoint = minPoint;
        this.maxPoint = maxPoint;
        this.quat = quat;
        this.vertices = this.buildVertices(minPoint, maxPoint);
    }

    buildVertices(minPoint, maxPoint) {
        return [
            {x:minPoint.x, y:minPoint.y, z:minPoint.z},
            {x:minPoint.x, y:minPoint.y, z:maxPoint.z},
            {x:minPoint.x, y:maxPoint.y, z:minPoint.z},
            {x:minPoint.x, y:maxPoint.y, z:maxPoint.z},
            {x:maxPoint.x, y:minPoint.y, z:minPoint.z},
            {x:maxPoint.x, y:minPoint.y, z:maxPoint.z},
            {x:maxPoint.x, y:maxPoint.y, z:minPoint.z},
            {x:maxPoint.x, y:maxPoint.y, z:maxPoint.z}
        ];
    }
}


// builds selection box, uses inputjson and testquat (testquat will be replaced with the actual selection box rotation later)
const selectionBox = new box(InputJson["selectionBox"]["minPoint"], InputJson["selectionBox"]["maxPoint"], testQuat);
Logger.Info("Selection Box: ");
Logger.Info(selectionBox);

// Function to adjust a vertex to the selection box's local space
function adjustVertexToBoxCoordinateSpace(vertex, box) {
    // Translate the vertex by the inverse of the box's min point
    let translatedVertex = {
        x: vertex.x - box.minPoint.x,
        y: vertex.y - box.minPoint.y,
        z: vertex.z - box.minPoint.z
    };

    // Apply the inverse of the box's quaternion rotation
    let inverseQuat = {
        i: -box.quat.i,
        j: -box.quat.j,
        k: -box.quat.k,
        r: box.quat.r
    };

    // Rotate the translated vertex by the inverse quaternion
    let adjustedVertex = rotateVertexByQuaternion(translatedVertex, inverseQuat);

    return adjustedVertex;
}

// Helper function to rotate a vertex by a quaternion
function rotateVertexByQuaternion(vertex, q) {
    let x = vertex.x, y = vertex.y, z = vertex.z;
    let ix = q.r * x + q.j * z - q.k * y;
    let iy = q.r * y + q.k * x - q.i * z;
    let iz = q.r * z + q.i * y - q.j * x;
    let iw = -q.i * x - q.j * y - q.k * z;

    return {
        x: ix * q.r + iw * -q.i + iy * -q.k - iz * -q.j,
        y: iy * q.r + iw * -q.j + iz * -q.i - ix * -q.k,
        z: iz * q.r + iw * -q.k + ix * -q.j - iy * -q.i
    };
}
// Helper function to convert a vertex from local space to world space
function convertToWorldSpace(localVertex, box) {
    // Then translate it back
    return {
        x: localVertex.x + box.minPoint.x,
        y: localVertex.y + box.minPoint.y,
        z: localVertex.z + box.minPoint.z
    };
}

// Function to transform to local space and back to world space
function transformAndRevertVertex(vertex, box) {
    
    // Transform to local space
    let localVertex = adjustVertexToBoxCoordinateSpace(vertex, box);
    
    // Convert back to world space
    let worldVertex = convertToWorldSpace(localVertex, box);
    
    return {
        localSpace: localVertex,
        worldSpace: worldVertex
    };
}

// Checks if vertex is inside box, requires box to be in world space but rotated properly
function isVertexInsideBox(vertex, box) {
    const { minPoint, maxPoint } = box;
    if (vertex.x >= minPoint.x && vertex.x <= maxPoint.x) {
        if (vertex.y >= minPoint.y && vertex.y <= maxPoint.y) {
            if (vertex.z >= minPoint.z && vertex.z <= maxPoint.z) {
                return true;
            }
        }
    }
    return false;
}

// Gets all relevant node info out of "node"
function getNodeInfo(nodeInstance, nodeIndex) {
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

// Loads the mesh as JSON from the depot path
function loadMeshJson(depotPath) {
    //Logger.Info("Started Loading Mesh JSON");
    const meshGameFile = wkit.GetFileFromArchive(depotPath, OpenAs.GameFile);
    const meshJson = TypeHelper.JsonParse(wkit.GameFileToJson(meshGameFile));
    //Logger.Info("Finished Loading Mesh JSON");
    return meshJson;
}

// Generates the mesh from the mesh object
function buildMesh(depotPath) {
    // for testing to reduce time
    globalMeshBuilds++;
    if (globalMeshBuilds > maxMeshBuilds) {
        Logger.Info("Max Mesh Builds Reached");
        return;
    }
    const meshJson = loadMeshJson(depotPath);
    const encodedRenderBuffer = meshJson.Data.RootChunk.renderResourceBlob.Data.renderBuffer.Bytes;
    const renderChunkInfos = meshJson.Data.RootChunk.renderResourceBlob.Data.header.renderChunkInfos;

    let maxLod = 0;
    for (let lod of renderChunkInfos) {
        if (lod.lodMask > maxLod) {
            maxLod = lod.lodMask;
        }
    }



    let vertices = [];
    let indices = [];
    
    return new mesh(vertices, indices);
}


// generates the AXL file and saves it to resources
function buildAXLFileOutput(InputJson) {
    let outputString = "streaming:\n  sectors:\n";
    for (let item of InputJson) {
        outputString += `    - path: ${item.sector.name}\n      expectedNodes: ${item.sector.expectedNodes}\n      nodeDeletions:\n`;
        for (let node of item.sector.nodes) {
            outputString += `        - index: ${node.AXLindex}\n          type: ${node.type}\n`;
        }
    }
    wkit.SaveToResources("CollisionDetection2.xl", outputString);
    Logger.Success("CollisionDetection2.xl saved to resources!");
}



// Ouput Json format
/*
{
    "sector": {
        "name": "sectorPath",
        "expectedNodes": nodeData.length,
        "nodes": [{"AXLindex": nodeDataIndex, "type": nodeType, "actors": [actorIndex, actorIndex, ...]}]
    }
}
*/
let outputJson = [];

// Main Loop
for (const sectorPath of InputJson.sectors) {
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
                let nodePosition = {x: nodeData[nodeDataIndex]["Position"]["X"], y: nodeData[nodeDataIndex]["Position"]["Y"], z: nodeData[nodeDataIndex]["Position"]["Z"]};
                let adjustedPosition = transformAndRevertVertex(nodePosition, selectionBox);
                // Checks if the position vertex is inside the selection box
                if (isVertexInsideBox(adjustedPosition.worldSpace, selectionBox)) {
                    nodeDataIndexes.push({"AXLindex": nodeDataIndex, "type": nodeIndex["type"]});
                }
                // If the node has a mesh, build it
                if (nodeIndex["mesh"] !== null) {
                    if (globalMeshBuilds < maxMeshBuilds) {
//                        Logger.Info(`Building Mesh ${nodeIndex["mesh"]}`);
                        let mesh = buildMesh(nodeIndex["mesh"]);

                    }
                }
            }
        }
    }
    if (nodeDataIndexes.length > 0) {
        outputJson.push({"sector": {"name": sectorPath, "expectedNodes": nodeData.length, "nodes": nodeDataIndexes}});
    }
    // Logger.Info(matchingNodes);

}
// Logger.Info(outputJson);
buildAXLFileOutput(outputJson);



