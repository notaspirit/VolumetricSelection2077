// Preprocesses all sectors found in the archiveContainsStreamingSectors.json file
// This extracts all relevent sector data and gets all actor meshes, bounding boxes and positions

// Imports 
import * as Logger from 'Logger.wscript';
import * as TypeHelper from 'TypeHelper.wscript';

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
// Processes each batch of sectors
for (let batchIndex in bachedJson) {
    Logger.Info('Processing batch: ' + batchIndex);
    let batchJson = [];
    // Processes each sector in the batch
    for (let sectorIndex in bachedJson[batchIndex]) {
        Logger.Info('Processing sector: ' + sectorIndex);
        let sectorJson = [];
        let sectorName = bachedJson[batchIndex][sectorIndex].name;
        let sectorGameFile = wkit.GetFileFromArchive(sectorName, OpenAs.GameFile);
        let sectorData = wkit.LoadFile(sectorGameFile);
        Logger.Info('Sector data: ' + sectorData);
    }
}
