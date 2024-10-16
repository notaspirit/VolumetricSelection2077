import * as THREE from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';

function loadGLBMesh(glbPath) {
    const loader = new GLTFLoader();
    let mesh = null;

    // Synchronous load (if supported in your environment)
    const gltf = loader.parseSync(wkit.LoadFile(glbPath));

    gltf.scene.traverse((child) => {
        if (child.isMesh && !mesh) {
            mesh = child;
        }
    });

    if (!mesh) {
        Logger.Error(`No mesh found in the GLB file: ${glbPath}`);
    }

    return mesh;
}

// In your main loop or node processing function
function processNode(nodeInfo) {
    if (nodeInfo.mesh) {
        const glbPath = meshPathToGLBFile(nodeInfo.mesh);
        const complexMesh = loadGLBMesh(glbPath);
        
        if (complexMesh) {
            const isIntersecting = checkBoxMeshIntersection(selectionBox, complexMesh);
            
            if (isIntersecting) {
                Logger.Info(`Intersection detected with node ${nodeInfo.index}`);
            }
        }
    }
}

// Your main loop remains synchronous
for (const sectorPath of InputJson.sectors) {
    // ... existing code ...

    for (let nodeDataIndex in nodeData) {
        for (let nodeIndex of matchingNodes) {
            if (nodeData[nodeDataIndex]["NodeIndex"] == nodeIndex["index"]) {
                if (nodeIndex["mesh"] !== null && globalMeshBuilds < maxMeshBuilds) {
                    processNode(nodeIndex);
                    globalMeshBuilds++;
                }
            }
        }
    }

    // ... rest of your code ...
}