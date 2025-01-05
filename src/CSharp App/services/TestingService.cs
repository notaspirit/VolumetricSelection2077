using System.Threading.Tasks;
using System.Text;
namespace VolumetricSelection2077.Services;

public class TestingService
{
    private readonly CacheService _cacheService;

    public TestingService()
    {
        _cacheService = new CacheService();
    }
    // It passed the save / load test, meaning last time it simply failed because the dataset didn't contain the data I was looking for, probably due to not having processed ep1 yet. 
    public (bool success, string error) SaveAndLoadDummyData()
    {
        Logger.Info("Saving dummy data...");
        string dummyKey = "SomeTestkey2";
        string dummyValue = "dummyValyueshcfs";
        var (success, error) = _cacheService.SaveEntry(CacheDatabase.FileMap.ToString(), dummyKey, Encoding.UTF8.GetBytes(dummyValue));
        if (!success)
        {
            Logger.Error($"Failed to save dummy data: {error}");
            return (false, error);
        }
        Logger.Success("Saved dummy data");
        Logger.Info($"Loading dummy data...");
        var (exists, data, error2) = _cacheService.GetEntry(CacheDatabase.FileMap.ToString(), dummyKey);
        if (!exists || data == null)
        {
            Logger.Error($"Failed to load dummy data: {error2}");
            return (false, error2);
        }
        Logger.Success("Loaded dummy data:");
        Logger.Info(Encoding.UTF8.GetString(data));
        Logger.Success("Dummy data test passed");
        return (true, "");
    }
}

