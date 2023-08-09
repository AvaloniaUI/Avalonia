using System.Diagnostics;
using Avalonia.Rendering;
using Tizen.Account.AccountManager;
using Tizen.System;

namespace Avalonia.Tizen;
internal class TizenRenderTimer : IRenderTimer
{
    private readonly Stopwatch _st = Stopwatch.StartNew();
    private Timer _timer;

    public bool RunsInBackground => true;

    public event Action<TimeSpan>? Tick;
    public event Action? RenderTick;

    public TizenRenderTimer()
    {
        _timer = new Timer(TimerTick, null, 16, 16);
        Display.StateChanged += Display_StateChanged;
    }

    private void TimerTick(object? state)
    {
        RenderTick?.Invoke();
    }

    private void Display_StateChanged(object? sender, DisplayStateChangedEventArgs e)
    {
        if (e.State == DisplayState.Off)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        else
        {
            _timer.Change(16, 16);
        }
    }

    internal void ManualTick()
    {
        Tick?.Invoke(_st.Elapsed);
    }
}
