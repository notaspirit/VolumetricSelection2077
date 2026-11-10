namespace VolumetricSelection2077.Core.Models;

public class CacheDatabaseMetadata
{
    public uint DatabaseVersion { get; set; }
    public string GameVersion { get; set; }
    public bool AreVanillaSectorBBsBuild { get; set; }
    
    public CacheDatabaseMetadata(uint databaseVersion, string gameVersion)
    { 
        DatabaseVersion = databaseVersion;
        GameVersion = gameVersion;
        AreVanillaSectorBBsBuild = false;
    }
}