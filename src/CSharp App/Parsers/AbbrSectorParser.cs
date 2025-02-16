using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Converters;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.TestingStuff;
using WolvenKit.RED4.Types;
using Quaternion = SharpDX.Quaternion;
using Vector3 = SharpDX.Vector3;

namespace VolumetricSelection2077.Parsers;

public class AbbrSectorParser
{
    public static AbbrSector? Deserialize(string jsonString)
    {
        /*
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        JObject sectorRaw = JObject.Parse(jsonString);
        JObject? rootChunk = sectorRaw["Data"]?["RootChunk"] as JObject;
        JArray? nodes = rootChunk?["nodes"] as JArray;
        JArray? nodeData = rootChunk?["nodeData"]?["Data"] as JArray;

        JArray? instancedMeshNodeTransforms = null;
        JArray? instancedDestructibleMeshNodeTransforms = null;
        
        if (nodes == null || nodeData == null)
        {
            return null;
        }
        List<AbbrStreamingSectorNodeDataEntry> _nodeDataEntries = new List<AbbrStreamingSectorNodeDataEntry>();
        List<AbbrStreamingSectorNodesEntry> _nodesEntries = new List<AbbrStreamingSectorNodesEntry>();
        foreach (var nodeDataEntry in nodeData)
        {
            if (nodeDataEntry?["Position"]?["X"]?.Value<float>() == null ||
                nodeDataEntry?["Position"]?["Y"]?.Value<float>() == null ||
                nodeDataEntry?["Position"]?["Z"]?.Value<float>() == null)
            {
                Logger.Warning("Cannot parse position of node data entry!");
            }
            if (nodeDataEntry?["Scale"]?["X"]?.Value<float>() == null ||
                nodeDataEntry?["Scale"]?["Y"]?.Value<float>() == null ||
                nodeDataEntry?["Scale"]?["Z"]?.Value<float>() == null)
            {
                Logger.Warning("Cannot parse scale of node data entry!");
            }
            if (nodeDataEntry?["Orientation"]?["i"]?.Value<float>() == null ||
                nodeDataEntry?["Orientation"]?["j"]?.Value<float>() == null ||
                nodeDataEntry?["Orientation"]?["k"]?.Value<float>() == null ||
                nodeDataEntry?["Orientation"]?["r"]?.Value<float>() == null)
            {
                Logger.Warning("Cannot parse rotation of node data entry!");
            }
            
            List<AbbrSectorTransform> transforms = new List<AbbrSectorTransform>();

            var node = nodes?[nodeDataEntry?["NodeIndex"].Value<int>()]?["Data"];
            string nodeType = node?["$type"].Value<string>();
            switch (nodeType)
            {
                case "worldInstancedMeshNode":
                    if (instancedMeshNodeTransforms == null)
                    {
                        instancedMeshNodeTransforms = node["worldTransformsBuffer"]?["sharedDataBuffer"]?["Data"]?["buffer"]?["Data"]?["Transforms"] as JArray;
                    }
                
                    int? startIndexWimn = node["worldTransformsBuffer"]?["startIndex"]?.Value<int>();
                    int? numElementsWimn = node["worldTransformsBuffer"]?["numElements"]?.Value<int>();

                    if (startIndexWimn == null || numElementsWimn == null)
                    {
                        Logger.Error(
                            $"No start or num of elements found for InstancedMeshNode! {nodeDataEntry?["NodeIndex"]}: {startIndexWimn ?? -1}, {numElementsWimn ?? -1}");
                        return null;
                    }

                    foreach (var element in instancedMeshNodeTransforms.ToArray().AsSpan((int)startIndexWimn, (int)numElementsWimn))
                    {
                        transforms.Add(new AbbrSectorTransform()
                        {
                            Position = new Vector3(element["translation"]["X"].Value<float>(), 
                                element["translation"]["Y"].Value<float>(), 
                                element["translation"]["Z"].Value<float>()),
                            Scale = new Vector3(element["scale"]["X"].Value<float>(), 
                                element["scale"]["Y"].Value<float>(), 
                                element["scale"]["Z"].Value<float>()),
                            Rotation = new Quaternion(element["rotation"]["i"].Value<float>(), 
                                element["rotation"]["j"].Value<float>(), 
                                element["rotation"]["k"].Value<float>(), 
                                element["rotation"]["r"].Value<float>()),
                        });
                    }
                    break;
                case "worldInstancedDestructibleMeshNode":

                    Vector3 nodeDataPosition = new(nodeDataEntry?["Position"]?["X"]?.Value<float>() ?? 0,
                        nodeDataEntry?["Position"]?["Y"]?.Value<float>() ?? 0,
                        nodeDataEntry?["Position"]?["Z"]?.Value<float>() ?? 0);
                    Quaternion nodeDataRotation = new(nodeDataEntry?["Orientation"]?["i"]?.Value<float>() ?? 0,
                        nodeDataEntry?["Orientation"]?["j"]?.Value<float>() ?? 0,
                        nodeDataEntry?["Orientation"]?["k"]?.Value<float>() ?? 0,
                        nodeDataEntry?["Orientation"]?["r"]?.Value<float>() ?? 1);
                    Vector3 nodeDataScale = new(nodeDataEntry?["Scale"]?["X"]?.Value<float>() ?? 1,
                            nodeDataEntry?["Scale"]?["Y"]?.Value<float>() ?? 1,
                            nodeDataEntry?["Scale"]?["Z"]?.Value<float>() ?? 1);
                    
                    if (instancedDestructibleMeshNodeTransforms == null)
                    {
                        instancedDestructibleMeshNodeTransforms = node["cookedInstanceTransforms"]?["sharedDataBuffer"]?["Data"]?["buffer"]?["Data"]?["Transforms"] as JArray;
                    }
                
                    int? startIndexWidmn = node["cookedInstanceTransforms"]?["startIndex"]?.Value<int>();
                    int? numElementsWidmn = node["cookedInstanceTransforms"]?["numElements"]?.Value<int>();

                    if (startIndexWidmn == null || numElementsWidmn == null)
                    {
                        Logger.Error(
                            $"No start or num of elements found for InstancedMeshNode! {nodeDataEntry?["NodeIndex"]}: {startIndexWidmn ?? -1}, {numElementsWidmn ?? -1}");
                        return null;
                    }

                    foreach (var element in instancedDestructibleMeshNodeTransforms.ToArray().AsSpan((int)startIndexWidmn, (int)numElementsWidmn))
                    {
                        transforms.Add(new AbbrSectorTransform()
                        {
                            Position = new Vector3(element["position"]["X"].Value<float>(), 
                                element["position"]["Y"].Value<float>(), 
                                element["position"]["Z"].Value<float>()) + nodeDataPosition,
                            Scale = nodeDataScale,
                            Rotation = new Quaternion(element["orientation"]["i"].Value<float>(), 
                                element["orientation"]["j"].Value<float>(), 
                                element["orientation"]["k"].Value<float>(), 
                                element["orientation"]["r"].Value<float>()) * nodeDataRotation,
                        });
                    }
                    break;
                default:
                    transforms.Add(new AbbrSectorTransform()
                    {
                        Position = new Vector3(nodeDataEntry?["Position"]?["X"]?.Value<float>() ?? 0,
                            nodeDataEntry?["Position"]?["Y"]?.Value<float>() ?? 0,
                            nodeDataEntry?["Position"]?["Z"]?.Value<float>() ?? 0),
                        Rotation = new Quaternion(nodeDataEntry?["Orientation"]?["i"]?.Value<float>() ?? 0,
                            nodeDataEntry?["Orientation"]?["j"]?.Value<float>() ?? 0,
                            nodeDataEntry?["Orientation"]?["k"]?.Value<float>() ?? 0,
                            nodeDataEntry?["Orientation"]?["r"]?.Value<float>() ?? 1),
                        Scale = new Vector3(nodeDataEntry?["Scale"]?["X"]?.Value<float>() ?? 1,
                            nodeDataEntry?["Scale"]?["Y"]?.Value<float>() ?? 1, 
                            nodeDataEntry?["Scale"]?["Z"]?.Value<float>() ?? 1), 
                    });
                    break;
            }
            _nodeDataEntries.Add(new AbbrStreamingSectorNodeDataEntry()
            {
                Transforms = transforms,
                NodeIndex = nodeDataEntry?["NodeIndex"]?.Value<int>() ?? 0,
            });
        }

        foreach (JToken node in nodes)
        {
            string _type = node?["Data"]?["$type"]?.Value<string>() ?? "";
            string? _sectorHash = node?["Data"]?["sectorHash"]?.Value<string>() ?? null;
            string? _debugName = node?["Data"]?["debugName"]?["$value"]?.Value<string>() ?? null;
            string? _meshPath = null;

            try
            {
                _meshPath = node?["Data"]?["mesh"]?["DepotPath"]?["$value"]?.Value<string>() ?? null;
            }
            catch (Exception)
            {
                
            }
            finally
            {
                if (_meshPath == null)
                {
                    try
                    {
                        _meshPath = node?["Data"]?["meshRef"]?["DepotPath"]?["$value"]?.Value<string>() ?? null;
                    }
                    catch (Exception)
                    {
                        
                    }
                }
            }
            List<AbbrCollisionActors>? _collisionActors = new();
            var actorArray = node?["Data"]?["compiledData"]?["Data"]?["Actors"] as JArray;
            if (actorArray?.GetType() == typeof(JArray))
            {
                foreach (var actor in actorArray)
                {
                    Vector3 _scale = new Vector3(actor?["Scale"]?["X"]?.Value<float>() ?? 1,
                        actor?["Scale"]?["Y"]?.Value<float>() ?? 1,
                        actor?["Scale"]?["Z"]?.Value<float>() ?? 1);
                    Quaternion _quaternion = new Quaternion(actor?["Orientation"]?["i"]?.Value<float>() ?? 0,
                        actor?["Orientation"]?["j"]?.Value<float>() ?? 0,
                        actor?["Orientation"]?["k"]?.Value<float>() ?? 0,
                        actor?["Orientation"]?["r"]?.Value<float>() ?? 1);
                    Vector3 _position = FixedPointVector3Converter.PosBitsToVec3(actor?["Position"]);
                    List<AbbrActorShapes>? _shapes = new();
                    var shapeArray = actor?["Shapes"];
                    if (shapeArray?.GetType() == typeof(JArray))
                    {
                        foreach (var shape in shapeArray)
                        {
                            string _shapeType = shape?["ShapeType"]?.Value<string>() ?? "";
                            string? _hash = shape?["Hash"]?.Value<string>() ?? null;
                            Vector3 _positionShape = new Vector3(shape?["Position"]?["X"]?.Value<float>() ?? 0,
                                shape?["Position"]?["Y"]?.Value<float>() ?? 0,
                                shape?["Position"]?["Z"]?.Value<float>() ?? 0);
                            Vector3 _scaleShape = new Vector3(shape?["Scale"]?["X"]?.Value<float>() ?? shape?["Size"]?["X"]?.Value<float>() ?? 1,
                                shape?["Scale"]?["Y"]?.Value<float>() ?? shape?["Size"]?["Y"]?.Value<float>() ?? 1,
                                shape?["Scale"]?["Z"]?.Value<float>() ?? shape?["Size"]?["Z"]?.Value<float>() ?? 1);
                            Quaternion _rotationShape = new Quaternion(
                                shape?["Rotation"]?["i"]?.Value<float>() ?? 0,
                                shape?["Rotation"]?["j"]?.Value<float>() ?? 0,
                                shape?["Rotation"]?["k"]?.Value<float>() ?? 0,
                                shape?["Rotation"]?["r"]?.Value<float>() ?? 1);
                            _shapes.Add(new AbbrActorShapes()   
                            {
                                Hash = _hash,
                                Transform = new AbbrSectorTransform(){
                                    Position = _positionShape,
                                    Scale = _scaleShape,
                                    Rotation = _rotationShape,
                                    },
                                ShapeType = _shapeType
                            });
                        }
                    }
                    else
                    {
                        _shapes = null;
                    }

                    _collisionActors.Add(new AbbrCollisionActors()
                    {
                        Transform = new AbbrSectorTransform(){
                            Scale = _scale,
                            Position = _position,
                            Rotation = _quaternion,
                            },
                        Shapes = _shapes
                    });
                }
            }
            else
            {
                _collisionActors = null;
            }

            _nodesEntries.Add(new AbbrStreamingSectorNodesEntry()
            {
                Type = _type,
                MeshDepotPath = _meshPath,
                SectorHash = _sectorHash,
                Actors = _collisionActors,
                DebugName = _debugName
            });
        }
        
        stopwatch.Stop();
        Benchmarking.Instance.SectorParsing.Add(stopwatch.Elapsed);
        
        return new AbbrSector()
        {
            Nodes = _nodesEntries,
            NodeData = _nodeDataEntries
        };
        */
        return null;
    }
}