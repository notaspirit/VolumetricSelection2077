const { Log } = require('./logger');
const { loadSettings } = require('./loadSettings');
const { exec } = require('child_process');
const fs = require('fs');
const path = require('path');

async function getSettings() {
    try {
        return await loadSettings();
    } catch (error) {
        Log.error('Failed to load settings in validator: ' + error.message);
        return null;
    }
}

async function saveSettings(settings) {
    const appPath = await ipcRenderer.invoke('getAppPath');
    const settingsPath = path.join(appPath, 'VolumetricSelection2077', 'settings.json');
    fs.writeFileSync(settingsPath, JSON.stringify(settings, null, 2));
}

async function validateGamePathInternal() {
    const settings = await getSettings();
    if (!settings) {
        return false;
    }

    const gamePath = settings.gamePath;
    if (!gamePath) {
        Log.error('Game path is not set in settings');
        return false;
    }
    Log.info('Validating game path: ' + gamePath, true);
    let archiveContentPath = path.join(gamePath, 'archive', 'pc', 'content');
    let archiveEp1Path = path.join(gamePath, 'archive', 'pc', 'ep1');

    Log.info('Checking if ' + archiveContentPath + ' and ' + archiveEp1Path + ' exist', true);

    if (fs.existsSync(archiveContentPath) && fs.existsSync(archiveEp1Path)) {
        Log.success('Game path is valid');
        return true;
    }

    Log.error('Game path is invalid');
    return false;
}

async function validateOutputFilenameInternal() {
    const settings = await getSettings();
    const outputFilename = settings.outputFilename;
    // Regular expression to check for invalid characters
    const invalidChars = /[<>:"/\\|?*\x00-\x1F]/g;
    // Reserved names on Windows
    const reservedNames = /^(con|prn|aux|nul|com[1-9]|lpt[1-9])$/i;
    // Check for invalid characters
    if (invalidChars.test(outputFilename)) {
        Log.error('Output filename contains invalid characters');
        return false;
    }
    // Check for reserved names
    if (reservedNames.test(outputFilename)) {
        Log.error('Output filename is a reserved name');
        return false;
    }
    // Check for empty or only spaces
    if (outputFilename.trim().length === 0) {
        Log.error('Output filename is empty');
        return false;
    }
    // Check for Spaces 
    if (outputFilename.includes(' ')) {
        Log.error('Output filename contains spaces');
        return false;
    }
    // Check for trailing spaces or dots
    if (/[\s.]$/.test(outputFilename)) {
        Log.error('Output filename ends with a space or dot');
        return false;
    }
    // Check for periods indicating an extension
    if (outputFilename.includes('.') && !outputFilename.startsWith('.') && !outputFilename.endsWith('.')) {
        Log.error('Output filename has a period indicating an extension');
        return false;
    }
    Log.success('Output filename is valid');
    return true;
}

function validateInputBoxMinMaxInternal(minPoint, maxPoint) {
    Log.info('Validating input box min max', true);
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

async function validateTransferStringInternal() {
    try {
        const settings = await getSettings();
        const transferString = settings.transferString;
        const json = JSON.parse(transferString);
        if (json.selectionBox && json.sectors) {
            Log.success('Transfer string has selectionBox and sectors', true);
            let {minPoint, maxPoint} = validateInputBoxMinMaxInternal(json.selectionBox.min, json.selectionBox.max);
            settings.transferString = JSON.stringify({selectionBox: {min: minPoint, max: maxPoint, quat: json.selectionBox.quat}, sectors: json.sectors});
            await saveSettings(settings);
            Log.success('Transfer string is valid');
            return true;
        }
    } catch (error) {
        Log.error('Transfer string is invalid: ' + error.message);
        return false;
    }
}

function validateWolvenkitCLIInternal() {
    Log.info('Validating Wolvenkit CLI', true);
    exec('cp77tools --version', (error, stdout, stderr) => {
        if (error) {
            Log.error('Wolvenkit CLI is not installed');
            return false;
        }
        Log.success('Wolvenkit CLI version: ' + stdout);
        return true;
    });
}
class ValidatorInterface {
    async validateGamePath() {
        return await validateGamePathInternal();
    }

    async validateOutputFilename() {
        return await validateOutputFilenameInternal();
    }

    async validateTransferString() {
        return await validateTransferStringInternal();
    }

    async validateWolvenkitCLI() {
        return await validateWolvenkitCLIInternal();
    }
}

const validator = new ValidatorInterface();
module.exports = {
    validator
}
