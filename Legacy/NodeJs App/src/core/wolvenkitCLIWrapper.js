const { exec } = require('child_process');
const { Log } = require('./logger');
const { getSettings } = require('./loadSettings');

async function getSettings() {
    try {
        return await loadSettings();
    } catch (error) {
        Log.error('Failed to load settings in WolvenkitCLIWrapper: ' + error.message);
        return null;
    }
}


async function testArchiveInternal() {
    const settings = await getSettings();
    const gamePath = settings.gamePath;
    exec('cp77tools archive ' + gamePath, (error, stdout, stderr) => {
        if (error) {
            Log.error('Error archiving: ' + stderr);
        }
        return stdout;
    });
}

class WolvenkitCLIWrapper {
    async testArchive() {
        return await testArchiveInternal();
    }
}

const wkitCLI = new WolvenkitCLIWrapper();

module.exports = {
    wkitCLI
}
