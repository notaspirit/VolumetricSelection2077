using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SharpDX;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class AbbrMeshToBlender
{
    public static async void Run()
    {
        var gfs = GameFileService.Instance;
        /*
        var testMeshPath = @"ep1\worlds\03_night_city\sectors\_external\proxy\1930979112\hill_park_totem.mesh";
        var mesh = gfs.GetCMesh(testMeshPath);
        */
        ulong testsector = 1506287456064029993;
        ulong testMesh = 5884246486332581384;
        var mesh = await gfs.GetPhysXMesh(testsector, testMesh);
        if (mesh == null)
            throw new Exception($"Failed to get CMesh from {testsector}:{testMesh}.");
        GenerateBlenderScriptFromMesh(mesh);
    }
    
    public static void GenerateBlenderScriptFromMesh(AbbrMesh mesh)
    {
        foreach (var subMesh in mesh.SubMeshes)
        {
            Logger.Info(GenerateBlenderScriptFromSubmesh(subMesh));
        }
    }
    
    public static string GenerateBlenderScriptFromSubmesh(AbbrSubMesh abbrSubMesh)
    {
        Logger.Info($"Mesh has {abbrSubMesh.Polygons.Length} polygons.");
        StringBuilder script = new StringBuilder();
        script.AppendLine("import bpy");
        script.AppendLine("import bmesh");

        // Create mesh and object
        script.AppendLine("mesh = bpy.data.meshes.new(name='GeneratedMesh')");
        script.AppendLine("obj = bpy.data.objects.new(name='GeneratedObject', object_data=mesh)");
        script.AppendLine("bpy.context.collection.objects.link(obj)");
        script.AppendLine("bm = bmesh.new()");

        // Add vertices
        script.AppendLine("# Adding vertices");
        script.AppendLine("verts = []");

        Dictionary<Vector3, int> vertexMap = new Dictionary<Vector3, int>();
        int vertexIndex = 0;

        foreach (var polygon in abbrSubMesh.Polygons)
        {
            foreach (var vertex in polygon.Vertices)
            {
                if (!vertexMap.ContainsKey(vertex))
                {
                    script.AppendLine($"v = bm.verts.new(({vertex.X.ToString(CultureInfo.InvariantCulture)}, " +
                                      $"{vertex.Y.ToString(CultureInfo.InvariantCulture)}, " +
                                      $"{vertex.Z.ToString(CultureInfo.InvariantCulture)}))");
                    script.AppendLine("verts.append(v)");
                    vertexMap[vertex] = vertexIndex++;
                }
            }
        }

        script.AppendLine("bm.verts.ensure_lookup_table()");

        // Add faces
        script.AppendLine("# Adding faces");
        script.AppendLine("faces = []");

        foreach (var polygon in abbrSubMesh.Polygons)
        {
            string faceVerts = string.Join(", ", polygon.Vertices.Select(v => $"verts[{vertexMap[v]}]"));
            script.AppendLine($"faces.append(bm.faces.new([{faceVerts}]))");
        }

        // Finalize the mesh
        script.AppendLine("bm.to_mesh(mesh)");
        script.AppendLine("mesh.update()");
        script.AppendLine("bm.free()");

        return script.ToString();
    }
}