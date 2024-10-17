const fs = require('fs');
const path = require('path');
const { app } = require('electron');

let logFilePath = '';

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
    info(message) {
        const logEntry = `${logEntryDateTime()} [INFO   ] ${message}`;
        fs.appendFileSync(logFilePath, logEntry + '\n');
        return logEntry;
    }
    error(message) {
        const logEntry = `${logEntryDateTime()} [ERROR  ] ${message}`;
        fs.appendFileSync(logFilePath, logEntry + '\n');
        return logEntry;
    }
    success(message) {
        const logEntry = `${logEntryDateTime()} [SUCCESS] ${message}`;
        fs.appendFileSync(logFilePath, logEntry + '\n');
        return logEntry;
    }
}

class LogAnywhere {
    info(message) {
        try {
            ipcRenderer.send('log-info', message);
        } catch (error) {
            console.log('Error sending log message:', error);
        }
    }
    error(message) {
        try {
            ipcRenderer.send('log-error', message);
        } catch (error) {
            console.log('Error sending log message:', error);
        }
    }
    success(message) {
        try {
            ipcRenderer.send('log-success', message);
        } catch (error) {
            console.log('Error sending log message:', error);
        }
        }
    getLogFilePath() {
        try {
            return ipcRenderer.invoke('get-log-file-path');
        } catch (error) {
            console.log('Error getting log file path:', error);
        }
    }
}

const LoggerMain = new LoggerMainOnly();
const Log = new LogAnywhere();

module.exports = { LoggerMain, Log, intializeLogFile, getLogFilePath };
