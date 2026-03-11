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
    private volatile bool _active;
    private bool _registered;

    public ThreadProxyRenderTimer(IRenderTimer inner, int maxStackSize = 1 * 1024 * 1024)
    {
        _inner = inner;
        _stopwatch = new Stopwatch();
        _autoResetEvent = new AutoResetEvent(false);
        _timerThread = new Thread(RenderTimerThreadFunc, maxStackSize) { Name = "RenderTimerLoop", IsBackground = true };
    }

    public Action<TimeSpan>? Tick { get; set; }

    public bool RunsInBackground => true;

    public void Start()
    {
        _active = true;
        EnsureStarted();
        _inner.Tick = InnerTick;
        _inner.Start();
    }

    public void Stop()
    {
        // Don't call _inner.Stop() here — may be on the wrong thread.
        // InnerTick will detect _active=false and call _inner.Stop() on the correct thread.
        _active = false;
    }

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
        if (!_active)
        {
            _inner.Stop();
            return;
        }
        _autoResetEvent.Set();
    }

    private void RenderTimerThreadFunc()
    {
        while (_autoResetEvent.WaitOne())
        {
            Tick?.Invoke(_stopwatch.Elapsed);
        }
    }
}
