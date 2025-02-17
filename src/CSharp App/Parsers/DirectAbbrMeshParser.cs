using System;
using System.Collections.Generic;
using System.IO;
using BulletSharp;
using Microsoft.ClearScript.Util.Web;
using SharpDX;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Services;
using WolvenKit.Common.PhysX;
using WolvenKit.RED4.Archive.CR2W;
using WolvenKit.RED4.Types;
using Vector4 = SharpDX.Vector4;

namespace VolumetricSelection2077.Parsers;

public class DirectAbbrMeshParser
{
    private static AbbrMesh ParseConvexMesh(ConvexMesh convexMesh)
    {
        var vertsOut = convexMesh.HullData.HullVertices.ToArray();
        var subMeshesOut = new AbbrSubMeshes[1];
        subMeshesOut[0] = new AbbrSubMeshes()
        {
            Vertices = vertsOut,
            Indices = new uint[0],
            BoundingBox = new BoundingBox(convexMesh.HullData.AABB.Minimum, convexMesh.HullData.AABB.Maximum),
            IsConvexCollider = true
        };
        return new AbbrMesh()
        {
            SubMeshes = subMeshesOut
        };
    }

    private static AbbrMesh ParseTriangleMesh(BV4TriangleMesh triangleMesh)
    {
        var vertsOut = triangleMesh.Vertices.ToArray();
        var indiciesOut = new uint[triangleMesh.NbTriangles * 3];
        int i = 0;
        foreach (var triangle in triangleMesh.Triangles)
        {
            indiciesOut[i] = triangle[0];
            indiciesOut[i+1] = triangle[1];
            indiciesOut[i+2] = triangle[2];
            i += 3;
        }
        var subMeshesOut = new AbbrSubMeshes[1];
        subMeshesOut[0] = new AbbrSubMeshes()
        {
            Vertices = vertsOut,
            Indices = indiciesOut,
            BoundingBox = new BoundingBox(triangleMesh.AABB.Minimum, triangleMesh.AABB.Maximum)
        };
        return new AbbrMesh()
        {
            SubMeshes = subMeshesOut
        };
    }
    
    public static AbbrMesh ParseFromPhysX(PhysXMesh inputMesh)
    {
        if (inputMesh is ConvexMesh convexMesh)
        {
            return ParseConvexMesh(convexMesh);
        }

        if (inputMesh is BV4TriangleMesh triangleMesh)
        {
            return ParseTriangleMesh(triangleMesh);
        }
        throw new Exception("Invalid input mesh type");
    }

    public static AbbrMesh? ParseFromCR2W(CR2WFile cr2w)
    {
        if (cr2w.RootChunk is not CMesh { RenderResourceBlob.Chunk: rendRenderMeshBlob rendBlob } cMesh)
        {
            throw new Exception("Invalid input mesh type");
        }

        int lowestLod = 1;
        var rendInfos = rendBlob.Header.RenderChunkInfos;
        foreach (var rendInfo in rendInfos)
        {
            if (rendInfo.LodMask > lowestLod) lowestLod = rendInfo.LodMask;
        }
        Logger.Info($"Found lowest LOD: {lowestLod}");

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

        List<AbbrSubMeshes> submeshesOut = new();
        
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

            for (int indexIndex = 0; indexIndex < rendInfo.NumIndices; indexIndex++)
            {
                indicesOut[indexIndex] = br.ReadUInt16();
            }
            
            submeshesOut.Add(new AbbrSubMeshes()
            {
                Vertices = vertsOut,
                Indices = indicesOut,
                BoundingBox = new OrientedBoundingBox(vertsOut).GetBoundingBox()
            });
        }
        
        return new AbbrMesh()
        {
            SubMeshes = submeshesOut.ToArray()
        };
    }
}