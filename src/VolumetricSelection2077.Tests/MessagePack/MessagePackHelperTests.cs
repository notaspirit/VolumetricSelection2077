using MessagePack;
using SharpDX;
using VolumetricSelection2077.Core.MessagePack.Helpers;
using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Tests.MessagePack;

public class MessagePackHelperTests
{
    [Fact]
    public void TryDeserialize_ValidData_ReturnsTrueAndValue()
    {
        var mesh = new VSMesh
        {
            SubMeshes =
            [
                new VSSubMesh
                {
                    BoundingBox = new BoundingBox(new Vector3(-1), new Vector3(1))
                }
            ]
        };
        var data = MessagePackSerializer.Serialize(mesh);

        var success = MessagePackHelper.TryDeserialize<VSMesh>(data, out var result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Single(result.SubMeshes);
    }

    [Fact]
    public void TryDeserialize_InvalidData_ReturnsFalseAndDefault()
    {
        var success = MessagePackHelper.TryDeserialize<VSMesh>([0xC1], out var result);

        Assert.False(success);
        Assert.Null(result);
    }
}
