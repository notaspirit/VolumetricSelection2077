using System;
using System.Collections.Generic;
using Avalonia.Threading;
using Tmds.DBus.Protocol;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.Models;

public class ProgressSection
{
    private int _targetCount = 1;
    private int _currentCount = 0;
    private bool offsetTargetCount = true;
    private static readonly object _lock = new object();
    public int ProgressPercentage => (int)((float)_currentCount / _targetCount * 1000);

    /// <summary>
    /// Adds to the target that represents 100%
    /// </summary>
    /// <param name="targetCount">int to add to current target</param>
    public void AddTarget(int targetCount)
    {
        lock (_lock)
        {
            var previousPercent = ProgressPercentage;
            _targetCount += targetCount;
            if (offsetTargetCount && targetCount > 0)
            {
                _targetCount -= 1;
                offsetTargetCount = false;
            }
            if (previousPercent == 1000)
                FulfilledChanged?.Invoke(this, false);
        }
    }

    /// <summary>
    /// Adds to the current status 
    /// </summary>
    /// <param name="currentCount">count to add to current status</param>
    public void AddCurrent(int currentCount)
    {
        lock (_lock)
        {
            _currentCount += currentCount;
            if (_currentCount == _targetCount)
                FulfilledChanged?.Invoke(this, true);
        }
    }

    /// <summary>
    /// Resets target and current count
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _currentCount = 0;
            _targetCount = 1;
            offsetTargetCount = true;
        }
    }
    
    /// <summary>
    /// Fires when the status of fulfillment has changed, is true if new status is fulfilled, false if it isn't
    /// </summary>
    public event EventHandler<bool>? FulfilledChanged;

}

public class Progress
{
    public enum ProgressSections
    {
        Startup,
        Processing,
        Finalization
    }
    
    private static Progress? _instance;
    private static readonly object _lock = new object();
    private static readonly object _eventLock = new object();
    private Dictionary<ProgressSections, ProgressSection> _sections = new() { { ProgressSections.Startup, new ProgressSection()},
        { ProgressSections.Processing, new ProgressSection()},
        { ProgressSections.Finalization, new ProgressSection()} };

    private float _StartupWeight = 0.1f;
    private float _ProcessingWeight = 0.8f;
    private float _FinalizationWeight = 0.1f;

    private bool isProcessSuppressed = true;
    
    public int ProgressPercentage => (int)((_sections[ProgressSections.Startup].ProgressPercentage * _StartupWeight) +
                                           (isProcessSuppressed ? 0 : _sections[ProgressSections.Processing].ProgressPercentage * _ProcessingWeight) +
                                           (_sections[ProgressSections.Finalization].ProgressPercentage * _FinalizationWeight));
    
    /// <summary>
    /// Fires when progress has changed, int represents the % in X / 1000
    /// </summary>
    public event EventHandler<int>? ProgressChanged;

    private Progress()
    {
        _sections[ProgressSections.Startup].FulfilledChanged += (_, result) =>
        {
            isProcessSuppressed = !result;
        };
    }
    
    /// <summary>
    /// Get Singleton instance
    /// </summary>
    public static Progress Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new Progress();
                }

                return _instance;
            }
        }
    }
    
    /// <summary>
    /// Adds to the target that represents 100%
    /// </summary>
    /// <param name="targetCount">int to add to current target</param>
    /// <param name="section">the section to add to</param>
    public void AddTarget(int targetCount, ProgressSections section)
    {
        _sections[section].AddTarget(targetCount);
        InvokeProgressChanged();
    }

    /// <summary>
    /// Adds to the current status 
    /// </summary>
    /// <param name="currentCount">count to add to current status</param>
    /// /// <param name="section">the section to add to</param>
    public void AddCurrent(int currentCount, ProgressSections section)
    {
        _sections[section].AddCurrent(currentCount);
        InvokeProgressChanged();
    }

    /// <summary>
    /// Resets target and current count
    /// </summary>
    public void Reset()
    {
        foreach (var section in _sections)
        {
            section.Value.Reset();
        }
        isProcessSuppressed = true;
        InvokeProgressChanged();
    }

    /// <summary>
    /// Sets the weighting of each section, this should be called just after resetting when cleaning up the progress for a new run
    /// </summary>
    /// <param name="startupWeight"></param>
    /// <param name="processingWeight"></param>
    /// <param name="finalizationWeight"></param>
    /// <exception cref="ArgumentException">if sum of weights isn't 1 or any weight is less than 0</exception>
    public void SetWeight(float startupWeight, float processingWeight, float finalizationWeight)
    {
        if (startupWeight + processingWeight + finalizationWeight != 1)
            throw new ArgumentException("Weights must add up to 1");
        if (startupWeight < 0 || processingWeight < 0 || finalizationWeight < 0)
            throw new ArgumentException("Weights cannot be less than 0");
        _StartupWeight = startupWeight;
        _ProcessingWeight = processingWeight;
        _FinalizationWeight = finalizationWeight;
        InvokeProgressChanged();
    }
    
    /// <summary>
    /// Event to update the UI
    /// </summary>
    private void InvokeProgressChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            lock (_eventLock)
            {
                ProgressChanged?.Invoke(this, ProgressPercentage);
            }
        });
    }
    
}