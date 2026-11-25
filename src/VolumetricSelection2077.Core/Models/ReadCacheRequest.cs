using VolumetricSelection2077.Core.Enums;

namespace VolumetricSelection2077.Core.Models;

public class ReadCacheRequest
{
    public CacheDatabase Database { get; set; }
    public string Key { get; set; }
    public ReadCacheRequest(string key, CacheDatabase database = CacheDatabase.Vanilla)
    {
        Key = key;
        Database = database;
    }
}