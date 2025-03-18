using System;
using System.Collections.Generic;
using System.IO;
using BulletSharp;
using DynamicData;
using Microsoft.ClearScript.Util.Web;
using SharpDX;
using VolumetricSelection2077.Converters;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;
using WolvenKit.Common.PhysX;
using WolvenKit.RED4.Archive.CR2W;
using WolvenKit.RED4.Types;
using Plane = SharpDX.Plane;
using Vector4 = SharpDX.Vector4;

namespace VolumetricSelection2077.Parsers;

public class DirectAbbrMeshParser
{
    public static List<SharpDX.Vector3> GetPolygonVertices(HullPolygonData polygon, ConvexHullData hullData)
    {
        List<SharpDX.Vector3> vertices = new List<SharpDX.Vector3>();
        
        if (polygon.VRef8 >= hullData.VertexData8.Length)
        {
            Logger.Error($"VRef8 {polygon.VRef8} is bigger or equals to {hullData.VertexData8.Length}");
            return vertices; 
        }
        
        for (int i = 0; i < polygon.NbVerts; i++)
        {
            int index = hullData.VertexData8[polygon.VRef8 + i];

            if (index < hullData.HullVertices.Count)
            {
                vertices.Add(hullData.HullVertices[index]);
            }
        }

        return vertices;
    }
    
    private static AbbrMesh ParseConvexMesh(ConvexMesh convexMesh)
    {
        var polygons = new Polygon[convexMesh.HullData.Polygons.Count];
        int i = 0;
        foreach (var hullPolygonData in convexMesh.HullData.Polygons)
        {
            var polygon = new Polygon()
            {
                Plane = WolvenkitToSharpDX.Plane(hullPolygonData.Plane),
                Vertices = GetPolygonVertices(hullPolygonData, convexMesh.HullData).ToArray()
            };
            polygons[i] = polygon;
            i++;
        }
        var subMeshesOut = new AbbrSubMesh[1];
        subMeshesOut[0] = new AbbrSubMesh()
        {
            BoundingBox = new BoundingBox(convexMesh.HullData.AABB.Minimum, convexMesh.HullData.AABB.Maximum),
            Polygons = polygons
        };
        return new AbbrMesh()
        {
            SubMeshes = subMeshesOut
        };
    }

    private static AbbrMesh ParseTriangleMesh(BV4TriangleMesh triangleMesh)
    {
        var verts = triangleMesh.Vertices.ToArray();
        var polygons = new Polygon[triangleMesh.NbTriangles];
        int i = 0;
        foreach (var triangle in triangleMesh.Triangles)
        {
            var i1 = triangle[0];
            var i2 = triangle[1];
            var i3 = triangle[2];
            polygons[i] = new Polygon()
            {
                Plane = new Plane(verts[i1], verts[i2], verts[i3]),
                Vertices = new[] { verts[i1], verts[i2], verts[i3] }
            };
            i++;
        }
        var subMeshesOut = new AbbrSubMesh[1];
        subMeshesOut[0] = new AbbrSubMesh()
        {
            BoundingBox = new BoundingBox(triangleMesh.AABB.Minimum, triangleMesh.AABB.Maximum),
            Polygons = polygons

        };
        return new AbbrMesh()
        {
            SubMeshes = subMeshesOut
        };
    }
    
    public static AbbrMesh ParseFromPhysX(PhysXMesh inputMesh)
    {
        switch (inputMesh)
        {
            case ConvexMesh convexMesh:
                return ParseConvexMesh(convexMesh);
            case BV4TriangleMesh triangleMesh:
                return ParseTriangleMesh(triangleMesh);
            default:
                throw new ArgumentException("Invalid physics mesh");
        }
    }

    public static AbbrMesh? ParseFromCR2W(CR2WFile cr2w)
    {
        if (cr2w.RootChunk is not CMesh { RenderResourceBlob.Chunk: rendRenderMeshBlob rendBlob } cMesh)
        {
            return null;
        }

        int lowestLod = 1;
        var rendInfos = rendBlob.Header.RenderChunkInfos;
        foreach (var rendInfo in rendInfos)
        {
            if (rendInfo.LodMask > lowestLod) lowestLod = rendInfo.LodMask;
        }

        using var ms = new MemoryStream(rendBlob.RenderBuffer.Buffer.GetBytes());
        var br = new BinaryReader(ms);

        var quantScale = new Vector4(rendBlob.Header.QuantizationScale.X,
            rendBlob.Header.QuantizationScale.Y,
            rendBlob.Header.QuantizationScale.Z,
            rendBlob.Header.QuantizationScale.W);
        var quantOffset = new Vector4(rendBlob.Header.QuantizationOffset.X,
            rendBlob.Header.QuantizationOffset.Y,
            rendBlob.Header.QuantizationOffset.Z,
            rendBlob.Header.QuantizationOffset.W);

        List<AbbrSubMesh> submeshesOut = new();
        
        for(int indexSubMesh = 0; indexSubMesh < rendInfos.Count; indexSubMesh++)
        {
            var rendInfo = rendInfos[indexSubMesh];
            if (rendInfo.LodMask != lowestLod) continue;
            
            var vertsOut = new SharpDX.Vector3[rendInfo.NumVertices];
            var indicesOut = new uint[rendInfo.NumIndices];

            for (int indexVertex = 0; indexVertex < rendInfo.NumVertices; indexVertex++)
            {
                br.BaseStream.Position = rendInfo.ChunkVertices.ByteOffsets[0] + (indexVertex * rendInfo.ChunkVertices.VertexLayout.SlotStrides[0]);

                var x = (br.ReadInt16() / 32767f * quantScale.X) + quantOffset.X;
                var y = (br.ReadInt16() / 32767f * quantScale.Y) + quantOffset.Y;
                var z = (br.ReadInt16() / 32767f * quantScale.Z) + quantOffset.Z;
                
                vertsOut[indexVertex] = new SharpDX.Vector3(x, y, z);
            }

            br.BaseStream.Position = rendBlob.Header.IndexBufferOffset + rendInfo.ChunkIndices.TeOffset;
            for (int indexIndex = 0; indexIndex < rendInfo.NumIndices; indexIndex++)
            {
                indicesOut[indexIndex] = br.ReadUInt16();
            }

            var polygons = new Polygon[rendInfo.NumIndices / 3];
            int j = 0;
            for (int i = 0; i < indicesOut.Length; i += 3)
            {
                var ti1 = indicesOut[i];
                var ti2 = indicesOut[i + 1];
                var ti3 = indicesOut[i + 2];
                polygons[j] = new Polygon()
                {
                    Plane = new Plane(vertsOut[ti1], vertsOut[ti2], vertsOut[ti3]),
                    Vertices = new[] { vertsOut[ti1], vertsOut[ti2], vertsOut[ti3] }
                };
                j++;
            }
            
            submeshesOut.Add(new AbbrSubMesh()
            {
                BoundingBox = new OrientedBoundingBox(vertsOut).GetBoundingBox(),
                Polygons = polygons
            });
        }
        
        return new AbbrMesh()
        {
            SubMeshes = submeshesOut.ToArray()
        };
    }
}