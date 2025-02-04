using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Converters;
using VolumetricSelection2077.Services;
using WolvenKit.RED4.Types;
using Quaternion = SharpDX.Quaternion;
using Vector3 = SharpDX.Vector3;

namespace VolumetricSelection2077.Parsers;

public class AbbrSectorParser
{
    public static AbbrSector? Deserialize(string jsonString)
    {
        JObject sectorRaw = JObject.Parse(jsonString);
        JObject? rootChunk = sectorRaw["Data"]?["RootChunk"] as JObject;
        JArray? nodes = rootChunk?["nodes"] as JArray;
        JArray? nodeData = rootChunk?["nodeData"]?["Data"] as JArray;

        JArray? instancedMeshNodeTransforms = null;
        
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
            
            if (node?["$type"]?.Value<string>() == "worldInstancedMeshNode")
            {
                if (instancedMeshNodeTransforms == null)
                {
                    instancedMeshNodeTransforms = node["worldTransformsBuffer"]?["sharedDataBuffer"]?["Data"]?["buffer"]?["Data"]?["Transforms"] as JArray;
                }
                
                int? startIndex = node["worldTransformsBuffer"]?["startIndex"]?.Value<int>();
                int? numElements = node["worldTransformsBuffer"]?["numElements"]?.Value<int>();

                if (startIndex == null || numElements == null)
                {
                    Logger.Error(
                        $"No start or num of elements found for InstancedMeshNode! {nodeDataEntry?["NodeIndex"]}: {startIndex ?? -1}, {numElements ?? -1}");
                    return null;
                }

                foreach (var element in instancedMeshNodeTransforms.ToArray().AsSpan((int)startIndex, (int)numElements))
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
                
            }
            else
            {
                transforms.Add(new AbbrSectorTransform()
                {
                    Position = new Vector3(nodeDataEntry?["Position"]?["X"]?.Value<float>() ?? 0,
                        nodeDataEntry?["Position"]?["Y"]?.Value<float>() ?? 0,
                        nodeDataEntry?["Position"]?["Z"]?.Value<float>() ?? 0),
                    Rotation = new Quaternion(nodeDataEntry?["Orientation"]?["i"]?.Value<float>() ?? 0,
                        nodeDataEntry?["Orientation"]?["j"]?.Value<float>() ?? 0,
                        nodeDataEntry?["Orientation"]?["k"]?.Value<float>() ?? 0,
                        nodeDataEntry?["Orientation"]?["r"]?.Value<float>() ?? 1),
                    Scale = new Vector3(nodeDataEntry?["Scale"]?["X"]?.Value<float>() ?? 0,
                        nodeDataEntry?["Scale"]?["Y"]?.Value<float>() ?? 0, 
                        nodeDataEntry?["Scale"]?["Z"]?.Value<float>() ?? 0), 
                });
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
            List<AbbrCollisionActors>? _collisionActors = new List<AbbrCollisionActors>();
            var actorArray = node?["Data"]?["compiledData"]?["Data"]?["Actors"] as JArray;
            if (actorArray?.GetType() == typeof(JArray))
            {
                foreach (var actor in actorArray)
                {
                    Vector3 _scale = new Vector3(actor?["Scale"]?["X"]?.Value<float>() ?? 0,
                        actor?["Scale"]?["Y"]?.Value<float>() ?? 0,
                        actor?["Scale"]?["Z"]?.Value<float>() ?? 0);
                    Quaternion _quaternion = new Quaternion(actor?["Orientation"]?["i"]?.Value<float>() ?? 0,
                        actor?["Orientation"]?["j"]?.Value<float>() ?? 0,
                        actor?["Orientation"]?["k"]?.Value<float>() ?? 0,
                        actor?["Orientation"]?["r"]?.Value<float>() ?? 1);
                    Vector3 _position = FixedPointVector3Converter.PosBitsToVec3(actor?["Position"]);
                    List<AbbrActorShapes>? _shapes = new List<AbbrActorShapes>();
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
                Actors = _collisionActors
            });
        }
        return new AbbrSector()
        {
            Nodes = _nodesEntries,
            NodeData = _nodeDataEntries
        };
    }
}