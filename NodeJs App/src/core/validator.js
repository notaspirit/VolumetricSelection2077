const { Log } = require('./logger');
const { loadSettings } = require('./loadSettings');
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
    if (settings.detailedLogging) {
        Log.info('Validating game path: ' + gamePath);
    }
    let archiveContentPath = path.join(gamePath, 'archive', 'pc', 'content');
    let archiveEp1Path = path.join(gamePath, 'archive', 'pc', 'ep1');

    if (settings.detailedLogging) {
        Log.info('Checking if ' + archiveContentPath + ' and ' + archiveEp1Path + ' exist');
    }

    if (fs.existsSync(archiveContentPath) && fs.existsSync(archiveEp1Path)) {
        Log.success('Game path is valid');
        return true;
    }

    Log.error('Game path is invalid');
    return false;
}

class ValidatorInterface {
    async validateGamePath() {
        return await validateGamePathInternal();
    }
}

const validator = new ValidatorInterface();
module.exports = {
    validator
}
