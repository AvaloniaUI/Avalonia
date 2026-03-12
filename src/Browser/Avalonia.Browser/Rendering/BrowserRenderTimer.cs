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
        get => _tick;
        set
        {
            _tick = value;
            if (value != null)
                StartOnThisThread();
        }
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
        _tick?.Invoke(TimeSpan.FromMilliseconds(timestamp));
    }
}
