using System;
using Avalonia.Threading;
using VolumetricSelection2077.Core.Services;

namespace VolumetricSelection2077.App.Services;

public class AppProgressService : ProgressService
{
    private readonly object _lock = new();
    
    public AppProgressService()
    {
        OnProgressChanged += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                lock (_lock)
                {
                    OnProgressChangedOnUiThread?.Invoke(this, EventArgs.Empty);
                }
            });
        };
    }

    /// <summary>
    /// Event raised based on the same source as <see cref="IProgressService.OnProgressChanged"/> with the difference of running on the UI thread.
    /// </summary>
    public event EventHandler? OnProgressChangedOnUiThread;
}