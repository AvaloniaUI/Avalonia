using System;
using System.Threading;

namespace Avalonia.X11.Selections.DragDrop;

internal sealed class DragDropTimeoutManager : IDisposable
{
    private readonly TimeSpan _timeout;
    private readonly Timer _timer;

    public DragDropTimeoutManager(TimeSpan timeout, Action onTimeout)
    {
        _timeout = timeout;

        _timer = new Timer(
            static state => ((Action)state!)(),
            onTimeout,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);
    }

    public void Restart()
    {
        try
        {
            _timer.Change(_timeout, Timeout.InfiniteTimeSpan);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public void Dispose()
        => _timer.Dispose();
}
