using System.Diagnostics;
using Avalonia.Rendering;
using Tizen.System;

namespace Avalonia.Tizen;
internal class TizenRenderTimer : IRenderTimer
{
    private readonly Stopwatch _st = Stopwatch.StartNew();
    private global::Tizen.NUI.Timer _timer;

    public bool RunsInBackground => true;

    public event Action<TimeSpan>? Tick;
    public event Action? RenderTick;

    public TizenRenderTimer()
    {
        _timer = new global::Tizen.NUI.Timer(16);
        Display.StateChanged += Display_StateChanged;

        _timer.Tick += TimerTick;
        _timer.Start();
    }

    private bool TimerTick(object source, global::Tizen.NUI.Timer.TickEventArgs e)
    {
        RenderTick?.Invoke();
        return true;
    }

    private void Display_StateChanged(object? sender, DisplayStateChangedEventArgs e)
    {
        if (e.State == DisplayState.Off)
        {
            _timer.Stop();
        }
        else if (!_timer.IsRunning())
        {
            _timer.Start();
        }
    }

    internal void ManualTick()
    {
        Tick?.Invoke(_st.Elapsed);
    }
}
