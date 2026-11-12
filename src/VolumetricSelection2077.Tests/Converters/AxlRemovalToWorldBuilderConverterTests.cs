using Moq;
using VolumetricSelection2077.Core.Converters.Complex;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.Tests.Converters;

public class AxlRemovalToWorldBuilderConverterTests
{
    private static AxlRemovalToWorldBuilderConverter CreateConverter()
    {
        return new AxlRemovalToWorldBuilderConverter(new Mock<IGameFileService>().Object);
    }

    private static AxlRemovalFile RemovalFile(params AxlRemovalSector[] sectors) => new()
    {
        Streaming = new AxlRemovalStreaming
        {
            Sectors = sectors.ToList()
        }
    };

    [Fact]
    public void Convert_EmptyFile_ReturnsOkWithNamedRoot()
    {
        var result = CreateConverter().Convert(RemovalFile(), "root", new WBConverterConfig());

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal("root", result.Data.RootElement.Name);
        Assert.Empty(result.Data.RootElement.Children);
        Assert.Empty(result.Data.EmbeddedFiles);
        Assert.Equal(string.Empty, result.Message);
    }

    [Fact]
    public void Convert_SectorWithoutDeletions_IsSkipped()
    {
        var sector = new AxlRemovalSector
        {
            Path = "sector",
            ExpectedNodes = 0,
            NodeDeletions = []
        };

        var result = CreateConverter().Convert(RemovalFile(sector), "root", new WBConverterConfig());

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.RootElement.Children);
    }

    [Fact]
    public void Convert_SectorThatCannotBeLoaded_ReturnsWarning()
    {
        var sector = new AxlRemovalSector
        {
            Path = "sector",
            ExpectedNodes = 1,
            NodeDeletions =
            [
                new AxlRemovalNodeDeletion
                {
                    Type = "worldStaticMeshNode",
                    Index = 0
                }
            ]
        };

        var result = CreateConverter().Convert(RemovalFile(sector), "root", new WBConverterConfig());

        Assert.Equal(ResultStatus.Warning, result.Status);
        Assert.Contains("Failed to get sector sector", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal("root", result.Data.RootElement.Name);
        Assert.Empty(result.Data.RootElement.Children);
    }

    [Fact]
    public void AxlRemovalFile_DefaultConstructor_InitializesEmpty()
    {
        var file = new AxlRemovalFile();
        Assert.NotNull(file.Streaming);
        Assert.Empty(file.Streaming.Sectors);
    }

    [Fact]
    public void AxlRemovalSector_DefaultConstructor_InitializesEmpty()
    {
        var sector = new AxlRemovalSector();
        Assert.Equal(string.Empty, sector.Path);
        Assert.Equal(0, sector.ExpectedNodes);
        Assert.Empty(sector.NodeDeletions);
    }
}
