const { ipcRenderer } = require('electron');

document.addEventListener('DOMContentLoaded', () => {
  const saveButton = document.getElementById('save-button');

  saveButton.addEventListener('click', () => {
    const settings = {
      // Collect your settings data here
      theme: 'dark',
      notifications: true
    };

    ipcRenderer.send('save-settings', settings);
  });
});
