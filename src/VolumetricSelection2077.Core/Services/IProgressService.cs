using VolumetricSelection2077.Core.Components;

namespace VolumetricSelection2077.Core.Services;

public interface IProgressService
{
    void Reset();
    void Clear();
    void AddProgress(string sectionId, uint progress);
    void AddTarget(string section, uint target);
    void AddSection(IProgressSection section);
    public float Progress { get; }
}