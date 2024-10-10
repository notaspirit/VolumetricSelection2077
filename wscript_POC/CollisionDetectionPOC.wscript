// @author spirit
// @version 0.0.0
// @description
// Proof of Concept for Collision Detection for the CtrlADel project
// (Is actually more of a cursor test playground :kek:)

// Base64 encoded data from mesh json
let vert_base64 = "PXYkPYTbyz0py3NAAACAPzKsPL/FFb+7t+4eQAAAgD89diQ9xRW/u7fuHkAAAIA/Mqw8v4Tbyz0py3NAAACAP2QuEcCE28s9KctzQAAAgD9kLhHAxRW/u7fuHkAAAIA/UOtCwITbyz0py3NAAACAP1DrQsDFFb+7t+4eQAAAgD8yrDy/xRW/uzR+Vb4AAIA/PXYkPcUVv7s0flW+AACAP1DrQsDFFb+7NH5VvgAAgD9kLhHAxRW/uzR+Vb4AAIA/T+tCwIC1TL4py3NAAACAP2suEcAinsG9t+4eQAAAgD9P60LAIp7BvbfuHkAAAIA/ay4RwIK1TL4py3NAAACAP0+sPL99tUy+KctzQAAAgD9NrDy/NJ7BvbfuHkAAAIA/FHYkPYW1TL4py3NAAACAPxR2JD05nsG9t+4eQAAAgD9rLhHAIp7BvTR+Vb4AAIA/T+tCwCKewb00flW+AACAPxR2JD05nsG9NH5VvgAAgD9NrDy/NJ7BvTR+Vb4AAIA/KXYkPWkjoj5Vio8+AACAP1J2JD3BXU6+EthzQAAAgD8pdiQ9aSOiPhLYc0AAAIA/UnYkPcFdTr5Vio8+AACAP1LrQsDCXU6+VYqPPgAAgD9O60LAZyOiPhLYc0AAAIA/UutCwMJdTr4S2HNAAACAP07rQsBnI6I+VYqPPgAAgD8=";
let indi_base64 = "AAABAAIAAwABAAAABAABAAMABQABAAQABgAFAAQABwAFAAYAAgAIAAkAAQAIAAIACgAFAAcACwAFAAoADAANAA4ADwANAAwAEAANAA8AEQANABAAEgARABAAEwARABIADgAUABUADQAUAA4AFgARABMAFwARABYAGAAZABoAGwAZABgAHAAdAB4AHwAdABwA";

// object coordinates vector4
let objectCoordinates = [0, 0, 0, 1];

// object bounding box min max
let objectBoundingBoxMin = [-3.04561281, -0.201529533, -0.208489239, 1];
let objectBoundingBoxMax = [0.0401519015, 0.316676408, 3.81006289, 1];

// Selector box min max
let selectorBoxMin = [0, 0, 0, 1];
let selectorBoxMax = [1, 1, 1, 1];
let selectorBoxQuant = [0, 0, 0, 0];

// Function to convert a cube's min and max points into an array of vertices
function cubeMinMaxToVertices(minPoint, maxPoint) {
    return [
        [minPoint[0], minPoint[1], minPoint[2]],
        [maxPoint[0], minPoint[1], minPoint[2]],
        [maxPoint[0], maxPoint[1], minPoint[2]],
        [minPoint[0], maxPoint[1], minPoint[2]],
        [minPoint[0], minPoint[1], maxPoint[2]],
        [maxPoint[0], minPoint[1], maxPoint[2]],
        [maxPoint[0], maxPoint[1], maxPoint[2]],
        [minPoint[0], maxPoint[1], maxPoint[2]]
    ];
}

let selectorCube = cubeMinMaxToVertices(selectorBoxMin, selectorBoxMax);
let objectBoundingCube = cubeMinMaxToVertices(objectBoundingBoxMin, objectBoundingBoxMax);

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

let vertices = decodeVertices(vert_base64);

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

let indices = decodeIndices(indi_base64);

function checkIntersection(selectionBox, objectBox, rotation) {
    // Function to rotate a point around the origin
    function rotatePoint(point, angle) {
        let cosTheta = Math.cos(angle);
        let sinTheta = Math.sin(angle);
        return [
            point[0] * cosTheta - point[1] * sinTheta,
            point[0] * sinTheta + point[1] * cosTheta
        ];
    }

    // Rotate the objectBox corners by the given rotation angle
    let rotatedObjectBox = objectBox.map(point => rotatePoint(point, rotation));

    // Function to check if two boxes intersect
    function boxesIntersect(box1, box2) {
        for (let i = 0; i < box1.length; i++) {
            let next = (i + 1) % box1.length;
            let edge = [box1[next][0] - box1[i][0], box1[next][1] - box1[i][1]];
            let axis = [-edge[1], edge[0]];

            let projection1 = box1.map(point => point[0] * axis[0] + point[1] * axis[1]);
            let projection2 = box2.map(point => point[0] * axis[0] + point[1] * axis[1]);

            let min1 = Math.min(...projection1);
            let max1 = Math.max(...projection1);
            let min2 = Math.min(...projection2);
            let max2 = Math.max(...projection2);

            if (max1 < min2 || max2 < min1) {
                return false;
            }
        }
        return true;
    }

    // Check if the selectionBox intersects with the rotated objectBox
    return boxesIntersect(selectionBox, rotatedObjectBox) && boxesIntersect(rotatedObjectBox, selectionBox);
}

// Example usage
let selectionBox = [
    [10, 10],
    [1, 0],
    [1, 1],
    [0, 1]
];

let objectBox = [
    [2, 2],
    [3, 2],
    [3, 3],
    [2, 3]
];

let rotation = Math.PI / 4; // 45 degrees

console.log("Intersection:", checkIntersection(selectionBox, objectBox, rotation));


