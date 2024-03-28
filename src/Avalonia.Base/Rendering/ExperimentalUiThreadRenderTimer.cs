using System;
using System.Diagnostics;
using Avalonia.Metadata;
using Avalonia.Threading;

namespace Avalonia.Rendering;

internal class ExperimentalUiThreadRenderTimer : DefaultRenderTimer
{
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    public ExperimentalUiThreadRenderTimer(int framesPerSecond) : base(framesPerSecond)
    {
    }

    public override bool RunsInBackground => false;
    

    class TimerInstance : IDisposable
    {
        private ExperimentalUiThreadRenderTimer _parent;
        private readonly Action<TimeSpan> _tick;
        private DispatcherTimer _timer = new DispatcherTimer(DispatcherPriority.Render);

        public TimerInstance(ExperimentalUiThreadRenderTimer parent, Action<TimeSpan> tick)
        {
            _parent = parent;
            _tick = tick;
            _timer.Tick += OnTick;
            _timer.Interval = Interval;
            _timer.Start();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            var tickedAt = _parent._clock.Elapsed;
            var nextTickAt = tickedAt + Interval;
            try
            {
                _tick?.Invoke(tickedAt);
            }
            finally
            {
                var afterTick = _parent._clock.Elapsed;
                var interval = nextTickAt - afterTick;
                if (interval < MinInterval)
                    // We are way overdue, but shouldn't cause starvation in other areas
                    interval = MinInterval;
                _timer.Interval = interval;
            }
        }

        private TimeSpan MinInterval = TimeSpan.FromMilliseconds(1);
        private TimeSpan Interval => TimeSpan.FromSeconds(1.0 / _parent.FramesPerSecond);

        public void Dispose() => _timer.Stop();
    }
    
    protected override IDisposable StartCore(Action<TimeSpan> tick)
    {
        return new TimerInstance(this, tick);
    }
}