using System;
using Avalonia.Threading;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.Models;

public class Progress
{
    private static Progress? _instance;
    private static readonly object _lock = new object();
    private static readonly object _eventLock = new object();
    private int _targetCount = 1;
    private int _currentCount = 0;
    private bool offsetTargetCount = true;
    public int ProgressPercentage => (int)((float)_currentCount / _targetCount * 1000);
    
    public event EventHandler<int>? ProgressChanged;
    
    private Progress()
    {
        
    }
    
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

    public void AddTarget(int targetCount)
    {
        lock (_lock)
        {
            _targetCount += targetCount;
            if (offsetTargetCount && targetCount > 0)
            {
                _targetCount -= 1;
                offsetTargetCount = false;
            }
        }
        InvokeProgressChanged();
    }

    public void AddCurrent(int currentCount)
    {
        lock (_lock)
        {
            _currentCount += currentCount;
        }
        InvokeProgressChanged();
    }

    public void Reset()
    {
        lock (_lock)
        {
            _currentCount = 0;
            _targetCount = 1;
            offsetTargetCount = true;
        }
        InvokeProgressChanged();
    }
    
    private void InvokeProgressChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            lock (_eventLock)
            {
                Logger.Info($"Called update event: target: {_targetCount}, current: {_currentCount}, progress: {ProgressPercentage}", true);
                ProgressChanged?.Invoke(this, ProgressPercentage);
            }
        });
    }
    
}