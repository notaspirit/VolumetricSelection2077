using VolumetricSelection2077.Core.Enums;

namespace VolumetricSelection2077.Core.Extensions;

public static class EnumExtensions
{
    public static CacheDatabase ToCacheDatabaseBounds(this WorldDataSource wds) => wds switch
    {
        WorldDataSource.VanillaOnly => CacheDatabase.VanillaBounds,
        WorldDataSource.ModdedOnly => CacheDatabase.ModdedBounds,
        WorldDataSource.All => CacheDatabase.AllBounds,
        _ => throw new ArgumentOutOfRangeException(nameof(wds), wds, null)
    };
}