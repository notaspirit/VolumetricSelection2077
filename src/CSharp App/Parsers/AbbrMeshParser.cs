using System.Collections.Generic;
using VolumetricSelection2077.Models;
using SharpGLTF.Schema2;
using BulletSharp.Math;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.Parsers;

public class AbbrMeshParser
{
    private static Vector3 SysVec3ToBulletVec3(System.Numerics.Vector3 vec3)
    {
        return new Vector3(vec3.X, vec3.Y, vec3.Z);
    }

    private static string GetLowestLodLevel(ModelRoot model)
    {
        int lodLevel = 1;

        foreach (var mesh in model.LogicalMeshes)
        {
            int meshLodLevel = int.Parse(mesh.Name.Split("_")[^1]);
            if (meshLodLevel > lodLevel)
            {
                lodLevel = meshLodLevel;
            }
        }
        return lodLevel.ToString();
    }
    public static AbbrMesh? ParseFromGlb(ModelRoot meshRaw)
    {
        List<AbbrSubMeshes> _subMeshes = new List<AbbrSubMeshes>();
        if (meshRaw.LogicalMeshes.Count == 0)
        {
            return null;
        }
        string lowestLodLevel = GetLowestLodLevel(meshRaw);
        foreach (var mesh in meshRaw.LogicalMeshes)
        {
            if (!mesh.Name.EndsWith(lowestLodLevel)) continue;
            Logger.Info($"Mesh: {mesh.Name}");
            List<Vector3> _vertices = new List<Vector3>();
            IList<uint>? _indices = null;
            foreach (var primitive in mesh.Primitives)
            {
                var vertices = primitive.GetVertices("POSITION").AsVector3Array();
                _indices = primitive.GetIndices();
                foreach (var vertex in vertices)
                {
                    _vertices.Add(SysVec3ToBulletVec3(vertex));
                }
            }

            if (_vertices.Count == 0 || _indices == null || _indices.Count == 0)
            {
                Logger.Warning($"Mesh: {mesh.Name} has no vertices or indices!");
                continue;
            }
            _subMeshes.Add(new AbbrSubMeshes()
            {
                Indices = _indices,
                Vertices = _vertices,
            });
        }

        return new AbbrMesh()
        {
            SubMeshes = _subMeshes,
        };
    }
}