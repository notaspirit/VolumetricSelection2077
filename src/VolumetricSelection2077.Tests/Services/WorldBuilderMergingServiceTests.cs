using Newtonsoft.Json;
using VolumetricSelection2077.Core.Json.Helpers;
using VolumetricSelection2077.Core.Models.WorldBuilder.Editor;
using VolumetricSelection2077.Core.Models.WorldBuilder.Favorites;
using VolumetricSelection2077.Core.Models.WorldBuilder.Spawn;
using VolumetricSelection2077.Core.Models.WorldBuilder.Spawn.Entity;
using VolumetricSelection2077.Core.Models.WorldBuilder.Spawn.Light;
using VolumetricSelection2077.Core.Models.WorldBuilder.Spawn.Mesh;
using VolumetricSelection2077.Core.Models.WorldBuilder.Spawn.Visual;
using VolumetricSelection2077.Core.Models.WorldBuilder.Structs;
using VolumetricSelection2077.Core.Services;
using WEnums = WolvenKit.RED4.Types.Enums;

namespace VolumetricSelection2077.Tests.Services;

public class WorldBuilderMergingServiceTests
{
    private const string MeshResource = @"base\fx\meshes\cube_debug.mesh";

    #region Merge

    [Fact]
    public void Merge_EmptyFavorites_ReturnsEmptyRootGroupNamedAfterFirstFavorite()
    {
        var result = WorldBuilderMergingService.Merge(EmptyFavorite("First"), EmptyFavorite("Second"));

        Assert.Equal("First", result.Name);
        var root = Assert.IsType<PositionableGroup>(result.Data);
        Assert.Equal("First", root.Name);
        Assert.Empty(root.Children);
    }

    [Fact]
    public void Merge_KeepsElementsFromBothFavoritesGroupedByTheirParent()
    {
        var first = FavoriteWith("First", Group("Group A", MeshElement("meshA")));
        var second = FavoriteWith("Second", Group("Group B", MeshElement("meshB", x: 10)));

        var result = WorldBuilderMergingService.Merge(first, second);

        Assert.Equal(["Group A", "Group B"], GroupNames(result));
        Assert.Equal(["Group A/meshA", "Group B/meshB"], ElementPaths(result));
    }

    [Fact]
    public void Merge_CombinesElementsThatShareAParentGroupName()
    {
        var first = FavoriteWith("First", Group("Shared", MeshElement("meshA")));
        var second = FavoriteWith("Second", Group("Shared", MeshElement("meshB", x: 10)));

        var result = WorldBuilderMergingService.Merge(first, second);

        Assert.Equal(["Shared"], GroupNames(result));
        Assert.Equal(["Shared/meshA", "Shared/meshB"], ElementPaths(result));
    }

    [Fact]
    public void Merge_GroupNameCollisionsWithinOneFavoriteEndUpInASingleGroup()
    {
        var favorite = FavoriteWith("Favorite",
            Group("Same", MeshElement("meshA")),
            Group("Same", MeshElement("meshB", x: 10)));

        var result = WorldBuilderMergingService.Merge(favorite, EmptyFavorite("Empty"));

        Assert.Equal(["Same"], GroupNames(result));
        Assert.Equal(["Same/meshA", "Same/meshB"], ElementPaths(result));
    }

    [Fact]
    public void Merge_DuplicateElementsAcrossFavoritesAppearOnlyOnce()
    {
        var first = FavoriteWith("First", Group("Group", MeshElement("meshA"), MeshElement("meshB", x: 5)));
        var second = FavoriteWith("Second", Group("Group", MeshElement("meshA")));

        var result = WorldBuilderMergingService.Merge(first, second);

        Assert.Equal(["Group/meshA", "Group/meshB"], ElementPaths(result));
    }

    [Fact]
    public void Merge_DuplicateElementsWithinOneFavoriteAppearOnlyOnce()
    {
        var favorite = FavoriteWith("Favorite", Group("Group", MeshElement("meshA"), MeshElement("meshA")));

        var result = WorldBuilderMergingService.Merge(favorite, EmptyFavorite("Empty"));

        Assert.Equal(["Group/meshA"], ElementPaths(result));
    }

    [Fact]
    public void Merge_SameFavoriteMergedWithItselfKeepsEachElementOnce()
    {
        var favorite = FavoriteWith("Favorite", Group("Group", MeshElement("meshA"), MeshElement("meshB", x: 5)));

        var result = WorldBuilderMergingService.Merge(favorite, favorite);

        Assert.Equal(["Group/meshA", "Group/meshB"], ElementPaths(result));
    }

