const { app, BrowserWindow, ipcMain, Menu, ipcRenderer } = require('electron');
const fs = require('fs');
const path = require('path');
const { LoggerMain, intializeLogFile, getLogFilePath } = require('../core/logger');

let mainWindow;

const createWindow = () => {
  mainWindow = new BrowserWindow({
    width: 800,
    height: 600,
    icon: path.join(__dirname, '../assets/redCube.ico'),
    color: '#fdfdfd',
    backgroundColor: '#171c26',
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false,
      enableRemoteModule: true // Enable remote module
    }
  });
  //mainWindow.setMenu(null);
  mainWindow.loadFile('src/gui/index.html');
};

const createSettingsWindow = () => {
  const settingsWin = new BrowserWindow({
    width: 600,
    height: 250,
    backgroundColor: '#171c26',
    parent: mainWindow,
    modal: true,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false
    }
  });

  settingsWin.setMenu(null);
  settingsWin.loadFile('src/gui/settings.html');
};

const saveSettings = (settings) => {
  const appDataPath = app.getPath('appData');
  const settingsPath = path.join(appDataPath, 'VolumetricSelection2077', 'settings.json');

  fs.mkdirSync(path.dirname(settingsPath), { recursive: true });
  fs.writeFileSync(settingsPath, JSON.stringify(settings, null, 2));
};

ipcMain.on('save-settings', (event, settings) => {
  saveSettings(settings);
});

ipcMain.handle('get-log-file-path', () => {
  return getLogFilePath();
});

const menuTemplate = [
  {
    label: 'Settings',
    click: () => {
      createSettingsWindow();
    }
  }
];

const menu = Menu.buildFromTemplate(menuTemplate);
//Menu.setApplicationMenu(menu);

app.whenReady().then(() => {
  intializeLogFile();
  LoggerMain.info('Application started');
  LoggerMain.info(getLogFilePath());

  if (getLogFilePath() !== '') {
    createWindow();
    // Notify the renderer process that the log file path is ready
    mainWindow.webContents.on('did-finish-load', () => {
      mainWindow.webContents.send('log-file-ready');
    });
  }
  //Menu.setApplicationMenu(menu);
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit();
});

// Logging Messages through ipcMain because some shit is retarded
ipcMain.on('log-info', async (event, message, reqDetailed) => {
  const logEntry = await LoggerMain.info(message, reqDetailed);
  if (typeof logEntry !== 'undefined') {
    mainWindow.webContents.send('append-to-console', logEntry);
  }
});

ipcMain.on('log-error', async (event, message, reqDetailed) => {
  const logEntry = await LoggerMain.error(message, reqDetailed);
  if (typeof logEntry !== 'undefined') {
    mainWindow.webContents.send('append-to-console', logEntry);
  }
});

ipcMain.on('log-success', async (event, message, reqDetailed) => {
  const logEntry = await LoggerMain.success(message, reqDetailed);
  if (typeof logEntry !== 'undefined') {
    mainWindow.webContents.send('append-to-console', logEntry);
  }
});

ipcMain.on('get-log-file-path', (event) => {
  const logFilePath = getLogFilePath();
  event.reply(logFilePath);
});

ipcMain.on('open-settings', (event) => {
  createSettingsWindow();
});

ipcMain.handle('getAppPath', () => {
  return app.getPath('appData');
});