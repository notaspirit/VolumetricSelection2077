const { ipcRenderer } = require('electron');
const { Log } = require('../core/logger');
const { loadSettings } = require('./settings');
const { validator } = require('../core/validator');

async function getSettings() {
  try {
      return await loadSettings();
  } catch (error) {
      Log.error('Failed to load settings: ' + error.message);
      return null;
  }
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
  await validator.validateGamePath();
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
  const checkSelectionButton = document.getElementById('check-selection-btn');
  const settingsButton = document.getElementById('open-settings-btn');
  checkSelectionButton.addEventListener('click', () => {
    checkSelectionButton.classList.add('loading');
    settingsButton.classList.add('disabled');
    if (settings.detailedLogging) {
      Log.info('Checking selection button clicked');
    }
    performAsyncOperation().then(() => {
      checkSelectionButton.classList.remove('loading');
      settingsButton.classList.remove('disabled');
    });
  });

  async function performAsyncOperation() {
    await checkSelection();
  }
});
