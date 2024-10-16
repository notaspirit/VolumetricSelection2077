// This is a script to build meshes from mesh path
// this is only for development purposes, and will be merged into the main script

// Imports
import * as Logger from 'Logger.wscript';
import * as TypeHelper from 'TypeHelper.wscript';

// Loads a mesh json from a mesh path
function meshPathToGLBPath(meshPath) {
    let meshGameFile = wkit.GetFileFromArchive(meshPath, OpenAs.GameFile);
    wkit.ExportFiles(meshGameFile);

}

// Builds a mesh from a mesh json
function buildMesh(meshPath) {
    let meshJson = loadMeshJson(meshPath);

    return mesh;
}
