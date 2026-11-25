using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.Tests.Services;

public class ArchiveXlRemovalToolsTests
{
    #region ProcessRemoval

    [Theory]
    [InlineData(FilePostProcessingMode.New)]
    [InlineData(FilePostProcessingMode.Overwrite)]
    public void ProcessRemoval_NewOrOverwrite_ReturnsOriginalFile(FilePostProcessingMode mode)
    {
        var file = RemovalFile(Sector("sector"));

        var result = ArchiveXlRemovalTools.ProcessRemoval(file, mode, RemovalFile(Sector("other")));

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Same(file, result.Data);
        Assert.Equal(string.Empty, result.Message);
    }

    [Fact]
    public void ProcessRemoval_ExtendWithoutSecondFile_ReturnsError()
    {
        var result = ArchiveXlRemovalTools.ProcessRemoval(RemovalFile(), FilePostProcessingMode.Extend);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Extend processing mode was selected but no file to extend with was provided", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public void ProcessRemoval_SubtractWithoutSecondFile_ReturnsError()
    {
        var result = ArchiveXlRemovalTools.ProcessRemoval(RemovalFile(), FilePostProcessingMode.Subtract);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Subtract processing mode was selected but no file to subtract from was provided", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public void ProcessRemoval_Extend_MergesFilesAndReportsChanges()
    {
        var first = RemovalFile();
        var second = RemovalFile(Sector("sector", Node(0, 1, 2)));

        var result = ArchiveXlRemovalTools.ProcessRemoval(first, FilePostProcessingMode.Extend, second);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal("sector", Assert.Single(result.Data.Streaming.Sectors).Path);
        Assert.Equal("Extended file with New sectors: 1, New nodes: 1, New actors: 2", result.Message);
    }

    [Fact]
    public void ProcessRemoval_ExtendWithNoChanges_ReportsNoNewChanges()
    {
        var result = ArchiveXlRemovalTools.ProcessRemoval(RemovalFile(), FilePostProcessingMode.Extend, RemovalFile());

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.Streaming.Sectors);
        Assert.Equal("No new changes", result.Message);
    }

    [Fact]
    public void ProcessRemoval_Subtract_RemovesMatchingRemovalsAndReportsChanges()
    {
        var first = RemovalFile(Sector("sector", Node(0, 1)));
        var second = RemovalFile(Sector("sector", Node(0, 1)));

        var result = ArchiveXlRemovalTools.ProcessRemoval(first, FilePostProcessingMode.Subtract, second);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.Streaming.Sectors);
        Assert.Equal("Subtracted file with Removed sectors: 1, Removed nodes: 1", result.Message);
    }

