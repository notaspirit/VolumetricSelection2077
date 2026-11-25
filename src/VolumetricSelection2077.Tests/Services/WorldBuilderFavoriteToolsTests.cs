using Newtonsoft.Json;
using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Json.Helpers;
using VolumetricSelection2077.Core.Models;
using VolumetricSelection2077.Core.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Core.Models.WorldBuilder.Favorites;
using VolumetricSelection2077.Core.Models.WorldBuilder.Spawn;
using VolumetricSelection2077.Core.Models.WorldBuilder.Spawn.Mesh;
using VolumetricSelection2077.Core.Models.WorldBuilder.Structs;
using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.Tests.Services;

public class WorldBuilderFavoriteToolsTests
{
    private const string MeshResource = @"base\fx\meshes\cube_debug.mesh";

    #region ProcessFavorite

    [Theory]
    [InlineData(FilePostProcessingMode.New)]
    [InlineData(FilePostProcessingMode.Overwrite)]
    public void ProcessFavorite_NewOrOverwrite_ReturnsOriginalFavorite(FilePostProcessingMode mode)
    {
        var favorite = EmptyFavorite("Favorite");

        var result = WorldBuilderFavoriteTools.ProcessFavorite(favorite, mode, EmptyFavorite("Other"));

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Same(favorite, result.Data);
        Assert.Equal(string.Empty, result.Message);
    }

    [Fact]
    public void ProcessFavorite_ExtendWithoutSecondFavorite_ReturnsError()
    {
        var result = WorldBuilderFavoriteTools.ProcessFavorite(EmptyFavorite("Favorite"), FilePostProcessingMode.Extend);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Extend processing mode was selected but no file to extend with was provided", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public void ProcessFavorite_SubtractWithoutSecondFavorite_ReturnsError()
    {
        var result = WorldBuilderFavoriteTools.ProcessFavorite(EmptyFavorite("Favorite"), FilePostProcessingMode.Subtract);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Subtract processing mode was selected but no file to subtract from was provided", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public void ProcessFavorite_Extend_MergesFavoritesAndReportsNewElements()
    {
        var first = EmptyFavorite("First");
        var second = FavoriteWith("Second", Group("Group", MeshElement("mesh")));

        var result = WorldBuilderFavoriteTools.ProcessFavorite(first, FilePostProcessingMode.Extend, second);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal("First", result.Data.Name);
        Assert.Equal(["Group/mesh"], ElementPaths(result.Data));
        Assert.Equal("Extended file with 1 new elements", result.Message);
    }

    [Fact]
    public void ProcessFavorite_ExtendWithNoChanges_ReportsNoNewChanges()
    {
        var result = WorldBuilderFavoriteTools.ProcessFavorite(EmptyFavorite("First"), FilePostProcessingMode.Extend,
            EmptyFavorite("Second"));

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.Data.Children);
        Assert.Equal("No new changes", result.Message);
    }

    [Fact]
    public void ProcessFavorite_Subtract_RemovesMatchingElementsAndReportsCount()
    {
        var first = FavoriteWith("First", Group("Group", MeshElement("mesh")));
        var second = FavoriteWith("Second", Group("Group", MeshElement("mesh")));

        var result = WorldBuilderFavoriteTools.ProcessFavorite(first, FilePostProcessingMode.Subtract, second);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.Data.Children);
        Assert.Equal("Subtracted 1 elements from file", result.Message);
    }

