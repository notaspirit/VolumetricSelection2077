const Database = require('better-sqlite3');
const path = require('path');
const fs = require('fs');
const { ipcRenderer } = require('electron');

let db;

async function initializeSectorDatabaseLocal() {

    const appPath = await ipcRenderer.invoke('get-app-path');
    const dbPath = path.join(appPath, 'cache','sectorDatabase.db');
    db = new Database(dbPath);
    // Create the table if it doesn't exist
    db.prepare(`
        CREATE TABLE IF NOT EXISTS meshes (
        hash TEXT PRIMARY KEY,
        path TEXT,
        vertices BLOB,
        triangles BLOB
    )
    `).run();
    return db;
}

function insertLocal(hash, path, vertices, triangles) {
    const stmt = db.prepare('INSERT INTO meshes (hash, path, vertices, triangles) VALUES (?, ?, ?, ?)');
    return stmt.run(hash, path, JSON.stringify(vertices), JSON.stringify(triangles));
}

function getLocal(identifier = null) {
    if (identifier) {
        const stmt = db.prepare('SELECT * FROM meshes WHERE path = ? OR hash = ?');
        const row = stmt.get(identifier, identifier);
        if (row) {
            return {
                hash: row.hash,
                path: row.path,
                vertices: JSON.parse(row.vertices),
                triangles: JSON.parse(row.triangles)
            };
        }
        return null;
    } else {
        const stmt = db.prepare('SELECT * FROM meshes');
        const rows = stmt.all();
        return rows.map(row => ({
            hash: row.hash,
            path: row.path,
            vertices: JSON.parse(row.vertices),
            triangles: JSON.parse(row.triangles)
        }));
    }
}

class SectorDatabase {
    async initialize() {
        return await initializeSectorDatabaseLocal();
    }
    insert(hash, path, vertices, triangles) {
        return insertLocal(hash, path, vertices, triangles);
    }
    get(identifier = null) {
        return getLocal(identifier);
    }
}

const sectorDB = new SectorDatabase();
module.exports = sectorDB;
