// Preprocesses all sectors found in the archiveContainsStreamingSectors.json file
// This extracts all relevent sector data and gets all actor meshes, bounding boxes and positions

// For testing
const testMode = true;

// Imports 
import * as Logger from 'Logger.wscript';
import * as TypeHelper from 'TypeHelper.wscript';

// Global variables
// See sectorExample.txt for template of data
let failedSectors = [];
let actorMeshes = [];
let settings = null;

let batchSize = 100;
let defaultSettings = {batchSize: batchSize, totalBatches: 0, lastBatch: 0, actorMeshId: 0};
// ------------------------------------------------------------------------------------------------
// Functions

// Decodes the fixed point coordinate
function decodeFixedPoint(bits, fractionalBits) {
    const isNegative = bits < 0;
    
    // Convert to positive if negative for calculation
    const absBits = Math.abs(bits);
    
    const scalingFactor = Math.pow(2, fractionalBits);
    const fixedPointValue = absBits / scalingFactor;
    
    // Adjust the sign based on the original bits
    return isNegative ? -fixedPointValue : fixedPointValue;
}

// Extracts Mesh and Transform data from component List
function getMeshSetFromComponents(components) {
    let localMeshGroup = [];
    for (let component of components) {
        if (component.$type.includes("Mesh")) {
            let localTransform = component.localTransform;
            let posRAW = [];
            posRAW.push({x: localTransform.Position.x, y: localTransform.Position.y, z: localTransform.Position.z});
            let meshPath = component.mesh.DepotPath.value;

            let posX = decodeFixedPoint(posRAW[0].x.Bits, 16);
            let posY = decodeFixedPoint(posRAW[0].y.Bits, 16);
            let posZ = decodeFixedPoint(posRAW[0].z.Bits, 16);

            let qi = localTransform.Orientation.i;
            let qj = localTransform.Orientation.j;
            let qk = localTransform.Orientation.k;
            let qr = localTransform.Orientation.r;
            Logger.Success(`Found mesh path: ${meshPath}`);

            localMeshGroup.push({
                meshPath: meshPath,
                quat: { i: qi, j: qj, k: qk, r: qr }, // Use curly braces for objects
                pos: { x: posX, y: posY, z: posZ }    // Use curly braces for objects
            });
        }
    }
    return localMeshGroup;
}


// gets mesh path from ent path and appearance
function getMeshPath(entPath, appearanceInput) {
    let entGameFile = wkit.GetFileFromArchive(entPath, OpenAs.GameFile);
    let entData = TypeHelper.JsonParse(wkit.GameFileToJson(entGameFile));
    let meshGroup = [];
    if (appearanceInput === 'default' || appearanceInput === 'random') {
        // Here you can get the mesh directly from the ent under "components"
        let components = entData.Data.RootChunk.components;
        meshGroup.push(...getMeshSetFromComponents(components));
    } else {
        // Here you have to get the mesh from the .app file
        // IMPORTANT:
        // The appearance name in the .app file isn't the same as in the node, it can be found under "appearanceName" in the .ent
        Logger.Info("Entity Has complex appearance: " + entPath + " " + appearanceInput);
        let appearances = entData.Data.RootChunk.appearances;
        let shortAppearanceName = '';
        let appearanceResource = '';
        for (let appearance of appearances) {
            if (appearance.name == appearanceInput) {
                shortAppearanceName = appearance.appearanceName;
                appearanceResource = appearance.appearanceResource.DepotPath.value;
                break;
            }
        }
        Logger.Info(`Short appearance name: ${shortAppearanceName}`);
        Logger.Info(`Appearance resource: ${appearanceResource}`);
        let appearanceData = null;
        try {
            let appearanceGameFile = wkit.GetFileFromArchive(appearanceResource, OpenAs.GameFile);
            appearanceData = TypeHelper.JsonParse(wkit.GameFileToJson(appearanceGameFile));
        } catch (error) {
            Logger.Error(`Failed to get appearance data for ${entPath} ${appearanceInput}: ${error.message}`);
        }
        let appAppearances = appearanceData.Data.RootChunk.appearances;
        for (let appAppearance of appAppearances) {
            if (appAppearance.Data.name.value == shortAppearanceName) {
                Logger.Success(`Found appearance: ${appAppearance.Data.name.value}`);
                let components = appAppearance.Data.components;
                meshGroup.push(...getMeshSetFromComponents(components));
                break;
            }
        }
    }

    return meshGroup;
}
let possibleActorShapeTypes = [];

