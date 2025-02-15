using System.Collections.Generic;
using System.Diagnostics;
using VolumetricSelection2077.Models;
using SharpGLTF.Schema2;
using SharpDX;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.TestingStuff;

namespace VolumetricSelection2077.Parsers;

public class AbbrMeshParser
{
    private static Vector3 SysVec3ToSharpVec3(System.Numerics.Vector3 vec3)
    {
        return new Vector3(vec3.X, -vec3.Z, vec3.Y);
    }

    private static string GetLowestLodLevel(ModelRoot model)
    {
        int lodLevel = 1;

        foreach (var mesh in model.LogicalMeshes)
        {
            int meshLodLevel;
            bool parsedLodLevel = int.TryParse(mesh.Name.Split("_")[^1], out meshLodLevel);
            if (!parsedLodLevel)
            {
                bool parsedLodLevel2 = int.TryParse(mesh.Name.Split("_")[^2], out int meshLodLevel2);
                if (!parsedLodLevel2)
                {
                    Logger.Error($"Failed to parse LOD level: {meshLodLevel}");
                    return "";
                }
                lodLevel = meshLodLevel2;
            }
            if (meshLodLevel > lodLevel)
            {
                lodLevel = meshLodLevel;
            }
        }
        return lodLevel.ToString();
    }
    public static AbbrMesh? ParseFromGlb(ModelRoot meshRaw)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        List<AbbrSubMeshes> _subMeshes = new List<AbbrSubMeshes>();
        if (meshRaw.LogicalMeshes.Count == 0)
        {
            return null;
        }
        string lowestLodLevel = GetLowestLodLevel(meshRaw);
        foreach (var mesh in meshRaw.LogicalMeshes)
        {
            if (!mesh.Name.EndsWith(lowestLodLevel)) continue;
            List<Vector3> _vertices = new List<Vector3>();
            IList<uint>? _indices = null;
            foreach (var primitive in mesh.Primitives)
            {
                var vertices = primitive.GetVertices("POSITION").AsVector3Array();
                _indices = primitive.GetIndices();
                foreach (var vertex in vertices)
                {
                    _vertices.Add(SysVec3ToSharpVec3(vertex));
                }
            }

            if (_vertices.Count == 0 || _indices == null || _indices.Count == 0)
            {
                Logger.Warning($"Mesh: {mesh.Name} has no vertices or indices!");
                continue;
            }
            
            OrientedBoundingBox meshBoundingBoxOBB = new(_vertices.ToArray());
            BoundingBox meshBoundingBox = meshBoundingBoxOBB.GetBoundingBox();
            _subMeshes.Add(new AbbrSubMeshes()
            {
                Indices = _indices,
                Vertices = _vertices,
                BoundingBox = meshBoundingBox
            });
        }

        
        stopwatch.Stop();
        Benchmarking.Instance.MeshGlbParsing.Add(stopwatch.Elapsed);
        
        return new AbbrMesh()
        {
            SubMeshes = _subMeshes,
        };
    }

    public static AbbrMesh? ParseFromJson(string jsonString)
    { 
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        JObject collisionMeshRaw = JObject.Parse(jsonString);
        
        var _vertices = collisionMeshRaw["Vertices"];
        var _indices = collisionMeshRaw["Triangles"];
        var _boundingBox = collisionMeshRaw["AABB"];
        
        bool _isConvexCollider = false;
        
        if (collisionMeshRaw["HullData"] != null)
        {
            _vertices = collisionMeshRaw?["HullData"]?["HullVertices"];
            _boundingBox = collisionMeshRaw?["HullData"]?["AABB"];
            _indices = new JArray();
            _isConvexCollider = true;
        }
        
        var hasVertices = _vertices != null;
        var hasIndices = _indices != null;
        var hasBoundingBox = _boundingBox != null;
        
        if (!hasVertices || !hasIndices || !hasBoundingBox)
        {
            Logger.Warning("Invalid mesh json!");
            return null;
        }
        
        List<Vector3> vertices = new List<Vector3>();
        IList<uint> indices = new List<uint>();
        BoundingBox meshBoundingBox = new();
        
        foreach (var vertex in _vertices)
        {
            // do vertices here need to also be inverted? => converted from glb => swap -Y and Z?
            vertices.Add(new Vector3(vertex["X"]?.Value<float>() ?? 0, vertex["Y"]?.Value<float>() ?? 0, vertex["Z"]?.Value<float>() ?? 0));
        }

        foreach (var triangle in _indices)
        {
            uint? index1 = triangle?[0]?.Value<uint>();
            uint? index2 = triangle?[1]?.Value<uint>();
            uint? index3 = triangle?[2]?.Value<uint>();

            if (index1 != null && index2 != null && index3 != null)
            {
                indices.Add((uint)index1);
                indices.Add((uint)index2);
                indices.Add((uint)index3);
            }
        }
        
        var bbMinX = _boundingBox?["Minimum"]?["X"]?.Value<float>();
        var bbMinY = _boundingBox?["Minimum"]?["Y"]?.Value<float>();
        var bbMinZ = _boundingBox?["Minimum"]?["Z"]?.Value<float>();
        
        var bbMaxX = _boundingBox?["Maximum"]?["X"]?.Value<float>();
        var bbMaxY = _boundingBox?["Maximum"]?["Y"]?.Value<float>();
        var bbMaxZ = _boundingBox?["Maximum"]?["Z"]?.Value<float>();
        
        if (bbMinX != null && bbMinY != null && bbMinZ != null && bbMaxX != null && bbMaxY != null && bbMaxZ != null)
        {
            var minimum = new Vector3(bbMinX.Value, bbMinY.Value, bbMinZ.Value);
            var maximum = new Vector3(bbMaxX.Value, bbMaxY.Value, bbMaxZ.Value);
            
            meshBoundingBox = new(minimum, maximum);
        }
        else
        {
            return null;
        }
        
        List<AbbrSubMeshes> subMeshes = new List<AbbrSubMeshes>();

        subMeshes.Add(new AbbrSubMeshes()
        {
            Indices = indices,
            Vertices = vertices,
            BoundingBox = meshBoundingBox,
            IsConvexCollider = _isConvexCollider,
        });

        stopwatch.Stop();
        Benchmarking.Instance.MeshJsonParsing.Add(stopwatch.Elapsed);
        
        return new AbbrMesh()
        {
            SubMeshes = subMeshes
        };
    }
}