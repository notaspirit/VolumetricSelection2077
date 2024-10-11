// @setting InputString:string:No
// @setting AxlFile:booL:true
// @setting Everything:booL:true
// @author spirit
// @version 0.0.0
// @description
// Main Processing Script for the CtrlADel project

// Imports
import * as Logger from 'Logger.wscript';
import * as TypeHelper from 'TypeHelper.wscript';
// Issue with undefined settings
// Logger.Info(settings.InputString);

let InputString = '{"selectionBox": {"min": [x:0, y:0, z:0, w:0], "max": [x:1, y:1, z:1, w:1], "quat": [x:0, y:0, z:0, w:0]}, "sectors": ["base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-6_4_0_3.streamingsector", "base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-43_37_3_0.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-42_37_3_0.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-22_18_2_1.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-22_18_1_1.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-21_18_2_1.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-21_18_1_1.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\interior_-11_9_0_2.streamingsector","base\\\\worlds\\\\03_night_city\\\\_compiled\\\\default\\\\exterior_-22_16_1_0.streamingsector"]}';

let InputJson = JSON.parse(InputString);

// Function to calculate the 3D vector connecting two points
function vectorBetweenPoints(pointA, pointB) {
    return [
        pointB.x - pointA.x,
        pointB.y - pointA.y,
        pointB.z - pointA.z
    ];
}

// Cube class to handle the cube's vertices and quantization
class Cube {
    constructor(minPoint, maxPoint, quant) {
        this.vertices = this.minMaxToVertices(minPoint, maxPoint);
        this.edges = this.calculateEdges(this.vertices);
        this.quant = quant;
    }

    minMaxToVertices(minPoint, maxPoint) {
        return [
            {x: minPoint.x, y: minPoint.y, z: minPoint.z, w: minPoint.w},
            {x: maxPoint.x, y: minPoint.y, z: minPoint.z, w: minPoint.w},
            {x: maxPoint.x, y: maxPoint.y, z: minPoint.z, w: minPoint.w},
            {x: minPoint.x, y: maxPoint.y, z: minPoint.z, w: minPoint.w},
            {x: minPoint.x, y: minPoint.y, z: maxPoint.z, w: minPoint.w},
            {x: maxPoint.x, y: minPoint.y, z: maxPoint.z, w: minPoint.w},
            {x: maxPoint.x, y: maxPoint.y, z: maxPoint.z, w: minPoint.w},
            {x: minPoint.x, y: maxPoint.y, z: maxPoint.z, w: minPoint.w}
        ];
    }

    calculateEdges(vertices) {
        return [
            {points: [vertices[0], vertices[1]], vector: vectorBetweenPoints(vertices[0], vertices[1])},
            {points: [vertices[1], vertices[2]], vector: vectorBetweenPoints(vertices[1], vertices[2])},
            {points: [vertices[2], vertices[3]], vector: vectorBetweenPoints(vertices[2], vertices[3])},
            {points: [vertices[3], vertices[0]], vector: vectorBetweenPoints(vertices[3], vertices[0])},
            {points: [vertices[4], vertices[5]], vector: vectorBetweenPoints(vertices[4], vertices[5])},
            {points: [vertices[5], vertices[6]], vector: vectorBetweenPoints(vertices[5], vertices[6])},
            {points: [vertices[6], vertices[7]], vector: vectorBetweenPoints(vertices[6], vertices[7])},
            {points: [vertices[7], vertices[4]], vector: vectorBetweenPoints(vertices[7], vertices[4])},
            {points: [vertices[0], vertices[4]], vector: vectorBetweenPoints(vertices[0], vertices[4])},
            {points: [vertices[1], vertices[5]], vector: vectorBetweenPoints(vertices[1], vertices[5])},
            {points: [vertices[2], vertices[6]], vector: vectorBetweenPoints(vertices[2], vertices[6])},
            {points: [vertices[3], vertices[7]], vector: vectorBetweenPoints(vertices[3], vertices[7])}
        ];
    }
}