let affectsOnlyXY = 0;

// Raw actor data to cleaned and with mesh data
function getCleanedActorData(actors, sectorHash) {
    /*
    Actor scale is weird, not sure how I would apply it so it goes into the database too,
    even if I could reduce db size, but I do not want to have to recompile all of the sector data if I am wrong.
    Especially spheres are weird, since if you scale it differently on x y and z you get an ellipsoid, not a sphere.
    Wolvenkit sector preview doesn't display them at all :kek:
    So yeah just play around with it in the postprocessing when you can actually compare it to the expected result.
    */
    let cleanedActors = [];
    for (let actor of actors) {
        // Position and scale need to be decoded first using decodeFixedPoint
        let pos = {x: decodeFixedPoint(actor.Position.x.Bits, 16), y: decodeFixedPoint(actor.Position.y.Bits, 16), z: decodeFixedPoint(actor.Position.z.Bits, 16)};
        let quat = {i: actor.Orientation.i, j: actor.Orientation.j, k: actor.Orientation.k, r: actor.Orientation.r};
        let scale = {x: actor.Scale["X"], y: actor.Scale["Y"], z: actor.Scale["Z"]};
        let transform = {pos: pos, quat: quat, scale: scale};
        let shapes = [];
        let actorIndex = 0;
        for (let shape of actor.Shapes) {
            let shapeType = shape.ShapeType;
            // Possible actor shape types
            //     "TriangleMesh",
            //     "Capsule",
            //     "ConvexMesh",
            //     "Box",
            //     "Sphere"
            if (shapeType.includes("Mesh")) {
                // Get mesh from geometry cache hash here
                let currentActorMeshId = settings.actorMeshId;
                let meshDataRAW = wkit.ExportGeometryCacheEntry(sectorHash, shape.Hash);
                let meshData = TypeHelper.JsonParse(meshDataRAW);
                let meshVertices = [];
                for (let vertex of meshData["Vertices"]) {
                    meshVertices.push({x: vertex["X"], y: vertex["Y"], z: vertex["Z"]});
                }
                let meshTriangles = meshData["Triangles"];
                let meshBoundingBoxMinRAW = meshData["AABB"]["Minimum"];
                let meshBoundingBoxMin = {x: meshBoundingBoxMinRAW["X"], y: meshBoundingBoxMinRAW["Y"], z: meshBoundingBoxMinRAW["Z"]};
                let meshBoundingBoxMaxRAW = meshData["AABB"]["Maximum"];
                let meshBoundingBoxMax = {x: meshBoundingBoxMaxRAW["X"], y: meshBoundingBoxMaxRAW["Y"], z: meshBoundingBoxMaxRAW["Z"]};
                let meshBoundingBox = {min: meshBoundingBoxMin, max: meshBoundingBoxMax};
                let shapePosition = {x: shape["Position"]["X"], y: shape["Position"]["Y"], z: shape["Position"]["Z"]};
                let shapeRotation = {i: shape.Rotation.i, j: shape.Rotation.j, k: shape.Rotation.k, r: shape.Rotation.r};
                let shapeTransform = {pos: shapePosition, quat: shapeRotation};
                settings.actorMeshId++;
                actorMeshes.push({
                    id: currentActorMeshId,
                    vertices: meshVertices,
                    triangles: meshTriangles,
                    boundingBox: meshBoundingBox
                });
                shapes.push({
                    type: shapeType,
                    MeshId: currentActorMeshId,
                    transform: shapeTransform
                });
                actorIndex++;
                continue;
            }
            if (shapeType.includes("Box")) {
                // Get bounding box here from size 
                /*
                Box might look weird as in the same shape appears in lots of actors,
                but that seems to be intended since they are in different actors which have their own modifiers.
                */
                let shapePos = {x: shape.Position["X"], y: shape.Position["Y"], z: shape.Position["Z"]};
                let shapeSize = {x: shape.Size["X"], y: shape.Size["Y"], z: shape.Size["Z"]};
                let shapeQuat = {i: shape.Rotation.i, j: shape.Rotation.j, k: shape.Rotation.k, r: shape.Rotation.r};
                shapes.push({
                    type: shapeType,
                    transform: {pos: shapePos, quat: shapeQuat, size: shapeSize},
                });
                actorIndex++;
                continue;
            }
            if (shapeType.includes("Sphere")) {
                // Get sphere here
                let shapePos = {x: shape.Position["X"], y: shape.Position["Y"], z: shape.Position["Z"]};
                let shapeRadius = shape.Size["X"];
                let shapeQuat = {i: shape.Rotation.i, j: shape.Rotation.j, k: shape.Rotation.k, r: shape.Rotation.r};
                shapes.push({
                    type: shapeType,
                    transform: {pos: shapePos, quat: shapeQuat, radius: shapeRadius},
                });
                actorIndex++;
                continue;
            }
            if (shapeType.includes("Capsule")) {
                // Get capsule here from size
                let shapePos = {x: shape.Position["X"], y: shape.Position["Y"], z: shape.Position["Z"]};
                let shapeSize = {r: shape.Size["X"], h: shape.Size["Y"]};
                let shapeQuat = {i: shape.Rotation.i, j: shape.Rotation.j, k: shape.Rotation.k, r: shape.Rotation.r};
                shapes.push({
                    type: shapeType,
                    transform: {pos: shapePos, quat: shapeQuat, size: shapeSize},
                });
                actorIndex++;
                continue;
            }
            
        }
        cleanedActors.push({transform: transform, shapes: shapes});
    }
    // Logger.Info(cleanedActors);
    return cleanedActors;
}

