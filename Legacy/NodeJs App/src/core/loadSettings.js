const { ipcRenderer, app } = require('electron');
const fs = require('fs');
const path = require('path');

let settings = {
  gamePath: '',
  detailedLogging: false,
  outputFormat: 'archivexl',
};

async function loadSettings() {
    let appPath;
    if (typeof ipcRenderer !== 'undefined') {   
        appPath = await ipcRenderer.invoke('getAppPath');
    } else {
        appPath = app.getPath('appData');
    }
    const settingsPath = path.join(appPath, 'VolumetricSelection2077', 'settings.json');
    if (fs.existsSync(settingsPath)) {
        return JSON.parse(fs.readFileSync(settingsPath, 'utf8'));
    }
    return settings;
}

module.exports = {
  loadSettings
};