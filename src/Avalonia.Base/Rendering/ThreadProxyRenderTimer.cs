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
    private Action<TimeSpan>? _tick;
    private int _subscriberCount;
    private bool _registered;

    public ThreadProxyRenderTimer(IRenderTimer inner, int maxStackSize = 1 * 1024 * 1024)
    {
        _inner = inner;
        _stopwatch = new Stopwatch();
        _autoResetEvent = new AutoResetEvent(false);
        _timerThread = new Thread(RenderTimerThreadFunc, maxStackSize) { Name = "RenderTimerLoop", IsBackground = true };
    }

    public event Action<TimeSpan> Tick
    {
        add
        {
            _tick += value;

            if (!_registered)
            {
                _registered = true;
                _timerThread.Start();
            }

            if (_subscriberCount++ == 0)
            {
                _inner.Tick += InnerTick;
            }
        }

        remove
        {
            if (--_subscriberCount == 0)
            {
                _inner.Tick -= InnerTick;
            }

            _tick -= value;
        }
    }

    private void RenderTimerThreadFunc()
    {
        while (_autoResetEvent.WaitOne())
        {
            _tick?.Invoke(_stopwatch.Elapsed);
        }
    }
    
    private void InnerTick(TimeSpan obj)
    {
        _autoResetEvent.Set();
    }

    public bool RunsInBackground => true;
}