// Gets all relevant node info out of "node"
function getNodeInfo(nodeInstance, nodeIndex) {
    // Logger.Info(`Getting Node Info for ${nodeIndex}`);
    let nodeInfo = {}; 
    nodeInfo["index"] = nodeIndex;
    nodeInfo["type"] = nodeInstance["Data"]["$type"];
    // Getting the mesh only path works and is done
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
    // Getting the meshes from an entity template works and is done
    try {
        let entPath = '';
        let entAppearance = '';
        const depoPathEntJS = nodeInstance["Data"]["entityTemplate"]["DepotPath"];
        const depoPathAppearanceJS = nodeInstance["Data"]["appearanceName"];
        for (let key in depoPathEntJS) {
            if (key.includes("value")) {
                entPath = depoPathEntJS[key];
            }
        }
        for (let key in depoPathAppearanceJS) {
            if (key.includes("value")) {
                entAppearance = depoPathAppearanceJS[key];
            }
        }
        if (entPath !== '' && entAppearance !== '') {
            nodeInfo["mesh"] = getMeshPath(entPath, entAppearance);
        }
        if ((entPath !== '' && entAppearance === '') || (entPath === '' && entAppearance !== '')) {
            Logger.Error(`Node ${nodeIndex} has an invalid entity template or appearance: ${entPath} ${entAppearance}`);
        }
    } catch (error) {
        nodeInfo["entTemplate"] = null;
    }
    // Getting the actors is work in progress
    try {
        let sectorHash = nodeInstance["Data"]["sectorHash"];
        let rawActors = nodeInstance["Data"]["compiledData"]["Data"]["Actors"];
        nodeInfo["actors"] = getCleanedActorData(rawActors, sectorHash);
    } catch (error) {
        nodeInfo["actors"] = null;
    }
    return nodeInfo;
}

