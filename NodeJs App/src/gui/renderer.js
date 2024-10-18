const { ipcRenderer } = require('electron');
const { Log } = require('../core/logger');
const { loadSettings } = require('../core/loadSettings');
const { validator } = require('../core/validator');
const fs = require('fs');
const path = require('path');

async function getSettings() {
  try {
      return await loadSettings();
  } catch (error) {
      Log.error('Failed to load settings in renderer: ' + error.message);
      return null;
  }
}

async function saveSettings(settings) {
  const appPath = await ipcRenderer.invoke('getAppPath');
  const settingsPath = path.join(appPath, 'VolumetricSelection2077', 'settings.json');
  fs.writeFileSync(settingsPath, JSON.stringify(settings, null, 2));
}

function appendToConsoleInternal(message) {
  const consoleElement = document.getElementById('console1');
  const messageElement = document.createElement('div');
    // Determine the message type and assign a class
    if (message.includes('[INFO')) {
      messageElement.classList.add('info-message');
    } else if (message.includes('[ERROR')) {
      messageElement.classList.add('error-message');
    } else if (message.includes('[SUCCESS')) {
      messageElement.classList.add('success-message');
  }
  messageElement.textContent = message;
  consoleElement.appendChild(messageElement);
  consoleElement.scrollTop = consoleElement.scrollHeight;
}

function cleanConsole() {
  const consoleElement = document.getElementById('console1');
  consoleElement.innerHTML = '';
}

async function checkSelection() {
  Log.info('Checking selection');
  if (await validator.validateGamePath() == false) {
    Log.error('Aborting operation');
    return;
  } 
  if (await validator.validateOutputFilename() == false) {
    Log.error('Aborting operation');
    return;
  }
  if (await validator.validateTransferString() == false) {
    Log.error('Aborting operation');
    return;
  }
  if (await validator.validateWolvenkitCLI() == false) {
    Log.error('Aborting operation');
    return;
  }
}

document.addEventListener('DOMContentLoaded', async () => {
  const settings = await getSettings();
  ipcRenderer.on('append-to-console', (event, message) => {
    appendToConsoleInternal(message);
  });
  document.getElementById('open-settings-btn').addEventListener('click', () => {
        ipcRenderer.send('open-settings');
  });
  document.getElementById('clear-console-btn').addEventListener('click', () => {
    cleanConsole();
  });
  document.getElementById('merge-files-btn').addEventListener('click', () => {
    Log.info('Merge files feature not implemented yet', true);
  });
  document.getElementById('cache-all-btn').addEventListener('click', () => {
    Log.info('Cache all feature not implemented yet', true);
  });
  document.getElementById('output-filename').value = settings.outputFilename;
  document.getElementById('output-filename').addEventListener('input', async (event) => {
    const settingslocal = await getSettings();
    settingslocal.outputFilename = event.target.value;
    Log.info('Output filename changed to: ' + settingslocal.outputFilename, true);
    saveSettings(settingslocal);
  });
  document.getElementById('transfer-string').value = settings.transferString;
  document.getElementById('transfer-string').addEventListener('input', async (event) => {
    const settingslocal = await getSettings();
    settingslocal.transferString = event.target.value;
    Log.info('Transfer string changed to: ' + settingslocal.transferString, true);
    saveSettings(settingslocal);
  });
  document.getElementById('output-format').value = settings.outputFormat;
  document.getElementById('output-format').addEventListener('change', async (event) => {
    const settingslocal = await getSettings();
    settingslocal.outputFormat = event.target.value;
    Log.info('Output format changed to: ' + settingslocal.outputFormat, true);
    saveSettings(settingslocal);
  });
  const checkSelectionButton = document.getElementById('check-selection-btn');
  const settingsButton = document.getElementById('open-settings-btn');
  checkSelectionButton.addEventListener('click', () => {
    checkSelectionButton.classList.add('loading');
    settingsButton.classList.add('disabled');
    Log.info('Checking selection button clicked', true);
    performAsyncOperation().then(() => {
      checkSelectionButton.classList.remove('loading');
      settingsButton.classList.remove('disabled');
    });
  });

  async function performAsyncOperation() {
    await checkSelection();
  }
});
