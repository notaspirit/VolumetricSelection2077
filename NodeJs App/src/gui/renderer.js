const { ipcRenderer } = require('electron');
const fs = require('fs');
const { Log } = require('../core/logger');

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
  console.log('DOMContentLoaded');
  ipcRenderer.on('append-to-console', (event, message) => {
    appendToConsoleInternal(message);
  });
  document.getElementById('open-settings-btn').addEventListener('click', () => {
    console.log('Settings button clicked');
        Log.info('Settings button clicked');
        Log.error('Settings button clicked');
        Log.success('Settings button clicked');
  });
});