    [Fact]
    public void Merge_KeepsDuplicateElementUnderTheParentOfTheFirstFavorite()
    {
        // Parent names are not part of the identity hash, so the first occurrence wins.
        var first = FavoriteWith("First", Group("First Parent", MeshElement("shared")));
        var second = FavoriteWith("Second", Group("Second Parent", MeshElement("shared")));

        var result = WorldBuilderMergingService.Merge(first, second);

        Assert.Equal(["First Parent"], GroupNames(result));
        Assert.Equal(["First Parent/shared"], ElementPaths(result));
    }

    [Fact]
    public void Merge_IsCommutativeForTheSetOfGroupedElements()
    {
        var first = FavoriteWith("First",
            Group("Group A", MeshElement("meshA")),
            Group("Group B", LightElement("lightA")));
        var second = FavoriteWith("Second",
            Group("Group A", MeshElement("meshB", x: 7)),
            Group("Group C", MeshElement("meshC", x: 8)));

        var forward = ElementPaths(WorldBuilderMergingService.Merge(first, second));
        var reverse = ElementPaths(WorldBuilderMergingService.Merge(second, first));

        Assert.Equal(forward, reverse);
    }

    [Fact]
    public void Merge_IgnoresRootLevelElementsAndNestedGroups()
    {
        var favorite = new Favorite
        {
            Name = "Nested",
            Data = new Positionable
            {
                Children =
                {
                    MeshElement("root level"),
                    new Positionable { Name = "plain positionable" },
                    Group("Outer", Group("Inner", MeshElement("nested")))
                }
            }
        };

        var result = WorldBuilderMergingService.Merge(favorite, EmptyFavorite("Empty"));

        Assert.Empty(result.Data.Children);
    }

    [Fact]
    public void Merge_PreservesElementDataInTheResult()
    {
        var element = MeshElement("mesh", x: 1.5f, y: -2.25f, z: 3f);
        var favorite = FavoriteWith("Favorite", Group("Group", element));

        var result = WorldBuilderMergingService.Merge(favorite, EmptyFavorite("Empty"));

        var merged = Assert.IsType<SpawnableElement>(Assert.Single(Assert.Single(result.Data.Children).Children));
        Assert.Equal("mesh", merged.Name);
        Assert.NotSame(element, merged);
        var spawnable = Assert.IsType<Mesh>(merged.Spawnable);
        Assert.Equal(MeshResource, spawnable.ResourcePath);
        Assert.Equal(1.5f, spawnable.Position.x);
        Assert.Equal(-2.25f, spawnable.Position.y);
        Assert.Equal(3f, spawnable.Position.z);
    }

    [Fact]
    public void Merge_ElementsDifferingOnlyInNameAreKeptSeparate()
    {
        AssertMergeKeepsBoth(MeshElement("meshA"), MeshElement("meshB"));
    }

    [Fact]
    public void Merge_ElementsDifferingOnlyInResourcePathAreKeptSeparate()
    {
        AssertMergeKeepsBoth(
            MeshElement("mesh", resourcePath: @"base\fx\meshes\cube_debug.mesh"),
            MeshElement("mesh", resourcePath: @"base\fx\meshes\sphere_debug.mesh"));
    }

    [Fact]
    public void Merge_ElementsDifferingOnlyInSpawnablePositionAreKeptSeparate()
    {
        AssertMergeKeepsBoth(MeshElement("mesh"), MeshElement("mesh", x: 1));
    }

    [Fact]
    public void Merge_ElementsDifferingOnlyInElementPositionAreKeptSeparate()
    {
        var first = MeshElement("mesh");
        var second = MeshElement("mesh");
        second.Position = new Vector4(0, 1, 0, 1);

        AssertMergeKeepsBoth(first, second);
    }

    [Fact]
    public void Merge_ElementsDifferingOnlyInRotationAreKeptSeparate()
    {
        var first = MeshElement("mesh");
        var second = MeshElement("mesh");
        ((Mesh)second.Spawnable).EulerRotation = new EulerAngles(0, 90, 0);

        AssertMergeKeepsBoth(first, second);
    }

    [Fact]
    public void Merge_ElementsDifferingOnlyInMeshShadowSettingsAreKeptSeparate()
    {
        var first = MeshElement("mesh");
        var second = MeshElement("mesh");
        ((Mesh)second.Spawnable).CastShadows = (WEnums.shadowsShadowCastingMode)1;

        AssertMergeKeepsBoth(first, second);
    }

    [Fact]
    public void Merge_ElementsDifferingOnlyInLightIntensityAreKeptSeparate()
    {
        AssertMergeKeepsBoth(LightElement("light", intensity: 100), LightElement("light", intensity: 200));
    }

    [Fact]
    public void Merge_ElementsDifferingOnlyInEntityDataAreKeptSeparate()
    {
        var first = ElementOf("entity", new Entity());
        var second = ElementOf("entity", new Entity { AppearanceIndex = 1 });

        AssertMergeKeepsBoth(first, second);
    }