function processBatch(batchJson) {  
    let matchingNodes = [];
    let finalNodes = [];
    // Processes a single sector for testing
    if (testMode === true) {
        let testSector = 'base\\worlds\\03_night_city\\_compiled\\default\\exterior_0_-34_0_0.streamingsector';
        // let testSector = 'base\\worlds\\03_night_city\\_compiled\\default\\exterior_0_18_0_1.streamingsector';
        let testSectorGameFile = wkit.GetFileFromArchive(testSector, OpenAs.GameFile);
        let testSectorData = TypeHelper.JsonParse(wkit.GameFileToJson(testSectorGameFile));
        let testNodes = testSectorData["Data"]["RootChunk"]["nodes"];
        let testNodeData = testSectorData["Data"]["RootChunk"]["nodeData"]["Data"];


        for (let nodeIndex in testNodes) {
            matchingNodes.push(getNodeInfo(testNodes[nodeIndex], nodeIndex));
        }
        for (let nodeDataIndex in testNodeData) {
            for (let nodeIndex in testNodes) {
                if (testNodeData[nodeDataIndex]['NodeIndex'] == nodeIndex) {
                    let matchingNodesInstance = matchingNodes[nodeIndex];
                    Logger.Info("Got matching node in nodeData");
                    let nodeDataInstance = testNodeData[nodeDataIndex];
                    let nodeDataPos = {x: nodeDataInstance['Position']['X'], y: nodeDataInstance['Position']['Y'], z: nodeDataInstance['Position']['Z'], w: nodeDataInstance['Position']['W']};
                    let nodeDataQuat = {i: nodeDataInstance.Orientation.i, j: nodeDataInstance.Orientation.j, k: nodeDataInstance.Orientation.k, r: nodeDataInstance.Orientation.r};
                    let nodeDataScale = {x: nodeDataInstance["Scale"]["X"], y: nodeDataInstance["Scale"]["Y"], z: nodeDataInstance["Scale"]["Z"]};
                    let nodeDataTransform = {pos: nodeDataPos, quat: nodeDataQuat, scale: nodeDataScale};
                    matchingNodesInstance['nodeTransform'] = nodeDataTransform;
                    matchingNodesInstance['index'] = nodeDataIndex;
                    finalNodes.push(matchingNodesInstance);
                }
            }
        }
        wkit.SaveToResources('testMatchingNodes.json', JSON.stringify(matchingNodes, null, 2));
        Logger.Success('Saved testMatchingNodes.json');
    }
    // Processes a batch of sectors
    if (testMode === false) {
            // Processes each sector in the batch
            for (let sectorIndex in batchJson) {
                let sectorName = batchJson[sectorIndex].name;
                Logger.Info(`Processing sector: ${sectorName}`);
                try {
                    let sectorGameFile = wkit.GetFileFromArchive(sectorName, OpenAs.GameFile);
                    let sectorData = TypeHelper.JsonParse(wkit.GameFileToJson(sectorGameFile));
                } catch (error) {
                    Logger.Error(`Failed to get sector data for ${sectorName}: ${error.message}`);
                    failedSectors.push(sectorName);
                    continue;
                }
                const nodeData = sectorData["Data"]["RootChunk"]["nodeData"]["Data"];
                const nodes = sectorData["Data"]["RootChunk"]["nodes"]
                for (let nodeIndex in nodes) {
                    matchingNodes.push(getNodeInfo(nodes[nodeIndex], nodeIndex));
                }
                Logger.Info(`Total matching nodes: ${matchingNodes.length}`);
                for (let nodeDataIndex in nodeData) {
                    for (let nodeIndex in nodes) {
                        if (nodeData[nodeDataIndex]['NodeIndex'] == nodeIndex) {
                            let matchingNodesInstance = matchingNodes[nodeIndex];
                            Logger.Info("Got matching node in nodeData");
                            let nodeDataInstance = nodeData[nodeDataIndex];
                            let nodeDataPos = {x: nodeDataInstance['Position']['X'], y: nodeDataInstance['Position']['Y'], z: nodeDataInstance['Position']['Z'], w: nodeDataInstance['Position']['W']};
                            let nodeDataQuat = {i: nodeDataInstance.Orientation.i, j: nodeDataInstance.Orientation.j, k: nodeDataInstance.Orientation.k, r: nodeDataInstance.Orientation.r};
                            let nodeDataScale = {x: nodeDataInstance.Scale.x, y: nodeDataInstance.Scale.y, z: nodeDataInstance.Scale.z};
                            let nodeDataTransform = {pos: nodeDataPos, quat: nodeDataQuat, scale: nodeDataScale};
                            Logger.Info(nodeDataTransform);
                        }
                    }
                }
            }
    }
    return finalNodes;
}



