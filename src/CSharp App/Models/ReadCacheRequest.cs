using VolumetricSelection2077.App.Enums;

namespace VolumetricSelection2077.App.Models;

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