    [Fact]
    public void Merge_HandlesEverySpawnableType()
    {
        var elements = new[]
        {
            ElementOf("mesh", new Mesh()),
            ElementOf("cloth", new ClothMesh()),
            ElementOf("rotating", new RotatingMesh()),
            ElementOf("dynamic", new DynamicMesh()),
            ElementOf("water", new WaterPatch()),
            ElementOf("decal", new Decal()),
            ElementOf("effect", new Effect()),
            ElementOf("particle", new Particle()),
            ElementOf("light", new Light()),
            ElementOf("entity", new Entity())
        };
        var favorite = FavoriteWith("Favorite", Group("Group", elements));

        var result = WorldBuilderMergingService.Merge(favorite, favorite);

        Assert.Equal(elements.Length, ElementPaths(result).Length);
    }

    [Fact]
    public void Merge_TreatsNegativeZeroAndPositiveZeroPositionsAsEqual()
    {
        var first = MeshElement("mesh");
        var second = MeshElement("mesh");
        ((Mesh)second.Spawnable).Position = new Vector4(-0f, 0f, 0f, 1);

        var result = WorldBuilderMergingService.Merge(
            FavoriteWith("First", Group("Group", first)),
            FavoriteWith("Second", Group("Group", second)));

        Assert.Equal(["Group/mesh"], ElementPaths(result));
    }

    [Fact]
    public void Merge_DoesNotMutateInputFavorites()
    {
        var first = FavoriteWith("First", Group("Group A", MeshElement("meshA")));
        var second = FavoriteWith("Second", Group("Group B", MeshElement("meshB", x: 4)));
        var firstBefore = Serialize(first);
        var secondBefore = Serialize(second);

        WorldBuilderMergingService.Merge(first, second);

        Assert.Equal(firstBefore, Serialize(first));
        Assert.Equal(secondBefore, Serialize(second));
    }

    #endregion

    #region Subtract

    [Fact]
    public void Subtract_RemovesMatchingElementsAndKeepsTheRest()
    {
        var baseFavorite = FavoriteWith("Base",
            Group("Group", MeshElement("keep"), MeshElement("remove", x: 5)));
        var subtraction = FavoriteWith("Subtraction", Group("Group", MeshElement("remove", x: 5)));

        var result = WorldBuilderMergingService.Subtract(baseFavorite, subtraction);

        Assert.Equal(["Group/keep"], ElementPaths(result));
    }

    [Fact]
    public void Subtract_ReturnsEmptyRootGroupWhenEverythingIsRemoved()
    {
        var element = MeshElement("mesh");
        var baseFavorite = FavoriteWith("Base", Group("Group", element));

        var result = WorldBuilderMergingService.Subtract(baseFavorite, FavoriteWith("Sub", Group("Group", element)));

        Assert.Equal("Base", result.Name);
        Assert.Empty(result.Data.Children);
    }

    [Fact]
    public void Subtract_DropsGroupsThatOnlyContainedRemovedElements()
    {
        var baseFavorite = FavoriteWith("Base",
            Group("Removed Group", MeshElement("meshA")),
            Group("Kept Group", MeshElement("meshB", x: 5)));

        var result = WorldBuilderMergingService.Subtract(baseFavorite,
            FavoriteWith("Sub", Group("Removed Group", MeshElement("meshA"))));

        Assert.Equal(["Kept Group"], GroupNames(result));
        Assert.Equal(["Kept Group/meshB"], ElementPaths(result));
    }

    [Fact]
    public void Subtract_RemovesElementEvenWhenItsParentNameDiffers()
    {
        var baseFavorite = FavoriteWith("Base", Group("Base Parent", MeshElement("shared")));
        var subtraction = FavoriteWith("Sub", Group("Other Parent", MeshElement("shared")));

        var result = WorldBuilderMergingService.Subtract(baseFavorite, subtraction);

        Assert.Empty(result.Data.Children);
    }

    [Fact]
    public void Subtract_KeepsElementsThatDifferInAnyHashedField()
    {
        var baseFavorite = FavoriteWith("Base", Group("Group", MeshElement("mesh", x: 1)));

        var result = WorldBuilderMergingService.Subtract(baseFavorite,
            FavoriteWith("Sub", Group("Group", MeshElement("mesh", x: 2))));

        Assert.Equal(["Group/mesh"], ElementPaths(result));
    }

    [Fact]
    public void Subtract_CollapsesDuplicateElementsInTheBaseFavorite()
    {
        var baseFavorite = FavoriteWith("Base", Group("Group", MeshElement("mesh"), MeshElement("mesh")));

        var result = WorldBuilderMergingService.Subtract(baseFavorite, EmptyFavorite("Empty"));

        Assert.Equal(["Group/mesh"], ElementPaths(result));
    }

