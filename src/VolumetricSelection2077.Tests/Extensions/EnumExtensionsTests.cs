using VolumetricSelection2077.Core.Enums;
using VolumetricSelection2077.Core.Extensions;

namespace VolumetricSelection2077.Tests.Extensions;

public class EnumExtensionsTests
{
    [Theory]
    [InlineData(WorldDataSource.VanillaOnly, CacheDatabase.VanillaBounds)]
    [InlineData(WorldDataSource.ModdedOnly, CacheDatabase.ModdedBounds)]
    [InlineData(WorldDataSource.All, CacheDatabase.AllBounds)]
    public void ToCacheDatabaseBounds_MapsWorldDataSource(WorldDataSource source, CacheDatabase expected)
    {
        Assert.Equal(expected, source.ToCacheDatabaseBounds());
    }

    [Fact]
    public void ToCacheDatabaseBounds_UnknownValue_Throws()
    {
        const WorldDataSource source = (WorldDataSource)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => source.ToCacheDatabaseBounds());
    }
}
