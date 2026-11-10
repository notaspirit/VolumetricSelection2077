using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Core.Services;

public class SelectionService : ISelectionService
{
    private CacheService _cacheService;
    
    public SelectionService(CacheService cacheService)
    {
        _cacheService = cacheService;
    }
    
    public Result<AxlRemovalFile> FindMatching(SelectionConfig config)
    {
        var result = new Result<AxlRemovalFile>();
        
        
        
        return result;
    }
}