// Main Logic
function main() {
    try {
        let settingsRaw = wkit.LoadFromResources('SPP/batchMetadata.json');
        settings = JSON.parse(settingsRaw);
        let settingsTest = settings.batchSize;
        Logger.Success(`SPP/batchMetadata.json exists`);
    } catch (error) {
        wkit.SaveToResources('SPP/batchMetadata.json', JSON.stringify(defaultSettings, null, 2));
        let settingsRaw = wkit.LoadFromResources('SPP/batchMetadata.json');
        settings = JSON.parse(settingsRaw);
        Logger.Success(`SPP/batchMetadata.json created`);
    }

    if (settings.batchSize !== batchSize) {
        Logger.Error(`Batch size in batchMetadata.json is not equal to the current batch size, adjust batch size or clear current progress`);
        return;
    }
    if (settings.totalBatches == 0) {
        Logger.Info(`No batches found, creating batches`);
        let archiveContainsStreamingSectors = [];
        let cleanedJson = [];
        try {
            const archiveContainsStreamingSectorsRAW = wkit.LoadFromResources('SPP/input/archiveContainsStreamingSectors.json');
            archiveContainsStreamingSectors = JSON.parse(archiveContainsStreamingSectorsRAW);
            if (archiveContainsStreamingSectors.length > 0) {
                Logger.Success('Successfully got archiveContainsStreamingSectors.json');
            } else {
                Logger.Error('archiveContainsStreamingSectors.json is empty');
                return;
            }
        } catch (error) {
            Logger.Error('Failed to get archiveContainsStreamingSectors.json from resources');
            return;
        }

        // removes the archiveName from json
        for (let jsonIndex in archiveContainsStreamingSectors) {
        for (let sectorIndex in archiveContainsStreamingSectors[jsonIndex].outputs) {
                cleanedJson.push(archiveContainsStreamingSectors[jsonIndex].outputs[sectorIndex]);
            }
        }
        Logger.Info(`Total Sectors: ${cleanedJson.length}`);
        // Splits the json into sets of 1000 sectors to process in batches
        let batchIndex = 1;
        for (let i = 0; i < cleanedJson.length; i += batchSize) {
            const batch = cleanedJson.slice(i, i + batchSize);
            wkit.SaveToResources(`SPP/batchedSectors/batch${batchIndex}.json`, JSON.stringify(batch, null, 2));
            batchIndex++;
        }
        settings.totalBatches = batchIndex;
        wkit.SaveToResources('SPP/batchMetadata.json', JSON.stringify(settings, null, 2));
        Logger.Success(`SPP/batchMetadata.json updated`);
        Logger.Info(`Total batches: ${settings.totalBatches}`);
        Logger.Info(`To start processing batches run script again`);
        return;
    } else if (settings.lastBatch < settings.totalBatches) {
        Logger.Info(`Batches already exist, starting next batch: ${settings.lastBatch + 1}/${settings.totalBatches}`);
        let batchJsonRAW = wkit.LoadFromResources(`SPP/batchedSectors/batch${settings.lastBatch + 1}.json`);
        let batchJson = JSON.parse(batchJsonRAW);
        let sectorMatchesOutput = processBatch(batchJson);
        settings.lastBatch++;
        wkit.SaveToResources('SPP/batchMetadata.json', JSON.stringify(settings, null, 2));
        Logger.Success(`SPP/batchMetadata.json updated`);
        wkit.SaveToResources(`SPP/output/actorMeshes${settings.lastBatch}.json`, JSON.stringify(actorMeshes, null, 2));
        Logger.Success(`SPP/output/actorMeshes${settings.lastBatch}.json saved`);
        wkit.SaveToResources(`SPP/output/batch${settings.lastBatch}.json`, JSON.stringify(sectorMatchesOutput, null, 2));
        Logger.Success(`SPP/output/batch${settings.lastBatch}.json saved`);
        Logger.Info('For better stability, clear wolvenkit logs');
        Logger.Info(`To continue processing batches run script again`);
        if (failedSectors.length > 0) {
            Logger.Info(`Failed to process ${failedSectors.length} sectors`);
            wkit.SaveToResources(`SPP/output/failedSectors${settings.lastBatch}.json`, JSON.stringify(failedSectors, null, 2));
            Logger.Success(`SPP/output/failedSectors${settings.lastBatch}.json saved`);
    }
    } else {
        Logger.Info(`All batches processed, merging results`);
        let finalOutput = [];
        for (let batchIndex in settings.output) {
            let batchOutputRAW = wkit.LoadFromResources(`SPP/output/batch${batchIndex}.json`);
            let batchOutput = JSON.parse(batchOutputRAW);
            finalOutput.push(...batchOutput);
        }
        wkit.SaveToResources('SPP/output/finalOutput.json', JSON.stringify(finalOutput, null, 2));
        Logger.Success('SPP/output/finalOutput.json saved');
    }
}

main();