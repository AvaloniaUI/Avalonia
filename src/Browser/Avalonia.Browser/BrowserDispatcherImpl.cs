using System;
using System.Diagnostics;
using System.Threading;

using Avalonia.Browser.Interop;
using Avalonia.Threading;

namespace Avalonia.Browser;

internal class BrowserDispatcherImpl : IDispatcherImpl
{
    private readonly Thread _thread;
    private readonly Stopwatch _clock;
    private bool _signaled;
    private int? _timerId;

    private readonly Action _timerCallback;
    private readonly Action _signalCallback;

    public BrowserDispatcherImpl()
    {
        _thread = Thread.CurrentThread;
        _clock = Stopwatch.StartNew();

        _timerCallback = () => Timer?.Invoke();
        _signalCallback = () =>
        {
            _signaled = false;
            Signaled?.Invoke();
        };
    }

    public bool CurrentThreadIsLoopThread => Thread.CurrentThread == _thread;

    public long Now => _clock.ElapsedMilliseconds;

    public event Action? Signaled;
    public event Action? Timer;

    public void Signal()
    {
        if (_signaled)
            return;

        // NOTE: by HTML5 spec minimal timeout is 4ms, but Chrome seems to work well with 1ms as well.
        var interval = 1;
        CanvasHelper.SetTimeout(_signalCallback, interval);
    }

    public void UpdateTimer(long? dueTimeInMs)
    {
        if (_timerId is { } timerId)
        {
            _timerId = null;
            CanvasHelper.ClearInterval(timerId);
        }

        if (dueTimeInMs.HasValue)
        {
            var interval = Math.Max(1, dueTimeInMs.Value - _clock.ElapsedMilliseconds);
            _timerId = CanvasHelper.SetInterval(_timerCallback, (int)interval);
        }
    }
}
