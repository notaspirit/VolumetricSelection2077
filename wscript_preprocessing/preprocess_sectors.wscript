// Preprocesses all sectors found in the archiveContainsStreamingSectors.json file
// This extracts all relevent sector data and gets all actor meshes, bounding boxes and positions

// For testing
const testMode = false;

// Imports 
import * as Logger from 'Logger.wscript';
import * as TypeHelper from 'TypeHelper.wscript';

// Global variables
// See sectorExample.txt for template of data
let actorMeshes = [];

let sphereData = null;
let capsuleData = null;

let failedSectors = [];

let batchSize = 1000;
let defaultSettings = {batchSize: batchSize, totalBatches: 0, lastBatch: 0};
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
function getCleanedActorData(actors) {
    let cleanedActors = [];
    for (let actor of actors) {
        // Logger.Info('Actor:');
        let pos = {x: actor.Position.x, y: actor.Position.y, z: actor.Position.z};
        let quat = {i: actor.Orientation.i, j: actor.Orientation.j, k: actor.Orientation.k, r: actor.Orientation.r};
        let scale = {x: actor.Scale.x, y: actor.Scale.y, z: actor.Scale.z};
        let transform = {pos: pos, quat: quat, scale: scale};
        let shapes = [];
        for (let shape of actor.Shapes) {
            // Logger.Info('Shape:');
            // Logger.Info(shape.ShapeType);
            let shapeType = shape.ShapeType;
            let shapeLocal = [];
            // Possible actor shape types
            //     "TriangleMesh",
            //     "Capsule",
            //     "ConvexMesh",
            //     "Box",
            //     "Sphere"
            if (shapeType.includes("Mesh")) {
                // Get mesh from geometry cache hash here
                break;
            }
            if (shapeType.includes("Box")) {
                // Get bounding box here from size 
                break;
            }
            if (shapeType.includes("Sphere")) {
                // Get sphere here
                // Logger.Success('Sphere found');
                if (sphereData == null) {
                    sphereData = shape;
                }
                break;
            }
            if (shapeType.includes("Capsule")) {
                // Logger.Success('Capsule found');
                let shapeSize = shape.Size;
                // Logger.Info(shapeSize);
                if (shapeSize.Z == 0 && (shapeSize.X != 0 || shapeSize.Y != 0)) {
                    affectsOnlyXY++;
                }
                // Get capsule here from size
                if (capsuleData == null) {
                    capsuleData = shape;
                }
                break;
            }
        }
        cleanedActors.push({transform: transform, shapes: shapes});
    }
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
        let rawActors = nodeInstance["Data"]["compiledData"]["Data"]["Actors"];
        nodeInfo["actors"] = getCleanedActorData(rawActors);
    } catch (error) {
        nodeInfo["actors"] = null;
    }
    return nodeInfo;
}

function processBatch(batchJson) {  
    let matchingNodes = [];
    // Processes a single sector for testing
    if (testMode === true) {
        // let testSector = 'base\\worlds\\03_night_city\\_compiled\\default\\exterior_0_-34_0_0.streamingsector';
        let testSector = 'base\\worlds\\03_night_city\\_compiled\\default\\exterior_0_18_0_1.streamingsector';
        let testSectorGameFile = wkit.GetFileFromArchive(testSector, OpenAs.GameFile);
        let testSectorData = TypeHelper.JsonParse(wkit.GameFileToJson(testSectorGameFile));
        let testNodes = testSectorData["Data"]["RootChunk"]["nodes"];
        let testNodeData = testSectorData["Data"]["RootChunk"]["nodeData"]["Data"];


        for (let nodeIndex in testNodes) {
            if (testNodes[nodeIndex].Data.$type.includes("Collision")) {
                matchingNodes.push(getNodeInfo(testNodes[nodeIndex], nodeIndex));
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
                let sectorGameFile = wkit.GetFileFromArchive(sectorName, OpenAs.GameFile);
                let sectorData = TypeHelper.JsonParse(wkit.GameFileToJson(sectorGameFile));
                const nodeData = sectorData["Data"]["RootChunk"]["nodeData"]["Data"];
                const nodes = sectorData["Data"]["RootChunk"]["nodes"]
                for (let nodeIndex in nodes) {
                    matchingNodes.push(getNodeInfo(nodes[nodeIndex], nodeIndex));
                }
                // Logger.Info(matchingNodes);
            }
            Logger.Info('Possible actor shape types:');
            Logger.Info(possibleActorShapeTypes);
    }
    Logger.Info('Sphere data:');
    Logger.Info(sphereData);
    Logger.Info('Capsule data:');
    Logger.Info(capsuleData);
    return matchingNodes;
}



// Main Logic
function main() {
    let settings = null;
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
Logger.Info(`Affects only XY: ${affectsOnlyXY}`);