const { Log } = require('./logger');
const { loadSettings } = require('./loadSettings');
const { ipcRenderer } = require('electron');
const fs = require('fs').promises;
const path = require('path');
const { exec } = require('child_process');
const util = require('util');
const execPromise = util.promisify(exec);


async function getSettings() {
    try {
        return await loadSettings();
    } catch (error) {
        Log.error('Failed to load settings in gameFileManager: ' + error.message);
        return null;
    }
}


async function getArchiveContentJSONInternal() {
    const appPath = await ipcRenderer.invoke('getAppPath');
    const settings = await getSettings();
    const gamePath = settings.gamePath;
    const archiveContentPath = path.join(gamePath, 'archive', 'pc', 'content');
    const archiveep1Path = path.join(gamePath, 'archive', 'pc', 'ep1');
    const outputPath = path.join(appPath, 'VolumetricSelection2077', 'output');
    let filePathsRaw = [];
    // Ensure the output directory exists
    try {
        await fs.mkdir(outputPath, { recursive: true });
    } catch (error) {
        Log.error('Failed to create output directory: ' + error.message);
        return [];
    }

    try {
        const files = await fs.readdir(archiveContentPath);
        for (const file of files) {
            const filePath = path.join(archiveContentPath, file);
            const fileStat = await fs.stat(filePath);
            if (fileStat.isFile()) {
                filePathsRaw.push(filePath);
            }
        }
    } catch (error) {
        Log.error('Failed to read archive content in gameFileManager: ' + error.message);
        return [];
    }
    try {
        const files = await fs.readdir(archiveep1Path);
        for (const file of files) {
            const filePath = path.join(archiveep1Path, file);
            const fileStat = await fs.stat(filePath);
            if (fileStat.isFile()) {
                filePathsRaw.push(filePath);
            }
        }
    } catch (error) {
        Log.error('Failed to read archive ep1 content in gameFileManager: ' + error.message);
    }
    let sectorCount = 0;
    let meshCount = 0;
    let archiveContainsStreamingSectors = [];
    let archiveContainsMeshes = [];
    const archiveKeywords = ['basegame_2_mainmenu.archive', 'basegame_3_nightcity.archive', 'ep1_1_nightcity.archive', 'ep1_2_gamedata.archive', 'basegame_1_engine.archive', 'basegame_3_nightcity.archive', 'basegame_3_nightcity_terrain.archive', 'basegame_4_appearance.archive','basegame_4_gamedata.archive','ep1_1_nightcity.archive','ep1_1_nightcity_terrain.archive','ep1_2_gamedata.archive'];
    for (const filePath of filePathsRaw) {
        Log.info('Processing file: ' + filePath);
        if (!archiveKeywords.some(keyword => filePath.includes(keyword))) {
            Log.info('Skipping file');
            continue;
        }
        try {
            const file = await execPromise('cp77tools archive ' + `"${filePath}" --diff`, { maxBuffer: 1024 * 1024 * 500 });
            let fileJson = JSON.parse(file.stdout);
            let outputsStreamingSectors = []
            let outputsMeshes = []
            for (const key in fileJson.Files) {
                if (fileJson.Files[key].Name.endsWith('.streamingsector') && (fileJson.Files[key].Name.includes('exterior') || fileJson.Files[key].Name.includes('interior'))) {
                    outputsStreamingSectors.push({name: fileJson.Files[key].Name, hash: fileJson.Files[key].Key});
                    sectorCount++;
                }
                if (fileJson.Files[key].Name.endsWith('.mesh')) {
                    outputsMeshes.push({name: fileJson.Files[key].Name, hash: fileJson.Files[key].Key});
                    meshCount++;
                }
            }
            if (outputsStreamingSectors.length > 0) {
                archiveContainsStreamingSectors.push({archiveFile:fileJson.Name, outputs: outputsStreamingSectors});
            }
            if (outputsMeshes.length > 0) {
                archiveContainsMeshes.push({archiveFile: fileJson.Name, outputs: outputsMeshes});
            }
            Log.success('Finished processing file');
        } catch (error) {
            Log.error('Failed to process file: ' + filePath + ' in gameFileManager: ' + error.message);
        }
    }
    try {
        await fs.writeFile(path.join(outputPath, 'archiveContainsStreamingSectors.json'), JSON.stringify(archiveContainsStreamingSectors, null, 2));
        await fs.writeFile(path.join(outputPath, 'archiveContainsMeshes.json'), JSON.stringify(archiveContainsMeshes, null, 2));
        Log.info('Saved archiveContainsStreamingSectors.json and archiveContainsMeshes.json', true);
        Log.info('Sector count: ' + sectorCount);
        Log.info('Mesh count: ' + meshCount);
    } catch (error) {
        Log.error('Failed to write JSON files: ' + error.message);
    }
}

async function outputToOnlyArchiveNameInternal() {
    const appPath = await ipcRenderer.invoke('getAppPath');
    const settings = await getSettings();
    const outputPath = path.join(appPath, 'VolumetricSelection2077', 'output');
    const archiveContainsStreamingSectors = JSON.parse(await fs.readFile(path.join(outputPath, 'archiveContainsStreamingSectors.json'), 'utf8'));
    const archiveContainsMeshes = JSON.parse(await fs.readFile(path.join(outputPath, 'archiveContainsMeshes.json'), 'utf8'));
    let sectorArchiveFiles = [];
    let meshArchiveFiles = [];
    Log.info('converting output to name only')
    for (const key in archiveContainsStreamingSectors) {
        sectorArchiveFiles.push(archiveContainsStreamingSectors[key].archiveFile);
    }
    for (const key in archiveContainsMeshes) {
        meshArchiveFiles.push(archiveContainsMeshes[key].archiveFile);
    }
    fs.writeFile(path.join(outputPath, 'sectorArchiveFiles.json'), JSON.stringify(sectorArchiveFiles, null, 2));
    fs.writeFile(path.join(outputPath, 'meshArchiveFiles.json'), JSON.stringify(meshArchiveFiles, null, 2));
    Log.success('Saved sectorArchiveFiles.json and meshArchiveFiles.json');
}
// Sector count: 37226
// Mesh count: 103752
// Will be fun to preprocess 140k files . . . 
class GameFileManager {
    async getArchiveContentJSON() {
        return await getArchiveContentJSONInternal();
    }
    async outputToOnlyArchiveName() {
        return await outputToOnlyArchiveNameInternal();
    }
}

const gfm = new GameFileManager();

module.exports = {
    gfm
}
