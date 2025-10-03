using VolumetricSelection2077.Enums;

namespace VolumetricSelection2077.Models;

public class ReadCacheRequest
{
    public CacheDatabases Database { get; set; }
    public string Key { get; set; }
    public ReadCacheRequest(string key, CacheDatabases database = CacheDatabases.Vanilla)
    {
        Key = key;
        Database = database;
    }
}