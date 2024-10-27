const fs = require('fs');
const path = require('path');
const { app } = require('electron');
const { loadSettings } = require('./loadSettings');
const { ipcRenderer } = require('electron');
let logFilePath = '';

async function getSettings() {
    try {
        return await loadSettings();
    } catch (error) {
        console.log('Failed to load settings in logger: ' + error.message);
        return null;
    }
}

// Returns formatted date and time for log file name
function logFileName() {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
    const seconds = String(now.getSeconds()).padStart(2, '0');
    return `${year}${month}${day}_${hours}${minutes}${seconds}`;
}

// Initialize log file
function intializeLogFile() {
    const appDataPath = app.getPath('appData');
    const logDirPath = path.join(appDataPath, 'VolumetricSelection2077', 'logs');
    logFilePath = path.join(logDirPath, `${logFileName()}.log`);
    fs.mkdirSync(logDirPath, { recursive: true });
}

// Return logFilePath
function getLogFilePath() {
    return logFilePath;
}

// Returns formatted date and time for log entry
function logEntryDateTime() {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
    const seconds = String(now.getSeconds()).padStart(2, '0');
    return `[${year}-${month}-${day} ${hours}:${minutes}:${seconds}]`;
}

class LoggerMainOnly {
    async info(message, reqDetailed = false) {
        const settings = await getSettings();
        if (reqDetailed) {
            if (settings.detailedLogging) {
                const logEntry = `${logEntryDateTime()} [INFO   ] ${message}`;
                fs.appendFileSync(logFilePath, logEntry + '\n');
                return logEntry;
            }
        } else {
            const logEntry = `${logEntryDateTime()} [INFO   ] ${message}`;
            fs.appendFileSync(logFilePath, logEntry + '\n');
            return logEntry;
        }
    }
    async error(message, reqDetailed = false) {
        const settings = await getSettings();
        if (reqDetailed) {
            if (settings.detailedLogging) {
                const logEntry = `${logEntryDateTime()} [ERROR  ] ${message}`;
                fs.appendFileSync(logFilePath, logEntry + '\n');
                return logEntry;
            }
        } else {
            const logEntry = `${logEntryDateTime()} [ERROR  ] ${message}`;
            fs.appendFileSync(logFilePath, logEntry + '\n');
            return logEntry;
        }
    }
    async success(message, reqDetailed = false) {
        const settings = await getSettings();
        if (reqDetailed) {
            if (settings.detailedLogging) {
                const logEntry = `${logEntryDateTime()} [SUCCESS] ${message}`;
                fs.appendFileSync(logFilePath, logEntry + '\n');
                return logEntry;
            }
        } else {
            const logEntry = `${logEntryDateTime()} [SUCCESS] ${message}`;
            fs.appendFileSync(logFilePath, logEntry + '\n');
            return logEntry;
        }
    }
    async getLogFilePath() {
        return logFilePath;
    }
}

class LogAnywhere {
    info(message, reqDetailed = false) {
        try {
            if (typeof ipcRenderer !== 'undefined') {
                ipcRenderer.send('log-info', message, reqDetailed);
            } else {
                console.log('ipcRenderer is not available');
            }
        } catch (error) {
            console.log('Error sending log message:', error);
        }
    }

    error(message, reqDetailed = false) {
        try {
            if (typeof ipcRenderer !== 'undefined') {
                ipcRenderer.send('log-error', message, reqDetailed);
            } else {
                console.log('ipcRenderer is not available');
            }
        } catch (error) {
            console.log('Error sending log message:', error);
        }
    }

    success(message, reqDetailed = false) {
        try {
            if (typeof ipcRenderer !== 'undefined') {
                ipcRenderer.send('log-success', message, reqDetailed);
            } else {
                console.log('ipcRenderer is not available');
            }
        } catch (error) {
            console.log('Error sending log message:', error);
        }
    }

    getLogFilePath() {
        try {
            if (typeof ipcRenderer !== 'undefined') {
                return ipcRenderer.invoke('get-log-file-path');
            } else {
                console.log('ipcRenderer is not available');
            }
        } catch (error) {
            console.log('Error getting log file path:', error);
        }
    }
}

const LoggerMain = new LoggerMainOnly();
const Log = new LogAnywhere();

module.exports = { LoggerMain, Log, intializeLogFile, getLogFilePath };
