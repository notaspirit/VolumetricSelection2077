using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Core.Services;

public interface ISelectionService
{ 
    Task<Result<AxlRemovalFile>> QueryWorldData(VSQuery query);
    Task<Result<AxlRemovalSector>> QuerySector(string sectorPath, VSQuery query);
}