using System;
using Avalonia.Threading;

namespace VolumetricSelection2077.Extensions;

public class TrackedDispatchTimer : DispatcherTimer
{
    private DateTime _startTime;
    private bool _isRunning;

    public double ElapsedSeconds => _isRunning ? Math.Round((DateTime.Now - _startTime).TotalSeconds) : 0;

    public new void Start()
    {
        _startTime = DateTime.Now;
        _isRunning = true;
        
        var fireTickMethod = typeof(DispatcherTimer).GetMethod("FireTick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fireTickMethod?.Invoke(this, null);
        
        base.Start();
    }

    public new void Stop()
    {
        _isRunning = false;
        base.Stop();
    }

    public void Restart()
    {
        _startTime = DateTime.Now;
        _isRunning = true;
        base.Start();
    }
}