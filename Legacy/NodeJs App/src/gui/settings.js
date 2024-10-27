const { ipcRenderer, remote } = require('electron');
const { Log } = require('../core/logger'); // Ensure this path is correct
const fs = require('fs');
const path = require('path');

let settings = {
  gamePath: '',
  detailedLogging: false,
  outputFormat: 'archivexl',
};

async function loadSettings() {
  const appPath = await ipcRenderer.invoke('getAppPath');
  const settingsPath = path.join(appPath, 'VolumetricSelection2077', 'settings.json');
  if (fs.existsSync(settingsPath)) {
    settings = JSON.parse(fs.readFileSync(settingsPath, 'utf8'));
  }
  return settings;
}

async function saveSettings() {
  const appPath = await ipcRenderer.invoke('getAppPath');
  const settingsPath = path.join(appPath, 'VolumetricSelection2077', 'settings.json');
  fs.writeFileSync(settingsPath, JSON.stringify(settings, null, 2));
}

document.addEventListener('DOMContentLoaded', async () => {
  Log.success('DOMContentLoaded event fired', true); // Log when DOMContentLoaded fires
  
  settings = await loadSettings();
  Log.info('Loaded settings: ' + JSON.stringify(settings), true);
  
  document.getElementById('game-path').value = settings.gamePath;
  document.getElementById('detailed-logging').checked = settings.detailedLogging;
});
window.addEventListener('beforeunload', () => {
  settings.gamePath = document.getElementById('game-path').value;
  settings.detailedLogging = document.getElementById('detailed-logging').checked;
  Log.info('Saving settings: ' + JSON.stringify(settings), true);
  saveSettings();
});

