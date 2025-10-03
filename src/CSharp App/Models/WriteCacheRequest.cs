using VolumetricSelection2077.Enums;

namespace VolumetricSelection2077.Models;

public class WriteCacheRequest
{
    public CacheDatabases Database { get; set; }
    public string Key { get; set; }
    public byte[] Data { get; set; }
    public WriteCacheRequest(string key, byte[] data, CacheDatabases database = CacheDatabases.Vanilla)
    {
        Key = key;
        Data = data;
        Database = database;
    }
}