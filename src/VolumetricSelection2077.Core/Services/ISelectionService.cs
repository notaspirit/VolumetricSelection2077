using VolumetricSelection2077.Core.Models;

namespace VolumetricSelection2077.Core.Services;

public interface ISelectionService
{
    Result<AxlRemovalFile> FindMatching(SelectionConfig config);
}