using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Tests.Models;

public class ResultTests
{
    [Fact]
    public void Constructor_SetsStatusAndMessage()
    {
        var result = new Result(ResultStatus.Warning, "something happened");

        Assert.Equal(ResultStatus.Warning, result.Status);
        Assert.Equal("something happened", result.Message);
    }

    [Fact]
    public void AddMessage_FirstMessage_ReplacesEmptyMessage()
    {
        var result = new Result();

        result.AddMessage("first");

        Assert.Equal("first", result.Message);
    }

    [Fact]
    public void AddMessage_AdditionalMessages_AreJoinedWithNewLines()
    {
        var result = new Result();

        result.AddMessage("first");
        result.AddMessage("second");
        result.AddMessage("third");

        Assert.Equal("first\nsecond\nthird", result.Message);
    }

    [Fact]
    public void ClearMessage_RemovesAllMessages()
    {
        var result = new Result(ResultStatus.Error, "first");
        result.AddMessage("second");

        result.ClearMessage();

        Assert.Equal(string.Empty, result.Message);
        Assert.Equal(ResultStatus.Error, result.Status);
    }

    [Fact]
    public void GenericResult_StoresData()
    {
        var result = new Result<int>
        {
            Status = ResultStatus.Ok,
            Data = 42
        };

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(42, result.Data);
    }
}
