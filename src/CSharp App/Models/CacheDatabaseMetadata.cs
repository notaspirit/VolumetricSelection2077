namespace VolumetricSelection2077.Models;

public class CacheDatabaseMetadata
{
    public string VS2077Version { get; set; }
    public string GameVersion { get; set; }
    public bool AreVanillaSectorBBsBuild { get; set; }
    public CacheDatabaseMetadata(string vs2077Version, string gameVersion)
    {
        VS2077Version = vs2077Version;
        GameVersion = gameVersion;
        AreVanillaSectorBBsBuild = false;
    }
}