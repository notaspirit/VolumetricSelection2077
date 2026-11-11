using VolumetricSelection2077.Core.Enums;

namespace VolumetricSelection2077.Core.Models;

public class WriteCacheRequest
{
    public CacheDatabase Database { get; set; }
    public string Key { get; set; }
    public byte[] Data { get; set; }
    public WriteCacheRequest(string key, byte[] data, CacheDatabase database = CacheDatabase.Vanilla)
    {
        Key = key;
        Data = data;
        Database = database;
    }
}