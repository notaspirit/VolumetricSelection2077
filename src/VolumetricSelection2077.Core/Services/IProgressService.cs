using VolumetricSelection2077.Core.Components;

namespace VolumetricSelection2077.Core.Services;

/// <summary>
/// Tracks weighted progress across one or more <see cref="IProgressSection"/> instances.
/// </summary>
public interface IProgressService
{
    /// <summary>
    /// Resets the <see cref="IProgressSection.Target"/> and <see cref="IProgressSection.Current"/>
    /// values of all registered sections to zero, keeping the sections themselves registered.
    /// </summary>
    void Reset();

    /// <summary>
    /// Removes all registered sections and resets the accumulated weight sum.
    /// </summary>
    void Clear();

    /// <summary>
    /// Adds the given amount to the <see cref="IProgressSection.Current"/> value of the section
    /// identified by <paramref name="sectionId"/>. Does nothing if no such section is registered.
    /// </summary>
    /// <param name="sectionId">The identifier of the target section.</param>
    /// <param name="progress">The amount of progress to add.</param>
    void AddProgress(string sectionId, uint progress);

    /// <summary>
    /// Adds the given amount to the <see cref="IProgressSection.Target"/> value of the section
    /// identified by <paramref name="sectionId"/>. Does nothing if no such section is registered.
    /// </summary>
    /// <param name="sectionId">The identifier of the target section.</param>
    /// <param name="target">The amount of work to add to the section's target.</param>
    void AddTarget(string sectionId, uint target);

    /// <summary>
    /// Registers a section so that its progress contributes to the overall progress.
    /// If a section with the same <see cref="IProgressSection.Id"/> is already registered, it is ignored.
    /// </summary>
    /// <param name="section">The section to register.</param>
    void AddSection(IProgressSection section);

    /// <summary>
    /// Gets the overall weighted progress in the range 0 to 1.
    /// Progress stops accumulating at the first registered section that blocks downstream updates
    /// and is not yet completed.
    /// </summary>
    float Progress { get; }
}