    [Fact]
    public void ProcessRemoval_SubtractWithNoChanges_ReportsNoChanges()
    {
        var result = ArchiveXlRemovalTools.ProcessRemoval(RemovalFile(), FilePostProcessingMode.Subtract,
            RemovalFile(Sector("other", Node(0, 1))));

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.Streaming.Sectors);
        Assert.Equal("No changes", result.Message);
    }

    [Fact]
    public void ProcessRemoval_InvalidMode_ReturnsError()
    {
        var result = ArchiveXlRemovalTools.ProcessRemoval(RemovalFile(), (FilePostProcessingMode)99);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Invalid file processing mode", result.Message);
        Assert.Null(result.Data);
    }

    #endregion

    #region MergeRemovals

    [Fact]
    public void MergeRemovals_NewSectors_AreAddedAndCounted()
    {
        var first = RemovalFile(Sector("existing"));
        var second = RemovalFile(Sector("new", Node(0, 1, 2)));

        var (result, changes) = ArchiveXlRemovalTools.MergeRemovals(first, second);

        Assert.Equal(["existing", "new"], result.Streaming.Sectors.Select(x => x.Path));
        Assert.Equal(1, changes.NewSectors);
        Assert.Equal(1, changes.NewNodes);
        Assert.Equal(2, changes.NewActors);
        Assert.True(changes.HasChanges);
    }

    [Fact]
    public void MergeRemovals_NewNodesInExistingSector_AreAddedAndCounted()
    {
        var first = RemovalFile(Sector("shared", Node(0, 1)));
        var second = RemovalFile(Sector("shared", Node(1, 2, 3)));

        var (result, changes) = ArchiveXlRemovalTools.MergeRemovals(first, second);

        var sector = Assert.Single(result.Streaming.Sectors);
        Assert.Equal([0, 1], sector.NodeDeletions.Select(x => x.Index).OrderBy(x => x));
        Assert.Equal(0, changes.NewSectors);
        Assert.Equal(1, changes.NewNodes);
        Assert.Equal(2, changes.NewActors);
    }

    [Fact]
    public void MergeRemovals_MatchingNode_UnionsActorDeletionsAndTakesExpectedActorsFromSecondFile()
    {
        var first = RemovalFile(Sector("shared", Node(0, 1, 2)));
        first.Streaming.Sectors[0].NodeDeletions[0].ExpectedActors = 2;
        var second = RemovalFile(Sector("shared", Node(0, 2, 3)));
        second.Streaming.Sectors[0].NodeDeletions[0].ExpectedActors = 3;

        var (result, _) = ArchiveXlRemovalTools.MergeRemovals(first, second);

        var node = Assert.Single(Assert.Single(result.Streaming.Sectors).NodeDeletions);
        Assert.Equal([1, 2, 3], node.ActorDeletions.OrderBy(x => x));
        Assert.Equal(3, node.ExpectedActors);
    }

    #endregion

    #region SubtractRemovals

    [Fact]
    public void SubtractRemovals_LeavesActorsThatWereNotSubtracted_AndReturnsBaseInstance()
    {
        var first = RemovalFile(Sector("sector", Node(0, 1, 2, 3)));
        var second = RemovalFile(Sector("sector", Node(0, 2)));

        var (result, changes) = ArchiveXlRemovalTools.SubtractRemovals(first, second);

        Assert.Same(first, result);
        var node = Assert.Single(Assert.Single(result.Streaming.Sectors).NodeDeletions);
        Assert.Equal([1, 3], node.ActorDeletions);
        Assert.Equal(2, changes.NewActors);
        Assert.Equal(0, changes.NewNodes);
        Assert.Equal(0, changes.NewSectors);
    }

    [Fact]
    public void SubtractRemovals_WhenAllActorsAreSubtracted_RemovesTheNode()
    {
        var first = RemovalFile(Sector("sector", Node(0, 1), Node(1, 2)));
        var second = RemovalFile(Sector("sector", Node(0, 1)));

        var (result, changes) = ArchiveXlRemovalTools.SubtractRemovals(first, second);

        var node = Assert.Single(Assert.Single(result.Streaming.Sectors).NodeDeletions);
        Assert.Equal(1, node.Index);
        Assert.Equal(1, changes.NewNodes);
        Assert.Equal(0, changes.NewSectors);
    }

    [Fact]
    public void SubtractRemovals_WhenLastNodeIsRemoved_RemovesTheSector()
    {
        var first = RemovalFile(Sector("sector", Node(0, 1)));
        var second = RemovalFile(Sector("sector", Node(0, 1)));

        var (result, changes) = ArchiveXlRemovalTools.SubtractRemovals(first, second);

        Assert.Empty(result.Streaming.Sectors);
        Assert.Equal(1, changes.NewSectors);
        Assert.Equal(1, changes.NewNodes);
    }

    [Fact]
    public void SubtractRemovals_UnmatchedSectorsAndNodes_AreIgnored()
    {
        var first = RemovalFile(Sector("sector", Node(0, 1)));
        var second = RemovalFile(Sector("other", Node(0, 1)), Sector("sector", Node(5, 1)));

        var (result, changes) = ArchiveXlRemovalTools.SubtractRemovals(first, second);

        Assert.Same(first, result);
        Assert.Equal("sector", Assert.Single(result.Streaming.Sectors).Path);
        Assert.Equal([1], Assert.Single(result.Streaming.Sectors[0].NodeDeletions).ActorDeletions);
        Assert.False(changes.HasChanges);
    }

    [Fact]
    public void SubtractRemovals_WhenEitherActorListIsNull_RemovesTheNode()
    {
        var baseWithNullActors = RemovalFile(Sector("sector", Node(0)));
        baseWithNullActors.Streaming.Sectors[0].NodeDeletions[0].ActorDeletions = null!;

        var (result, changes) = ArchiveXlRemovalTools.SubtractRemovals(baseWithNullActors,
            RemovalFile(Sector("sector", Node(0, 1))));

        // The sector is removed too because it no longer has any nodes.
        Assert.Empty(result.Streaming.Sectors);
        Assert.Equal(1, changes.NewNodes);
        Assert.Equal(1, changes.NewSectors);
    }

    #endregion

    #region SerializeRemoval

    [Fact]
    public void SerializeRemoval_Json_UsesJsonPropertyNames()
    {
        var result = ArchiveXlRemovalTools.SerializeRemoval(RemovalFile(Sector("sector", Node(7, 1, 2))),
            AxlRemovalFormat.Json);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(string.Empty, result.Message);
        var json = JObject.Parse(result.Data!);
        var sector = json["streaming"]!["sectors"]![0]!;
        Assert.Equal("sector", sector["path"]!.Value<string>());
        Assert.Equal(7, sector["nodeDeletions"]![0]!["index"]!.Value<int>());
        Assert.Equal([1, 2], sector["nodeDeletions"]![0]!["actorDeletions"]!.Values<int>());
    }

    [Fact]
    public void SerializeRemoval_Yaml_UsesCamelCaseKeys()
    {
        var result = ArchiveXlRemovalTools.SerializeRemoval(RemovalFile(Sector("sector", Node(7, 1, 2))),
            AxlRemovalFormat.Yaml);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(string.Empty, result.Message);
        Assert.Contains("streaming:", result.Data);
        Assert.Contains("path: sector", result.Data);
        Assert.Contains("nodeDeletions:", result.Data);
        Assert.Contains("index: 7", result.Data);
        Assert.Contains("actorDeletions:", result.Data);
    }

    [Fact]
    public void SerializeRemoval_UnsupportedFormat_ReturnsError()
    {
        var result = ArchiveXlRemovalTools.SerializeRemoval(RemovalFile(), (AxlRemovalFormat)42);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Unsupported AxlRemovalFormat", result.Message);
        Assert.Null(result.Data);
    }

    #endregion

    #region SaveRemoval

    [Fact]
    public void SaveRemoval_NewWithMissingFile_WritesToRequestedPath()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "removals.json");
            var file = RemovalFile(Sector("sector", Node(0, 1)));

            var result = ArchiveXlRemovalTools.SaveRemoval(file, FilePostProcessingMode.New, AxlRemovalFormat.Json, path);

            Assert.Equal(ResultStatus.Ok, result.Status);
            Assert.Equal(path, result.Data);
            var written = Utils.TryParseAxlRemovalFile(File.ReadAllText(path));
            Assert.NotNull(written);
            Assert.Equal("sector", Assert.Single(written.Streaming.Sectors).Path);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void SaveRemoval_NewWithExistingFile_WritesNumberedFile()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "removals.json");
            File.WriteAllText(path, "{}");
            File.WriteAllText(Path.Combine(directory, "removals+1.json"), "{}");

            var result = ArchiveXlRemovalTools.SaveRemoval(RemovalFile(), FilePostProcessingMode.New,
                AxlRemovalFormat.Json, path);

            var expected = Path.Combine(directory, "removals+2.json");
            Assert.Equal(ResultStatus.Ok, result.Status);
            Assert.Equal(expected, result.Data);
            Assert.True(File.Exists(expected));
            Assert.Equal("{}", File.ReadAllText(path));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void SaveRemoval_NewWithYamlFormat_WritesYaml()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "removals.yaml");

            var result = ArchiveXlRemovalTools.SaveRemoval(RemovalFile(Sector("sector")), FilePostProcessingMode.New,
                AxlRemovalFormat.Yaml, path);

            Assert.Equal(ResultStatus.Ok, result.Status);
            Assert.Equal(path, result.Data);
            Assert.Contains("path: sector", File.ReadAllText(path));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void SaveRemoval_Overwrite_ReplacesExistingFile()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "removals.json");
            File.WriteAllText(path, "{}");

            var result = ArchiveXlRemovalTools.SaveRemoval(RemovalFile(Sector("replacement")),
                FilePostProcessingMode.Overwrite, AxlRemovalFormat.Json, path);

            Assert.Equal(ResultStatus.Ok, result.Status);
            Assert.Equal(path, result.Data);
            var written = Utils.TryParseAxlRemovalFile(File.ReadAllText(path));
            Assert.NotNull(written);
            Assert.Equal("replacement", Assert.Single(written.Streaming.Sectors).Path);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(FilePostProcessingMode.Extend)]
    [InlineData(FilePostProcessingMode.Subtract)]
    public void SaveRemoval_ExtendOrSubtractWithMissingFile_ReturnsError(FilePostProcessingMode mode)
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "removals.json");

            var result = ArchiveXlRemovalTools.SaveRemoval(RemovalFile(), mode, AxlRemovalFormat.Json, path);

            Assert.Equal(ResultStatus.Error, result.Status);
            Assert.Equal("File does not exist", result.Message);
            Assert.Null(result.Data);
            Assert.False(File.Exists(path));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(FilePostProcessingMode.Extend)]
    [InlineData(FilePostProcessingMode.Subtract)]
    public void SaveRemoval_ExtendOrSubtractWithUnparseableFile_ReturnsError(FilePostProcessingMode mode)
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "removals.json");
            File.WriteAllText(path, "not a removal file");

            var result = ArchiveXlRemovalTools.SaveRemoval(RemovalFile(), mode, AxlRemovalFormat.Json, path);

            Assert.Equal(ResultStatus.Error, result.Status);
            Assert.Equal("Failed to parse existing removal file", result.Message);
            Assert.Null(result.Data);
            Assert.Equal("not a removal file", File.ReadAllText(path));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void SaveRemoval_ExtendWithExistingFile_WritesMergedRemoval()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "removals.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(RemovalFile(Sector("existing", Node(0, 1)))));

            var result = ArchiveXlRemovalTools.SaveRemoval(RemovalFile(Sector("incoming", Node(0, 2))),
                FilePostProcessingMode.Extend, AxlRemovalFormat.Json, path);

            Assert.Equal(ResultStatus.Ok, result.Status);
            Assert.Equal(path, result.Data);
            var written = Utils.TryParseAxlRemovalFile(File.ReadAllText(path));
            Assert.NotNull(written);
            Assert.Equal(["existing", "incoming"], written.Streaming.Sectors.Select(x => x.Path));
            Assert.Equal([2], written.Streaming.Sectors[1].NodeDeletions[0].ActorDeletions);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void SaveRemoval_SubtractWithExistingFile_WritesSubtractedRemoval()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "removals.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(RemovalFile(Sector("sector", Node(0, 1, 2)))));

            var result = ArchiveXlRemovalTools.SaveRemoval(RemovalFile(Sector("sector", Node(0, 2))),
                FilePostProcessingMode.Subtract, AxlRemovalFormat.Json, path);

            Assert.Equal(ResultStatus.Ok, result.Status);
            Assert.Equal(path, result.Data);
            var written = Utils.TryParseAxlRemovalFile(File.ReadAllText(path));
            Assert.NotNull(written);
            var node = Assert.Single(Assert.Single(written.Streaming.Sectors).NodeDeletions);
            Assert.Equal([1], node.ActorDeletions);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    #endregion

    #region Helpers

    private static AxlRemovalFile RemovalFile(params AxlRemovalSector[] sectors) => new()
    {
        Streaming = new AxlRemovalStreaming
        {
            Sectors = sectors.ToList()
        }
    };

    private static AxlRemovalSector Sector(string path, params AxlRemovalNodeDeletion[] nodes) => new()
    {
        Path = path,
        ExpectedNodes = nodes.Length,
        NodeDeletions = nodes.ToList()
    };

    private static AxlRemovalNodeDeletion Node(int index, params int[] actors) => new()
    {
        Type = "worldStaticMeshNode",
        Index = index,
        ActorDeletions = actors.ToList(),
        ExpectedActors = actors.Length
    };

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "vs2077-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    #endregion
}
