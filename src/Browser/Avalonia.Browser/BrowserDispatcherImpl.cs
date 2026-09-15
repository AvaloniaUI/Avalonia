using System;
using System.Diagnostics;
using System.Threading;

using Avalonia.Browser.Interop;
using Avalonia.Threading;

namespace Avalonia.Browser;

internal class BrowserDispatcherImpl : IDispatcherImplWithPendingInput
{
    private readonly Thread _thread;
    private readonly Stopwatch _clock;
    private bool _inInputWake;
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

        TimerHelper.Timeout = RunSignaled;
    }

    public bool CurrentThreadIsLoopThread => Thread.CurrentThread == _thread;

    public long Now => _clock.ElapsedMilliseconds;

    public event Action? Signaled;
    public event Action? Timer;

    public bool CanQueryPendingInput => true;

    /// <summary>
    /// Input that JS has queued but C# has not decoded yet.
    /// </summary>
    public bool HasPendingInput => BrowserInputQueue.HasPendingInput;

    public void Signal()
    {
        if (Interlocked.CompareExchange(ref _signaled, 1, 0) != 0)
            return;

        // RunSignaled is guaranteed to be called inside of input wake.
        if (_inInputWake)
            return;

        // NOTE: by HTML5 spec minimal timeout is 4ms, but Chrome seems to work well with 1ms as well.
        const int interval = 1;
        TimerHelper.SetTimeout(interval);
    }

    /// <summary>
    /// Runs <paramref name="drainInput"/> and with a single Signaled call.
    /// </summary>
    public void RunInputWake(Action drainInput)
    {
        _inInputWake = true;
        try
        {
            drainInput();
        }
        finally
        {
            _inInputWake = false;
        }

        RunSignaled();
    }

    private void RunSignaled()
    {
        Interlocked.Exchange(ref _signaled, 0);
        Signaled?.Invoke();
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
