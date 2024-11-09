
using System.Threading.Tasks;
using System;

namespace VolumetricSelection2077.Services;

public class ProcessService
{
    private readonly GameFileService _gameFileService;
    private readonly CacheService _cacheService;
    private readonly SettingsService _settings;
    public ProcessService()
    {
        _gameFileService = new GameFileService();
        _cacheService = new CacheService();
        _settings = SettingsService.Instance;
    }

    public async Task<(bool success, string error)> Process()
    {
        try{

            Logger.Info("Starting process...");
            Logger.Info("Validating input...");
            if (!await ValidationService.ValidateInput(_settings.GameDirectory, _settings.OutputFilename))
            {
                return (false, "Validation failed");
            }
            Logger.Info("Checking for filemap...");
            var (success, error) = await _gameFileService.buildFileMap();
            if (!success)
            {
                return (false, error);
            }
            Logger.Success("Process complete");
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
