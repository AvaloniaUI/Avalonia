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
    private int _signaled;
    private int? _timerId;

    public BrowserDispatcherImpl()
    {
        _thread = Thread.CurrentThread;
        _clock = Stopwatch.StartNew();

        TimerHelper.Interval += () =>
        {
            Timer?.Invoke();
        };
        
        TimerHelper.Timeout = () =>
        {
            Interlocked.Exchange(ref _signaled, 0);
            Signaled?.Invoke();
        };
    }

    public bool CurrentThreadIsLoopThread => Thread.CurrentThread == _thread;

    public long Now => _clock.ElapsedMilliseconds;

    public event Action? Signaled;
    public event Action? Timer;

    public void Signal()
    {
        if (Interlocked.CompareExchange(ref _signaled, 1, 0) != 0)
            return;

        // NOTE: by HTML5 spec minimal timeout is 4ms, but Chrome seems to work well with 1ms as well.
        const int interval = 1;
        TimerHelper.SetTimeout(interval);
    }

    public void UpdateTimer(long? dueTimeInMs)
    {
        if (_timerId is { } timerId)
        {
            _timerId = null;
            TimerHelper.ClearInterval(timerId);
        }

        if (dueTimeInMs.HasValue)
        {
            var interval = Math.Max(1, dueTimeInMs.Value - _clock.ElapsedMilliseconds);
            _timerId = TimerHelper.SetInterval((int)interval);
        }
    }
}
