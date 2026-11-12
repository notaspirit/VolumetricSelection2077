using VolumetricSelection2077.Core.Components;

namespace VolumetricSelection2077.Core.Services;

public class ProgressService : IProgressService
{
    private static readonly object Lock = new();
    private List<IProgressSection> _sections = [];
    private Dictionary<string, IProgressSection> _sectionMap = new();
    private uint _weightSum = 0;
    
    public void Reset()
    {
        lock (Lock)
        {
            foreach (var section in _sections)
            {
                section.Target = 0;
                section.Current = 0;
            }
        }
    }

    public void Clear()
    {
        lock (Lock)
        {
            _sections.Clear();
            _sectionMap.Clear();
            _weightSum = 0;
        }
    }

    public void AddProgress(string sectionId, uint progress)
    {
        lock (Lock)
        {
            if (!_sectionMap.TryGetValue(sectionId, out var section))
                return;
        
            section.Current += progress;
        }
    }

    public void AddTarget(string sectionId, uint target)
    {
        lock (Lock)
        {
            if (!_sectionMap.TryGetValue(sectionId, out var section))
                return;
        
            section.Target += target;
        }
    }

    public void AddSection(IProgressSection section)
    {
        if (_sectionMap.ContainsKey(section.Id))
            return;
        
        lock (Lock)
        {
            _sections.Add(section);
            _sectionMap.Add(section.Id, section);
            _weightSum += section.Weight;
        }
    }

    public float Progress
    {
        get
        {
            if (_weightSum == 0) return 0;
            float total = 0;
            foreach (var section in _sections)
            {
                total += section.Progress * section.Weight / _weightSum;
                if (section is { BlockingDownstreamUpdates: true, IsCompleted: false })
                    break;
            }
            return total;
        }
    }
}