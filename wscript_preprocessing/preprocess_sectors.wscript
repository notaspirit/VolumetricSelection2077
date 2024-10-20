// Preprocesses all sectors found in the archiveContainsStreamingSectors.json file
// This extracts all relevent sector data and gets all actor meshes, bounding boxes and positions

// For testing
const maxSectors = 50;
let sectorCount = 0;

// Imports 
import * as Logger from 'Logger.wscript';
import * as TypeHelper from 'TypeHelper.wscript';

/*
let archiveContainsStreamingSectors = [];

try {
    const archiveContainsStreamingSectorsRAW = wkit.LoadFromResources('archiveContainsStreamingSectors.json');
    archiveContainsStreamingSectors = JSON.parse(archiveContainsStreamingSectorsRAW);
    Logger.Success('Successfully got archiveContainsStreamingSectors.json');
    // Logger.Info('archiveContainsStreamingSectors.json: ' + JSON.stringify(archiveContainsStreamingSectors[0]));

} catch (error) {
    Logger.Error('Failed to get archiveContainsStreamingSectors.json: ' + error.message);
}

// removes the archiveName from json
let cleanedJson = [];
for (let jsonIndex in archiveContainsStreamingSectors) {
    for (let sectorIndex in archiveContainsStreamingSectors[jsonIndex].outputs) {
        cleanedJson.push(archiveContainsStreamingSectors[jsonIndex].outputs[sectorIndex]);
        // Logger.Info(archiveContainsStreamingSectors[jsonIndex].outputs[sectorIndex]);
    }
}
Logger.Info('Length of cleanedJson: ' + cleanedJson.length);
// Splits the json into sets of 1000 sectors to process in batches
const batchSize = 1000;
let bachedJson = [];
for (let i = 0; i < cleanedJson.length; i += batchSize) {
    const batch = cleanedJson.slice(i, i + batchSize);
    bachedJson.push(batch);
}
Logger.Info('Length of bachedJson: ' + bachedJson.length);

// saves the bachedJson to a file
function saveBachedJson(bachedJson, batchIndex) {
    wkit.SaveToResources('\\preprocessedSectors\\bachedJson' + batchIndex + '.json', JSON.stringify(bachedJson, null, 2));
}
*/
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
    if (appearanceInput === 'default') {
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


// Gets all relevant node info out of "node"
function getNodeInfo(nodeInstance, nodeIndex) {
    Logger.Info(`Getting Node Info for ${nodeIndex}`);
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
        Logger.Error(error.message);
        nodeInfo["entTemplate"] = null;
    }
    try {
        nodeInfo["actors"] = nodeInstance["Data"]["compiledData"]["Data"]["Actors"];
    } catch (error) {
        nodeInfo["actors"] = null;
    }
    return nodeInfo;
}

let testSector = 'base\\worlds\\03_night_city\\_compiled\\default\\exterior_0_-34_0_0.streamingsector';
let testSectorGameFile = wkit.GetFileFromArchive(testSector, OpenAs.GameFile);
let testSectorData = TypeHelper.JsonParse(wkit.GameFileToJson(testSectorGameFile));
let testNodes = testSectorData["Data"]["RootChunk"]["nodes"];
let testNodeData = testSectorData["Data"]["RootChunk"]["nodeData"]["Data"];

let matchingNodes = [];
for (let nodeIndex in testNodes) {
    if (testNodes[nodeIndex].Data.$type.includes("Entity")) {
        matchingNodes.push(getNodeInfo(testNodes[nodeIndex], nodeIndex));
    }
}

wkit.SaveToResources('testMatchingNodes.json', JSON.stringify(matchingNodes, null, 2));
Logger.Success('Saved testMatchingNodes.json');

/*
// Processes each batch of sectors
for (let batchIndex in bachedJson) {
    Logger.Info('Processing batch: ' + batchIndex);
    let batchJson = [];
    // Processes each sector in the batch
    for (let sectorIndex in bachedJson[batchIndex]) {
        if (sectorCount > maxSectors) {
            break;
        }
        sectorCount++;
        let sectorName = bachedJson[batchIndex][sectorIndex].name;
        Logger.Info(`Processing sector: ${sectorName}`);
        let sectorJson = [];
        let sectorGameFile = wkit.GetFileFromArchive(sectorName, OpenAs.GameFile);
        let sectorData = TypeHelper.JsonParse(wkit.GameFileToJson(sectorGameFile));
        const nodeData = sectorData["Data"]["RootChunk"]["nodeData"]["Data"];
        const nodes = sectorData["Data"]["RootChunk"]["nodes"];

        let matchingNodes = [];
        for (let nodeIndex in nodes) {
            matchingNodes.push(getNodeInfo(nodes[nodeIndex], nodeIndex));
        }
        Logger.Info(matchingNodes);
    }
}
*/