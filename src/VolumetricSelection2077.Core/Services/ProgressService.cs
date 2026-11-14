using VolumetricSelection2077.Core.Components;

namespace VolumetricSelection2077.Core.Services;

/// <summary>
/// Thread-safe implementation of <see cref="IProgressService"/> that aggregates the weighted
/// progress of registered <see cref="IProgressSection"/> instances.
/// </summary>
public class ProgressService : IProgressService
{
    private static readonly object Lock = new();
    private List<IProgressSection> _sections = [];
    private Dictionary<string, IProgressSection> _sectionMap = new();
    private uint _weightSum = 0;

    /// <summary>
    /// Resets the <see cref="IProgressSection.Target"/> and <see cref="IProgressSection.Current"/>
    /// values of all registered sections to zero, keeping the sections themselves registered.
    /// </summary>
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

    /// <summary>
    /// Removes all registered sections and resets the accumulated weight sum.
    /// </summary>
    public void Clear()
    {
        lock (Lock)
        {
            _sections.Clear();
            _sectionMap.Clear();
            _weightSum = 0;
        }
    }

    /// <summary>
    /// Adds the given amount to the <see cref="IProgressSection.Current"/> value of the section
    /// identified by <paramref name="sectionId"/>. Does nothing if no such section is registered.
    /// </summary>
    /// <param name="sectionId">The identifier of the target section.</param>
    /// <param name="progress">The amount of progress to add.</param>
    public void AddProgress(string sectionId, uint progress)
    {
        lock (Lock)
        {
            if (!_sectionMap.TryGetValue(sectionId, out var section))
                return;

            section.Current += progress;
        }
    }

    /// <summary>
    /// Adds the given amount to the <see cref="IProgressSection.Target"/> value of the section
    /// identified by <paramref name="sectionId"/>. Does nothing if no such section is registered.
    /// </summary>
    /// <param name="sectionId">The identifier of the target section.</param>
    /// <param name="target">The amount of work to add to the section's target.</param>
    public void AddTarget(string sectionId, uint target)
    {
        lock (Lock)
        {
            if (!_sectionMap.TryGetValue(sectionId, out var sectionEntry))
                return;

            sectionEntry.Target += target;
        }
    }

    /// <summary>
    /// Registers a section so that its progress contributes to the overall progress.
    /// If a section with the same <see cref="IProgressSection.Id"/> is already registered, it is ignored.
    /// </summary>
    /// <param name="section">The section to register.</param>
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

    /// <summary>
    /// Gets the overall weighted progress in the range 0 to 1.
    /// Progress stops accumulating at the first registered section that blocks downstream updates
    /// and is not yet completed.
    /// </summary>
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
