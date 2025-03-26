using System;
using System.Diagnostics;
using Avalonia.Threading;

namespace VolumetricSelection2077.Extensions;

public class TrackedDispatchTimer : DispatcherTimer
{
    private Stopwatch _stopwatch;
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <summary>
    /// Starts the stopwatch and timer, fires a "FireTick" event
    /// </summary>
    public new void Start()
    {
        _stopwatch = Stopwatch.StartNew();
        var fireTickMethod = typeof(DispatcherTimer).GetMethod("FireTick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fireTickMethod?.Invoke(this, null);
        
        base.Start();
    }
    /// <summary>
    /// Stops the stopwatch and timer
    /// </summary>
    public new void Stop()
    {
        _stopwatch.Stop();
        base.Stop();
    }
}