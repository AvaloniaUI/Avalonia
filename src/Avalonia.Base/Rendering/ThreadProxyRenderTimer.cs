using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Metadata;

namespace Avalonia.Rendering;

[PrivateApi]
public sealed class ThreadProxyRenderTimer : IRenderTimer
{
    private readonly IRenderTimer _inner;
    private readonly Stopwatch _stopwatch;
    private readonly Thread _timerThread;
    private readonly AutoResetEvent _autoResetEvent;
    private readonly object _lock = new();
    private volatile Action<TimeSpan>? _tick;
    private volatile bool _active;
    private bool _registered;

    public ThreadProxyRenderTimer(IRenderTimer inner, int maxStackSize = 1 * 1024 * 1024)
    {
        _inner = inner;
        _stopwatch = new Stopwatch();
        _autoResetEvent = new AutoResetEvent(false);
        _timerThread = new Thread(RenderTimerThreadFunc, maxStackSize) { Name = "RenderTimerLoop", IsBackground = true };
    }

    public Action<TimeSpan>? Tick
    {
        get => _tick;
        set
        {
            lock (_lock)
            {
                if (value != null)
                {
                    _tick = value;
                    _active = true;
                    EnsureStarted();
                    _inner.Tick = InnerTick;
                }
                else
                {
                    // Don't set _inner.Tick = null here — may be on the wrong thread.
                    // InnerTick will detect _active=false and clear _inner.Tick on the correct thread.
                    _active = false;
                    _tick = null;
                }
            }
        }
    }

    public bool RunsInBackground => true;

    private void EnsureStarted()
    {
        if (!_registered)
        {
            _registered = true;
            _stopwatch.Start();
            _timerThread.Start();
        }
    }

    private void InnerTick(TimeSpan obj)
    {
        lock (_lock)
        {
            if (!_active)
            {
                _inner.Tick = null;
                return;
            }
        }
        _autoResetEvent.Set();
    }

    private void RenderTimerThreadFunc()
    {
        while (_autoResetEvent.WaitOne())
        {
            _tick?.Invoke(_stopwatch.Elapsed);
        }
    }
}
