using System;
using System.Diagnostics;
using Avalonia.Browser.Interop;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Browser.Rendering;

internal class BrowserRenderTimer : IRenderTimer
{
    private bool _started;

    public BrowserRenderTimer(bool isBackground)
    {
        RunsInBackground = isBackground;
    }

    public bool RunsInBackground { get; }

    public Action<TimeSpan>? Tick { get; set; }

    public void Start()
    {
        StartOnThisThread();
    }

    public void Stop()
    {
        // No-op: requestAnimationFrame is one-shot per frame,
        // the render loop just won't request the next one.
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
        Tick?.Invoke(TimeSpan.FromMilliseconds(timestamp));
    }
}
