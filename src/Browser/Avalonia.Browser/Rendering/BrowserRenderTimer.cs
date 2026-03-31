using System;
using System.Diagnostics;
using Avalonia.Browser.Interop;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Browser.Rendering;

internal class BrowserRenderTimer : IRenderTimer
{
    private Action<TimeSpan>? _tick;
    private bool _started;

    public BrowserRenderTimer(bool isBackground)
    {
        RunsInBackground = isBackground;
    }

    public bool RunsInBackground { get; }

    public Action<TimeSpan>? Tick
    {
        set
        {
            if (!BrowserWindowingPlatform.IsThreadingEnabled)
                StartOnThisThread();

            _tick = value;
        }
        get => _tick;
    }

    public void StartOnThisThread()
    {
        if (!_started)
        {
            _started = true;
            TimerHelper.AnimationFrame += RenderFrameCallback;
            TimerHelper.RunAnimationFrames();
        }
    }

    private void RenderFrameCallback(double timestamp)
    {
        if (_tick is { } tick)
        {
            tick.Invoke(TimeSpan.FromMilliseconds(timestamp));
        }
    }
}