using System.Globalization;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class CheckAbbrMeshParser
{
    public static void Test(AbbrMesh mesh)
    {
        foreach (var submesh in mesh.SubMeshes)
        {
            string blenderVerticies = "verts = [\n";
            foreach (var vertex in submesh.Vertices)
            {
                blenderVerticies += $"bm.verts.new(({vertex.X.ToString(CultureInfo.InvariantCulture)}, " +
                                    $"{vertex.Y.ToString(CultureInfo.InvariantCulture)}, " +
                                    $"{vertex.Z.ToString(CultureInfo.InvariantCulture)})),\n";
            }
            blenderVerticies += "]";
            
            string blenderIndicies = "faces = [\n";
            for (int i = 0; i < submesh.Indices.Length; i += 3)
            {
                blenderIndicies += $"({submesh.Indices[i]}, {submesh.Indices[i+1]}, {submesh.Indices[i+2]}),\n";
            }
            blenderIndicies += "]";
            Logger.Debug($"\n" +
                         $"{blenderVerticies}\n" +
                         $"\n" +
                         $"{blenderIndicies}");
        }
    }

    public static void BuildBlenderCollision(string rawJson)
    {
        JObject collisionMeshRaw = JObject.Parse(rawJson);
        
        var _vertices = collisionMeshRaw["Vertices"] as JArray;
        var _indices = collisionMeshRaw["Triangles"] as JArray;
        
        string blenderVerticies = "verts = [\n";
        foreach (var vertex in _vertices)
        {
            blenderVerticies += $"bm.verts.new(({vertex["X"]?.Value<float>().ToString(CultureInfo.InvariantCulture)}, " +
                                $"{vertex["Y"]?.Value<float>().ToString(CultureInfo.InvariantCulture)}, " +
                                $"{vertex["Z"]?.Value<float>().ToString(CultureInfo.InvariantCulture)})),\n";
        }
        blenderVerticies += "]";
            
        string blenderIndicies = "faces = [\n";
        foreach (var triangle in _indices)
        {
            blenderIndicies += $"({triangle[0]}, {triangle[1]}, {triangle[2]}),\n";
        }
        blenderIndicies += "]";
        Logger.Debug($"\n" +
                     $"{blenderVerticies}\n" +
                     $"\n" +
                     $"{blenderIndicies}");
    }
}