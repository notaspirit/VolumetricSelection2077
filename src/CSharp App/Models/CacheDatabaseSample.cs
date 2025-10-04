namespace VolumetricSelection2077.Models;

public class CacheDatabaseSample
{
    public int moddedEntriesCount { get; set; }
    public int vanillaEntriesCount { get; set; }
    public string[] moddedEntriesSample { get; set; }
    public string[] vanillaEntriesSample { get; set; }
    public CacheDatabaseSample(int moddedEntriesCount, int vanillaEntriesCount, string[] moddedEntriesSample, string[] vanillaEntriesSample)
    {
        this.moddedEntriesCount = moddedEntriesCount;
        this.vanillaEntriesCount = vanillaEntriesCount;
        this.moddedEntriesSample = moddedEntriesSample;
        this.vanillaEntriesSample = vanillaEntriesSample;
    }
}