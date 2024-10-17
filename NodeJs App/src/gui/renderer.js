const { ipcRenderer } = require('electron');
const { Log } = require('../core/logger');
const { loadSettings } = require('./settings');

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
document.addEventListener('DOMContentLoaded', () => {
  ipcRenderer.on('append-to-console', (event, message) => {
    appendToConsoleInternal(message);
  });
  document.getElementById('open-settings-btn').addEventListener('click', () => {
        ipcRenderer.send('open-settings');
  });
  const checkSelectionButton = document.getElementById('check-selection-btn');
  checkSelectionButton.addEventListener('click', () => {
    checkSelectionButton.classList.add('loading');
    performAsyncOperation().then(() => {
      checkSelectionButton.classList.remove('loading');
    });
  });
  function performAsyncOperation() {
    return new Promise((resolve) => {
      setTimeout(() => {
        resolve();
      }, 2000);
    });
  }
});
