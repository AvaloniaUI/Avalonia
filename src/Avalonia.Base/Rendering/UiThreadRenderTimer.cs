using System;
using System.Diagnostics;
using Avalonia.Metadata;
using Avalonia.Threading;

namespace Avalonia.Rendering;

/// <summary>
/// Render timer that ticks on UI thread. Useful for debugging or bootstrapping on new platforms 
/// </summary>
[PrivateApi]
public class UiThreadRenderTimer : DefaultRenderTimer
{
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    /// <summary>
    /// Initializes a new instance of the <see cref="UiThreadRenderTimer"/> class.
    /// </summary>
    /// <param name="framesPerSecond">The number of frames per second at which the loop should run.</param>
    public UiThreadRenderTimer(int framesPerSecond) : base(framesPerSecond)
    {
    }

    /// <inheritdoc />
    public override bool RunsInBackground => false;
    
    class TimerInstance : IDisposable
    {
        private UiThreadRenderTimer _parent;
        private readonly Action<TimeSpan> _tick;
        private DispatcherTimer _timer = new DispatcherTimer(DispatcherPriority.Render);

        public TimerInstance(UiThreadRenderTimer parent, Action<TimeSpan> tick)
        {
            _parent = parent;
            _tick = tick;
            _timer.Tick += OnTick;
            _timer.Interval = Interval;
            Interval = TimeSpan.FromSeconds(1.0 / _parent.FramesPerSecond);
            _timer.Start();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            var tickedAt = _parent._clock.Elapsed;
            var nextTickAt = tickedAt + Interval;
            try
            {
                _tick(tickedAt);
            }
            finally
            {
                var afterTick = _parent._clock.Elapsed;
                var interval = nextTickAt - afterTick;
                if (interval < s_minInterval)
                    // We are way overdue, but shouldn't cause starvation in other areas
                    interval = s_minInterval;
                _timer.Interval = interval;
            }
        }

        private static readonly TimeSpan s_minInterval = TimeSpan.FromMilliseconds(1);
        private TimeSpan Interval { get; }

        public void Dispose() => _timer.Stop();
    }
    
    /// <inheritdoc />
    protected override IDisposable StartCore(Action<TimeSpan> tick)
    {
        return new TimerInstance(this, tick);
    }
}