    [Fact]
    public void ProcessFavorite_SubtractWithNoChanges_ReportsNoChanges()
    {
        var result = WorldBuilderFavoriteTools.ProcessFavorite(EmptyFavorite("First"), FilePostProcessingMode.Subtract,
            FavoriteWith("Second", Group("Group", MeshElement("mesh"))));

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.Data.Children);
        Assert.Equal("No changes", result.Message);
    }

    [Fact]
    public void ProcessFavorite_InvalidMode_ReturnsError()
    {
        var result = WorldBuilderFavoriteTools.ProcessFavorite(EmptyFavorite("Favorite"), (FilePostProcessingMode)99);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Invalid file processing mode", result.Message);
        Assert.Null(result.Data);
    }

    #endregion

    #region SavePrefab

    [Fact]
    public void SavePrefab_NewWithMissingFile_CreatesRootWithDefaultMetadata()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "GeneratedByVS2077.json");

            var result = WorldBuilderFavoriteTools.SavePrefab(EmptyFavorite("New Favorite"), FilePostProcessingMode.New,
                path);

            Assert.Equal(ResultStatus.Ok, result.Status);
            Assert.Equal("New Favorite", result.Data);
            var root = ReadRoot(path);
            Assert.Equal("Generated by VS2077", root.Name);
            Assert.Equal("Group", root.Icon);
            Assert.False(root.Grouped);
            Assert.False(root.HeaderOpen);
            var written = Assert.Single(root.Favorites);
            Assert.Equal("New Favorite", written.Name);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void SavePrefab_NewWithExistingName_AppendsNextAvailableNumber()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "GeneratedByVS2077.json");
            WriteRoot(path, EmptyFavorite("Favorite"), EmptyFavorite("Favorite+1"), EmptyFavorite("Favorite+3"));

            var result = WorldBuilderFavoriteTools.SavePrefab(EmptyFavorite("Favorite"), FilePostProcessingMode.New, path);

            Assert.Equal(ResultStatus.Ok, result.Status);
            Assert.Equal("Favorite+4", result.Data);
            Assert.Equal(["Favorite", "Favorite+1", "Favorite+3", "Favorite+4"],
                ReadRoot(path).Favorites.Select(x => x.Name));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void SavePrefab_OverwriteExistingFavorite_WritesIncomingDataUnderOriginalName()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "GeneratedByVS2077.json");
            WriteRoot(path, FavoriteWith("Favorite", Group("Group", MeshElement("original"))));

            var result = WorldBuilderFavoriteTools.SavePrefab(
                FavoriteWith("Favorite", Group("Group", MeshElement("replacement"))),
                FilePostProcessingMode.Overwrite, path);

            Assert.Equal(ResultStatus.Ok, result.Status);
            Assert.Equal("Favorite", result.Data);
            var written = ReadRoot(path).Favorites;
            // SavePrefab adds the processed favorite to the root alongside the existing one.
            Assert.Equal(2, written.Count);
            Assert.Equal(["Group/original"], ElementPaths(written[0]));
            Assert.Equal("Favorite", written[1].Name);
            Assert.Equal(["Group/replacement"], ElementPaths(written[1]));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void SavePrefab_ExtendExistingFavorite_AppendsMergedFavorite()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "GeneratedByVS2077.json");
            WriteRoot(path, FavoriteWith("Favorite", Group("Group", MeshElement("first"))));

            var result = WorldBuilderFavoriteTools.SavePrefab(
                FavoriteWith("Favorite", Group("Group", MeshElement("second"))),
                FilePostProcessingMode.Extend, path);

            Assert.Equal(ResultStatus.Ok, result.Status);
            Assert.Equal("Favorite", result.Data);
            var written = ReadRoot(path).Favorites;
            Assert.Equal(2, written.Count);
            Assert.Equal(["Group/first", "Group/second"], ElementPaths(written[1]));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void SavePrefab_SubtractExistingFavorite_AppendsFavoriteWithRemainingElements()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "GeneratedByVS2077.json");
            WriteRoot(path, FavoriteWith("Favorite", Group("Group", MeshElement("remove"), MeshElement("keep"))));

            var result = WorldBuilderFavoriteTools.SavePrefab(
                FavoriteWith("Favorite", Group("Group", MeshElement("remove"))),
                FilePostProcessingMode.Subtract, path);

            Assert.Equal(ResultStatus.Ok, result.Status);
            Assert.Equal("Favorite", result.Data);
            var written = ReadRoot(path).Favorites;
            Assert.Equal(2, written.Count);
            Assert.Equal(["Group/keep"], ElementPaths(written[1]));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(FilePostProcessingMode.Extend)]
    [InlineData(FilePostProcessingMode.Subtract)]
    public void SavePrefab_ExtendOrSubtractWithoutMatchingFavorite_ReturnsError(FilePostProcessingMode mode)
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "GeneratedByVS2077.json");
            WriteRoot(path, EmptyFavorite("Other"));

            var result = WorldBuilderFavoriteTools.SavePrefab(EmptyFavorite("Favorite"), mode, path);

            Assert.Equal(ResultStatus.Error, result.Status);
            Assert.Equal("Failed to find existing favorite", result.Message);
            Assert.Null(result.Data);
            Assert.Equal("Other", Assert.Single(ReadRoot(path).Favorites).Name);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    public void SavePrefab_WithUnparseableExistingFile_ReturnsError(string contents)
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "GeneratedByVS2077.json");
            File.WriteAllText(path, contents);

            var result = WorldBuilderFavoriteTools.SavePrefab(EmptyFavorite("Favorite"), FilePostProcessingMode.New,
                path);

            Assert.Equal(ResultStatus.Error, result.Status);
            Assert.Contains("Failed to parse existing favorites file", result.Message);
            Assert.Null(result.Data);
            Assert.Equal(contents, File.ReadAllText(path));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    #endregion

    #region Helpers

    private static Favorite EmptyFavorite(string name) => new()
    {
        Name = name,
        Data = new Positionable()
    };

    private static Favorite FavoriteWith(string name, params PositionableGroup[] groups) => new()
    {
        Name = name,
        Data = new Positionable
        {
            Children = groups.Cast<Element>().ToList()
        }
    };

    private static PositionableGroup Group(string name, params Element[] children) => new()
    {
        Name = name,
        Children = children.ToList()
    };

    private static SpawnableElement MeshElement(string name) => new()
    {
        Name = name,
        Spawnable = new Mesh
        {
            ResourcePath = MeshResource,
            Position = new Vector4(0, 0, 0, 1),
            EulerRotation = new EulerAngles()
        }
    };

    private static string[] ElementPaths(Favorite favorite) =>
        favorite.Data.Children
            .OfType<PositionableGroup>()
            .SelectMany(group => group.Children
                .OfType<SpawnableElement>()
                .Select(element => $"{group.Name}/{element.Name}"))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

    private static FavoritesRoot ReadRoot(string path) =>
        JsonConvert.DeserializeObject<FavoritesRoot>(File.ReadAllText(path), JsonSerializerPresets.WorldBuilder)!;

    private static void WriteRoot(string path, params Favorite[] favorites) =>
        File.WriteAllText(path, JsonConvert.SerializeObject(
            new FavoritesRoot { Favorites = favorites.ToList() }, JsonSerializerPresets.WorldBuilder));

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "vs2077-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    #endregion
}
