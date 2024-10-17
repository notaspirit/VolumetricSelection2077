const { ipcRenderer } = require('electron');

document.addEventListener('DOMContentLoaded', () => {
  document.getElementById('open-settings-btn').addEventListener('click', () => {
    ipcRenderer.send('open-settings-window');
  });
});