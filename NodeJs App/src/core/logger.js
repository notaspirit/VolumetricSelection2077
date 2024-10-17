const fs = require('fs');
const path = require('path');
const { app } = require('electron');

let logFilePath = '';

// returns formated date and time for log file name
function logFileName() {
    const now = new Date();
    // Get the current date components
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0'); // Months are zero-indexed
    const day = String(now.getDate()).padStart(2, '0');

    // Get the current time components
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
    const seconds = String(now.getSeconds()).padStart(2, '0');

    // Format time as YYYYMMDD_HHMMSS
    return `${year}${month}${day}_${hours}${minutes}${seconds}`;
}

//initialize log file 
function intializeLogFile() {
    const appDataPath = app.getPath('appData');
    const logDirPath = path.join(appDataPath, 'VolumetricSelection2077', 'logs');
    const logFilePath = path.join(logDirPath, `${logFileName()}.log`);
    // Ensure the log directory exists
    fs.mkdirSync(logDirPath, { recursive: true });

    // Optionally, clear the log file at startup
    fs.writeFileSync(logFilePath, ''); // Clear the file
}

// returns formated date and time for log entry
function logEntryDateTime() {
    const now = new Date();
    // Get the current date components
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0'); // Months are zero-indexed
    const day = String(now.getDate()).padStart(2, '0');

    // Get the current time components
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
    const seconds = String(now.getSeconds()).padStart(2, '0');

    // Format time as [YYYY-MM-DD HH:MM:SS]
    return `[${year}-${month}-${day} ${hours}:${minutes}:${seconds}]`;
}

// Default Log Message
function logMessage(message) {
  // Determine the log file path
  const appDataPath = app.getPath('appData');
  const logDirPath = path.join(appDataPath, 'VolumetricSelection2077', 'logs');
  const logFilePath = path.join(logDirPath, `${logFileName()}.log`);

  // Ensure the log directory exists
  fs.mkdirSync(logDirPath, { recursive: true });

  // Log message to a file
  fs.appendFileSync(logFilePath, `${logEntryDateTime()} ${message}\n`);
}

module.exports = { logMessage, intializeLogFile };