let SelectionBox = new Cube(InputJson.selectionBox.min, InputJson.selectionBox.max, InputJson.selectionBox.quat);

// Main Loop
for (let sector of InputJson.sectors) {
    // Logger.Info(sector);
}
// Logger.Info(SelectionBox);
Logger.Info(InputJson.selectionBox.min);
Logger.Info(InputJson.selectionBox.max);
Logger.Info(InputJson.selectionBox.quat);
































/*
Will Requqire a section that gets the corner points and meshes from the game given the sector names
Base64 encoded data from mesh json
let vert_base64 = "PXYkPYTbyz0py3NAAACAPzKsPL/FFb+7t+4eQAAAgD89diQ9xRW/u7fuHkAAAIA/Mqw8v4Tbyz0py3NAAACAP2QuEcCE28s9KctzQAAAgD9kLhHAxRW/u7fuHkAAAIA/UOtCwITbyz0py3NAAACAP1DrQsDFFb+7t+4eQAAAgD8yrDy/xRW/uzR+Vb4AAIA/PXYkPcUVv7s0flW+AACAP1DrQsDFFb+7NH5VvgAAgD9kLhHAxRW/uzR+Vb4AAIA/T+tCwIC1TL4py3NAAACAP2suEcAinsG9t+4eQAAAgD9P60LAIp7BvbfuHkAAAIA/ay4RwIK1TL4py3NAAACAP0+sPL99tUy+KctzQAAAgD9NrDy/NJ7BvbfuHkAAAIA/FHYkPYW1TL4py3NAAACAPxR2JD05nsG9t+4eQAAAgD9rLhHAIp7BvTR+Vb4AAIA/T+tCwCKewb00flW+AACAPxR2JD05nsG9NH5VvgAAgD9NrDy/NJ7BvTR+Vb4AAIA/KXYkPWkjoj5Vio8+AACAP1J2JD3BXU6+EthzQAAAgD8pdiQ9aSOiPhLYc0AAAIA/UnYkPcFdTr5Vio8+AACAP1LrQsDCXU6+VYqPPgAAgD9O60LAZyOiPhLYc0AAAIA/UutCwMJdTr4S2HNAAACAP07rQsBnI6I+VYqPPgAAgD8=";

let indi_base64 = "AAABAAIAAwABAAAABAABAAMABQABAAQABgAFAAQABwAFAAYAAgAIAAkAAQAIAAIACgAFAAcACwAFAAoADAANAA4ADwANAAwAEAANAA8AEQANABAAEgARABAAEwARABIADgAUABUADQAUAA4AFgARABMAFwARABYAGAAZABoAGwAZABgAHAAdAB4AHwAdABwA";

// object coordinates vector4
let objectCoordinates = {x: 20, y: 32, z: 4, w: 1};

// object bounding box min max
//let objectBoundingBoxMin = {x: -3.04561281, y: -0.201529533, z: -0.208489239, w: 1};
//let objectBoundingBoxMax = {x: 0.0401519015, y: 0.316676408, z: 3.81006289, w: 1};
let relativeObjectBoundingBoxMin = {x: -1.0, y: -1.0, z: -1.0, w: 1};
let relativeObjectBoundingBoxMax = {x: 1.0, y: 1.0, z: 1.0, w: 1};
let boundingCubeQuant = {x: 0, y: 0, z: 0, w: 1};

// Function to convert a relative point to an absolute point
function relativeToAbsolute(relativePoint, referencePoint) {
    return [relativePoint.x + referencePoint.x, relativePoint.y + referencePoint.y, relativePoint.z + referencePoint.z];
}

// function to convert a relative quat into an absolute quat
function relativeToAbsoluteQuat(relativeQuat, referenceQuat) {
    return multiplyQuaternions(relativeQuat, referenceQuat);
}

let objectAbsoluteBoundingBoxMin = relativeToAbsolute(relativeObjectBoundingBoxMin, objectCoordinates);
let objectAbsoluteBoundingBoxMax = relativeToAbsolute(relativeObjectBoundingBoxMax, objectCoordinates);
let objectAbsoluteRotation = relativeToAbsoluteQuat(relativeObjectRotation, objectRotation);

// absolute selector box min max
let selectorBoxMin = {x: 0, y: 0, z: 0, w: 1};
let selectorBoxMax = {x: 1, y: 1, z: 1, w: 1};
let selectorBoxQuant = {x: -1, y: 0.3, z: 0.7, w: 1};


// Decoding functions
function decodeVertices(base64String) {
    // Decode the Base64 string for vertices
    let binaryDataVert = atob(base64String);

    // Create a Uint8Array from the binary string
    let uint8Array = new Uint8Array(binaryDataVert.length);
    for (let i = 0; i < binaryDataVert.length; i++) {
        uint8Array[i] = binaryDataVert.charCodeAt(i);
    }

    // Create a Float32Array from the Uint8Array
    let floatData = new Float32Array(uint8Array.buffer);

    // Grouping the floats into sets of 4 (X, Y, Z, W coordinates)
    let vertices = [];
    for (let i = 0; i < floatData.length; i += 4) {
        vertices.push([floatData[i], floatData[i + 1], floatData[i + 2], floatData[i + 3]]);
    }

    return vertices;
}

function decodeIndices(base64String) {
    // Decode the Base64 string for indices
    let binaryDataIndi = atob(base64String);

    // Create a Uint8Array from the binary string
    let uint8Array = new Uint8Array(binaryDataIndi.length);
    for (let i = 0; i < binaryDataIndi.length; i++) {
        uint8Array[i] = binaryDataIndi.charCodeAt(i);
    }

    // Create a Uint16Array from the Uint8Array
    let uint16Data = new Uint16Array(uint8Array.buffer);

    // Grouping the indices into sets of 3
    let indices = [];
    for (let i = 0; i < uint16Data.length; i += 3) {
        indices.push([uint16Data[i], uint16Data[i + 1], uint16Data[i + 2]]);
    }

    return indices;
}

// Decoded indices and vertices for a given mesh
let indices = decodeIndices(indi_base64);
let vertices = decodeVertices(vert_base64);

// Create the cubes
let selectorCube = new Cube(selectorBoxMin, selectorBoxMax, selectorBoxQuant);
let objectBoundingCubeOld = new Cube(objectBoundingBoxMin, objectBoundingBoxMax, boundingCubeQuant);

// Function to subtract two 3D vectors
function subtractVectors(v1, v2) {
    return [v1[0] - v2[0], v1[1] - v2[1], v2[2] - v2[2]];
}

// Function to multiply two quaternions
function multiplyQuaternions(q1, q2) {
    let x1 = q1[0], y1 = q1[1], z1 = q1[2], w1 = q1[3];
    let x2 = q2[0], y2 = q2[1], z2 = q2[2], w2 = q2[3];

    return [
        w1 * x2 + x1 * w2 + y1 * z2 - z1 * y2,
        w1 * y2 - x1 * z2 + y1 * w2 + z1 * x2,
        w1 * z2 + x1 * y2 - y1 * x2 + z1 * w2,
        w1 * w2 - x1 * x2 - y1 * y2 - z1 * z2
    ];
}

// Function to normalize a quaternion
function normalizeQuaternion(q) {
    let length = Math.sqrt(q[0] * q[0] + q[1] * q[1] + q[2] * q[2] + q[3] * q[3]);
    return [q[0] / length, q[1] / length, q[2] / length, q[3] / length];
}

// Function to get the inverse of a quaternion (assuming it's normalized)
function inverseQuaternion(q) {
    // Inverse is the conjugate for a normalized quaternion
    return [-q[0], -q[1], -q[2], q[3]];
}

// Function to rotate a vector by a quaternion
function rotateVectorByQuaternion(vec, quat) {
    // Convert vector to a quaternion (x, y, z, w) where w = 0
    let vecQuat = [vec[0], vec[1], vec[2], 0];

    // Inverse of quaternion (negate x, y, z, keep w)
    let invQuat = inverseQuaternion(quat);

    // Rotate vector: invQuat * vecQuat * quat
    let rotatedVecQuat = multiplyQuaternions(multiplyQuaternions(invQuat, vecQuat), quat);

    // Return the rotated vector part (ignore the w component)
    return [rotatedVecQuat[0], rotatedVecQuat[1], rotatedVecQuat[2]];
}

// Main function to transform a point into the local space of the cube
function transformPointToLocalSpace(point, cubePosition, cubeQuant) {
    console.log("Input point:", point);
    console.log("Cube position:", cubePosition);
    console.log("Cube quant:", cubeQuant);

    // Convert point and cubePosition to arrays
    let pointArray = [point.x, point.y, point.z];
    let positionArray = [cubePosition.x, cubePosition.y, cubePosition.z];

    // Step 1: Subtract cube position from the point (inverse translation)
    let localPoint = subtractVectors(pointArray, positionArray);
    console.log("Local point after translation:", localPoint);

    // Step 2: If cubeQuant represents a rotation, apply it
    // For now, we'll skip rotation since cubeQuant doesn't seem to be a valid quaternion
    let rotatedPoint = localPoint;

    // Step 3: Apply quantization
    let quantizedPoint = [
        rotatedPoint[0] / cubeQuant.x,
        rotatedPoint[1] / cubeQuant.y,
        rotatedPoint[2] / cubeQuant.z
    ];
    console.log("Quantized point:", quantizedPoint);

    // Step 4: Return the result as a vector4 (x, y, z, w)
    return [...quantizedPoint, 1];
}

// Function to transform a cube into the local space of another cube
function transformCubeToLocalSpace(cube, referenceCube) {
    console.log("Input cube:", cube);
    console.log("Reference cube:", referenceCube);

    // Extract position and quantization from the cubes
    let cubePosition = [cube.vertices[0].x, cube.vertices[0].y, cube.vertices[0].z];
    let referencePosition = [referenceCube.vertices[0].x, referenceCube.vertices[0].y, referenceCube.vertices[0].z];
    let cubeQuant = [cube.quant.x, cube.quant.y, cube.quant.z];
    let referenceQuant = [referenceCube.quant.x, referenceCube.quant.y, referenceCube.quant.z];

    // Step 1: Subtract reference cube position from the cube position (inverse translation)
    let localPosition = subtractVectors(cubePosition, referencePosition);
    console.log("Local position after translation:", localPosition);

    // Step 2: If reference cube has a rotation, apply it to the cube position
    if (referenceCube.rotation) {
        localPosition = rotateVectorByQuaternion(localPosition, inverseQuaternion(referenceCube.rotation));
        console.log("Local position after rotation:", localPosition);
    }

    // Step 3: Apply quantization
    let quantizedPosition = [
        localPosition[0] / referenceQuant[0],
        localPosition[1] / referenceQuant[1],
        localPosition[2] / referenceQuant[2]
    ];
    console.log("Quantized position:", quantizedPosition);

    // Step 4: Return the transformed cube with the new position and quantization
    return {
        position: {
            x: quantizedPosition[0],
            y: quantizedPosition[1],
            z: quantizedPosition[2]
        },
        quant: {
            x: cubeQuant[0] / referenceQuant[0],
            y: cubeQuant[1] / referenceQuant[1],
            z: cubeQuant[2] / referenceQuant[2]
        }
    };
}
let objectBoundingCubeNew = transformCubeToLocalSpace(objectBoundingCubeOld, selectorCube);

console.log(objectBoundingCubeNew);
*/
