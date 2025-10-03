namespace VolumetricSelection2077.Models;

public class CacheStats
{
    public long VanillaEntries { get; set; }
    public FileSize EstVanillaSize { get; set; }
    public long ModdedEntries { get; set; }
    public FileSize EstModdedSize { get; set; }

    public long VanillaBoundsEntries { get; set; }
    public FileSize EstVanillaBoundsSize { get; set; }
    public long ModdedBoundsEntries { get; set; }
    public FileSize EstModdedBoundsSize { get; set; }
        
    public CacheStats()
    {
        VanillaEntries = -1;
        EstVanillaSize = new(0);
        ModdedEntries = -1;
        EstModdedSize = new(0);
        VanillaBoundsEntries = -1;
        EstVanillaBoundsSize = new(0);
        ModdedBoundsEntries = -1;
        EstModdedBoundsSize = new(0);
    }
}