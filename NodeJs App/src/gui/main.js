const { app, BrowserWindow, ipcMain, Menu } = require('electron')

let mainWindow; // Declare a variable to hold the main window reference

const createWindow = () => {
  mainWindow = new BrowserWindow({
    width: 800,
    height: 600,
    color: '#fdfdfd',
    backgroundColor: '#171c26',
    webPreferences: {
      nodeIntegration: true, // Enable Node.js integration
      contextIsolation: false // Disable context isolation
    }
  })

  mainWindow.loadFile('src/gui/index.html')
}

const createSettingsWindow = () => {
  const settingsWin = new BrowserWindow({
    width: 400,
    height: 300,
    backgroundColor: '#171c26',
    parent: mainWindow, // Set the main window as the parent
    modal: true, // Make it a modal window
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false
    }
  })

  settingsWin.setMenu(null); // Remove the menu from the settings window

  settingsWin.loadFile('src/gui/settings.html')
}

const menuTemplate = [
  {
    label: 'Settings',
    click: () => {
      createSettingsWindow(); // Open the settings window
    }
  }
]

const menu = Menu.buildFromTemplate(menuTemplate);
Menu.setApplicationMenu(menu);

app.whenReady().then(() => {
  createWindow()
  Menu.setApplicationMenu(menu);
  // Optionally, you can open the settings window automatically for testing
  // createSettingsWindow()
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit()
})

ipcMain.on('open-settings-window', () => {
  createSettingsWindow()
})
