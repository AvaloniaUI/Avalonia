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
    /// Input that JS has queued but C# has not decoded yet. The dispatcher uses this to stop running
    /// background jobs while input is waiting. Plain memory reads, no interop.
    /// </summary>
    public bool HasPendingInput => BrowserInputQueue.HasPendingInput;

    public void Signal()
    {
        if (Interlocked.CompareExchange(ref _signaled, 1, 0) != 0)
            return;

        // Inside an input wake the Signaled handler is guaranteed to run at the end of the current
        // task, so there is no need to schedule another one.
        if (_inInputWake)
            return;

        // NOTE: by HTML5 spec minimal timeout is 4ms, but Chrome seems to work well with 1ms as well.
        const int interval = 1;
        TimerHelper.SetTimeout(interval);
    }

    /// <summary>
    /// Runs <paramref name="drainInput"/> and then the dispatcher's Signaled handler, in the current task.
    /// Signals raised while draining (the input queue posting its Input-priority job) do not schedule a
    /// timeout, so a burst of input costs exactly one macrotask.
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

    /// <summary>
    /// Runs the dispatcher's Signaled handler now, on the loop thread.
    /// A timeout scheduled by <see cref="Signal"/> that is still pending after this simply finds no work.
    /// </summary>
    private void RunSignaled()
    {
        Interlocked.Exchange(ref _signaled, 0);
        Signaled?.Invoke();
    }

    private bool _inInputWake;

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
