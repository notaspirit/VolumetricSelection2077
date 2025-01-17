using System;
using System.Collections.Generic;
using System.Linq;
using BulletSharp.Math;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Converters;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.Parsers;

public class AbbrSectorParser
{
    public static AbbrSector? Deserialize(string jsonString)
    {
        JObject sectorRaw = JObject.Parse(jsonString);
        JObject? rootChunk = sectorRaw["Data"]?["RootChunk"] as JObject;
        JArray? nodes = rootChunk?["nodes"] as JArray;
        JArray? nodeData = rootChunk?["nodeData"]?["Data"] as JArray;

        if (nodes == null || nodeData == null)
        {
            return null;
        }
        List<AbbrStreamingSectorNodeDataEntry> _nodeDataEntries = new List<AbbrStreamingSectorNodeDataEntry>();
        List<AbbrStreamingSectorNodesEntry> _nodesEntries = new List<AbbrStreamingSectorNodesEntry>();
        foreach (var nodeDataEntry in nodeData)
        {
            _nodeDataEntries.Add(new AbbrStreamingSectorNodeDataEntry()
            {
                Position = new Vector3(nodeDataEntry?["Position"]?["X"]?.Value<float>() ?? 0,
                    nodeDataEntry?["Position"]?["Y"]?.Value<float>() ?? 0,
                    nodeDataEntry?["Position"]?["Z"]?.Value<float>() ?? 0),
                Rotation = new Quaternion(nodeDataEntry?["Orientation"]?["i"]?.Value<int>() ?? 0,
                    nodeDataEntry?["Orientation"]?["j"]?.Value<int>() ?? 0,
                    nodeDataEntry?["Orientation"]?["k"]?.Value<int>() ?? 0,
                    nodeDataEntry?["Orientation"]?["r"]?.Value<int>() ?? 0),
                Scale = new Vector3(nodeDataEntry?["Scale"]?["X"]?.Value<int>() ?? 0,
                    nodeDataEntry?["Scale"]?["Y"]?.Value<int>() ?? 0, nodeDataEntry?["Scale"]?["Z"]?.Value<int>() ?? 0),
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
            catch (Exception ex)
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
                    catch (Exception ex)
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
                    Vector3 _scale = new Vector3(actor?["Scale"]?["X"]?.Value<int>() ?? 0,
                        actor?["Scale"]?["Y"]?.Value<int>() ?? 0, actor?["Scale"]?["Z"]?.Value<int>() ?? 0);
                    Quaternion _quaternion = new Quaternion(actor?["Orientation"]?["i"]?.Value<int>() ?? 0,
                        actor?["Orientation"]?["j"]?.Value<int>() ?? 0, actor?["Orientation"]?["k"]?.Value<int>() ?? 0,
                        actor?["Orientation"]?["r"]?.Value<int>() ?? 0);
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
                            Vector3 _scaleShape = new Vector3(shape?["Scale"]?["X"]?.Value<float>() ?? 0,
                                shape?["Scale"]?["Y"]?.Value<float>() ?? 0,
                                shape?["Scale"]?["Z"]?.Value<float>() ?? 0);
                            Quaternion _rotationShape = new Quaternion(
                                shape?["Orientation"]?["i"]?.Value<int>() ?? 0,
                                shape?["Orientation"]?["j"]?.Value<int>() ?? 0,
                                shape?["Orientation"]?["k"]?.Value<int>() ?? 0,
                                shape?["Orientation"]?["r"]?.Value<int>() ?? 0);
                            _shapes.Add(new AbbrActorShapes()
                            {
                                Hash = _hash,
                                Position = _positionShape,
                                Scale = _scaleShape,
                                Rotation = _rotationShape,
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
                        Scale = _scale,
                        Position = _position,
                        Rotation = _quaternion,
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