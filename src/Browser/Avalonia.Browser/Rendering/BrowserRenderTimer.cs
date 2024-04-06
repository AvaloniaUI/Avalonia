using System;
using System.Diagnostics;
using Avalonia.Browser.Interop;
using Avalonia.Rendering;

namespace Avalonia.Browser.Rendering;

internal class BrowserRenderTimer : IRenderTimer
{
    private Action<TimeSpan>? _tick;

    public BrowserRenderTimer(bool isBackground)
    {
        RunsInBackground = isBackground;
    }

    public bool RunsInBackground { get; }

    public event Action<TimeSpan>? Tick
    {
        add
        {
            if (_tick is null)
            {
                TimerHelper.RunAnimationFrames(RenderFrameCallback);
            }

            _tick += value;
        }
        remove
        {
            _tick -= value;
        }
    }

    private bool RenderFrameCallback(double timestamp)
    {
        if (_tick is { } tick)
        {
            tick.Invoke(TimeSpan.FromMilliseconds(timestamp));
            return true;
        }

        return false;
    }
}
