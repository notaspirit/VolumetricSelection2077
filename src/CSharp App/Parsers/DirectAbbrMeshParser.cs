using System;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using VolumetricSelection2077.Converters.Simple;
using VolumetricSelection2077.Models;
using WolvenKit.Common.PhysX;
using WolvenKit.RED4.Archive;
using WolvenKit.RED4.Archive.CR2W;
using WolvenKit.RED4.Types;
using Vector4 = SharpDX.Vector4;

namespace VolumetricSelection2077.Parsers;

public class DirectAbbrMeshParser
{
    /// <summary>
    /// Gets the indicies of a convex collision mesh
    /// </summary>
    /// <param name="polygon"></param>
    /// <param name="hullData"></param>
    /// <returns></returns>
    public static uint[] GetPolygonIndices(HullPolygonData polygon, ConvexHullData hullData)
    {
        var indices = new uint[polygon.NbVerts];
        for (int i = 0; i < polygon.NbVerts; i++)
        {
            indices[i] = hullData.VertexData8[polygon.VRef8 + i];
        }
        return indices;
    }
    
    /// <summary>
    /// Parses the collision n polygon mesh (convexMesh) into an AbbrMesh
    /// </summary>
    /// <param name="convexMesh"></param>
    /// <returns></returns>
    private static AbbrMesh ParseConvexMesh(ConvexMesh convexMesh)
    {
        var polygons = new uint[convexMesh.HullData.Polygons.Count][];
        int i = 0;
        foreach (var hullPolygonData in convexMesh.HullData.Polygons)
        {
            polygons[i] = GetPolygonIndices(hullPolygonData, convexMesh.HullData);
            i++;
        }
        var subMeshesOut = new AbbrSubMesh[1];
        subMeshesOut[0] = new AbbrSubMesh()
        {
            BoundingBox = new BoundingBox(convexMesh.HullData.AABB.Minimum, convexMesh.HullData.AABB.Maximum),
            Vertices = convexMesh.HullData.HullVertices.ToArray(),
            PolygonIndices = polygons
        };
        return new AbbrMesh()
        {
            SubMeshes = subMeshesOut
        };
    }
    
    /// <summary>
    /// Parses the collision triangle mesh into an AbbrMesh
    /// </summary>
    /// <param name="triangleMesh"></param>
    /// <returns></returns>
    private static AbbrMesh ParseTriangleMesh(BV4TriangleMesh triangleMesh)
    {
        var verts = triangleMesh.Vertices.ToArray();
        var polygons = new uint[triangleMesh.Triangles.Count][];
        int i = 0;
        foreach (var triangle in triangleMesh.Triangles)
        {
            polygons[i] = triangle.ToArray();
            i++;
        }
        var subMeshesOut = new AbbrSubMesh[1];
        subMeshesOut[0] = new AbbrSubMesh()
        {
            BoundingBox = new BoundingBox(triangleMesh.AABB.Minimum, triangleMesh.AABB.Maximum),
            Vertices = verts,
            PolygonIndices = polygons

        };
        return new AbbrMesh()
        {
            SubMeshes = subMeshesOut
        };
    }
    
    /// <summary>
    /// Parses a PhysX Collision mesh into an AbbrMesh
    /// </summary>
    /// <param name="inputMesh"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
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
    
    /// <summary>
    /// Parses a CMesh from an embedded file, uses <see cref="ParseFromCMesh"/> 
    /// </summary>
    /// <param name="efile"></param>
    /// <returns></returns>
    public static AbbrMesh? ParseFromEmbedded(ICR2WEmbeddedFile efile)
    {
        if (efile.Content is not CMesh cmesh)
            return null;
        return ParseFromCMesh(cmesh);
    }
    
    /// <summary>
    /// Parses a CMesh from a CR2W file, uses <see cref="ParseFromCMesh"/> 
    /// </summary>
    /// <param name="cr2w"></param>
    /// <returns></returns>
    public static AbbrMesh? ParseFromCR2W(CR2WFile cr2w)
    {
        if (cr2w.RootChunk is not CMesh cmesh)
            return null;
        return ParseFromCMesh(cmesh);
    }
    
    /// <summary>
    /// Parses the CMesh into an AbbrMesh
    /// </summary>
    /// <param name="inputMesh"></param>
    /// <returns></returns>
    /// <remarks>returns mesh with just bounding box for occlusion meshes</remarks>
    public static AbbrMesh? ParseFromCMesh(CMesh inputMesh)
    {
        if (inputMesh.RenderResourceBlob is not { Chunk: rendRenderMeshBlob rendBlob })
            return new AbbrMesh
            {
                SubMeshes =
                [
                    new AbbrSubMesh
                    {
                        BoundingBox = new BoundingBox
                        {
                            Minimum = WolvenkitToSharpDXConverter.Vector3(inputMesh.BoundingBox.Min),
                            Maximum = WolvenkitToSharpDXConverter.Vector3(inputMesh.BoundingBox.Max)
                        }
                    }
                ]
            };

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

            var polygons = new uint[rendInfo.NumIndices / 3][];
            int j = 0;
            for (int i = 0; i < indicesOut.Length; i += 3)
            {
                var ti1 = indicesOut[i];
                var ti2 = indicesOut[i + 1];
                var ti3 = indicesOut[i + 2];
                polygons[j] = new[] { ti1, ti2, ti3 };
                j++;
            }
            
            submeshesOut.Add(new AbbrSubMesh()
            {
                BoundingBox = new OrientedBoundingBox(vertsOut).GetBoundingBox(),
                Vertices = vertsOut,
                PolygonIndices = polygons
            });
        }
        
        return new AbbrMesh()
        {
            SubMeshes = submeshesOut.ToArray()
        };
    }
}