using VolumetricSelection2077.Core.Models;
using WolvenKit.RED4.CR2W.Archive;

namespace VolumetricSelection2077.Core.Services;

/// <summary>
/// Provides access to game archives and the resources and PhysX collision meshes stored within them.
/// </summary>
public interface IGameFileService
{
    bool IsInitialized { get; }

    ArchiveManager ArchiveManager { get; }

    void Initialize();

    Task<Result<VSMesh>> GetPhysXMesh(ulong sectorHash, ulong actorHash);

    Result GetResource(string path, Type type);

    Result<T> GetResource<T>(string path);
}