    [Fact]
    public void Subtract_WithEmptySubtractionFavoriteReturnsAllDistinctBaseElements()
    {
        var baseFavorite = FavoriteWith("Base",
            Group("Group A", MeshElement("meshA")),
            Group("Group B", MeshElement("meshB", x: 5)));

        var result = WorldBuilderMergingService.Subtract(baseFavorite, EmptyFavorite("Empty"));

        Assert.Equal(["Group A/meshA", "Group B/meshB"], ElementPaths(result));
    }

    [Fact]
    public void Subtract_EmptyBaseFavoriteReturnsEmptyRootGroup()
    {
        var result = WorldBuilderMergingService.Subtract(
            EmptyFavorite("Base"),
            FavoriteWith("Sub", Group("Group", MeshElement("mesh"))));

        Assert.Equal("Base", result.Name);
        Assert.Empty(result.Data.Children);
    }

    [Fact]
    public void Subtract_SubtractingAFavoriteFromItselfReturnsEmptyRootGroup()
    {
        var favorite = FavoriteWith("Favorite", Group("Group", MeshElement("meshA"), MeshElement("meshB", x: 5)));

        var result = WorldBuilderMergingService.Subtract(favorite, favorite);

        Assert.Equal("Favorite", result.Name);
        Assert.Empty(result.Data.Children);
    }

    [Fact]
    public void Subtract_IgnoresRootLevelElementsAndNestedGroups()
    {
        var baseFavorite = new Favorite
        {
            Name = "Base",
            Data = new Positionable
            {
                Children =
                {
                    MeshElement("root level"),
                    Group("Outer", Group("Inner", MeshElement("nested")))
                }
            }
        };
        var subtraction = new Favorite
        {
            Name = "Sub",
            Data = new Positionable
            {
                Children =
                {
                    MeshElement("root level"),
                    Group("Outer", Group("Inner", MeshElement("nested")))
                }
            }
        };

        var result = WorldBuilderMergingService.Subtract(baseFavorite, subtraction);

        Assert.Empty(result.Data.Children);
    }

    [Fact]
    public void Subtract_TreatsNegativeZeroAndPositiveZeroPositionsAsEqual()
    {
        var first = MeshElement("mesh");
        var second = MeshElement("mesh");
        ((Mesh)second.Spawnable).Position = new Vector4(-0f, 0f, 0f, 1);

        var result = WorldBuilderMergingService.Subtract(
            FavoriteWith("Base", Group("Group", first)),
            FavoriteWith("Sub", Group("Group", second)));

        Assert.Empty(result.Data.Children);
    }

    [Fact]
    public void Subtract_DoesNotMutateInputFavorites()
    {
        var baseFavorite = FavoriteWith("Base", Group("Group A", MeshElement("meshA")));
        var subtraction = FavoriteWith("Sub", Group("Group B", MeshElement("meshB", x: 4)));
        var baseBefore = Serialize(baseFavorite);
        var subtractionBefore = Serialize(subtraction);

        WorldBuilderMergingService.Subtract(baseFavorite, subtraction);

        Assert.Equal(baseBefore, Serialize(baseFavorite));
        Assert.Equal(subtractionBefore, Serialize(subtraction));
    }

    #endregion

    #region Helpers

    private static void AssertMergeKeepsBoth(SpawnableElement first, SpawnableElement second)
    {
        var result = WorldBuilderMergingService.Merge(
            FavoriteWith("First", Group("Group", first)),
            FavoriteWith("Second", Group("Group", second)));

        Assert.Equal(2, ElementPaths(result).Length);
    }

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

    private static SpawnableElement MeshElement(
        string name,
        float x = 0,
        float y = 0,
        float z = 0,
        string resourcePath = MeshResource)
        => ElementOf(name, new Mesh
        {
            ResourcePath = resourcePath,
            Position = new Vector4(x, y, z, 1)
        });

    private static SpawnableElement LightElement(string name, float intensity = 100) => ElementOf(name, new Light
    {
        Intensity = intensity
    });

    private static SpawnableElement ElementOf(string name, Spawnable spawnable)
    {
        spawnable.EulerRotation = new EulerAngles();
        return new SpawnableElement
        {
            Name = name,
            Spawnable = spawnable
        };
    }

    private static string[] GroupNames(Favorite favorite) =>
        favorite.Data.Children
            .OfType<PositionableGroup>()
            .Select(x => x.Name)
            .ToArray();

    private static string[] ElementPaths(Favorite favorite) =>
        favorite.Data.Children
            .OfType<PositionableGroup>()
            .SelectMany(group => group.Children
                .OfType<SpawnableElement>()
                .Select(element => $"{group.Name}/{element.Name}"))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

    private static string Serialize(Favorite favorite) =>
        JsonConvert.SerializeObject(favorite, JsonSerializerPresets.WorldBuilder);

    #endregion
}
