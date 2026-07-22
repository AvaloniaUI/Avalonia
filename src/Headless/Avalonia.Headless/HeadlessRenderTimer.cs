using System;
using System.Diagnostics;
using Avalonia.Reactive;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Headless;

/// <summary>
/// A render timer implementation for headless environments that uses a DispatcherTimer to schedule ticks on the UI thread.
/// Can be controlled with <see cref="ForceTick"/> method. 
/// </summary>
internal class HeadlessRenderTimer(int framesPerSecond) : DefaultRenderTimer(framesPerSecond)
{
    private readonly int _framesPerSecond = framesPerSecond;
    private Action? _forceTick; 
    protected override IDisposable StartCore(Action<TimeSpan> tick)
    {
        var st = Stopwatch.StartNew();
        _forceTick = () => tick(st.Elapsed);

        var timer = new DispatcherTimer(DispatcherPriority.UiThreadRender)
        {
            Interval = TimeSpan.FromSeconds(1.0 / _framesPerSecond),
            Tag = "HeadlessRenderTimer"
        };
        timer.Tick += (s, e) => tick(st.Elapsed);
        timer.Start();

        return Disposable.Create(() =>
        {
            _forceTick = null;
            timer.Stop();
        });
    }

    public override bool RunsInBackground => false;

    public void ForceTick() => _forceTick?.Invoke();
}
