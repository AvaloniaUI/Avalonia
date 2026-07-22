using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Metadata;

namespace Avalonia.Rendering
{
    [PrivateApi]
    public class SleepLoopRenderTimer : IRenderTimer
    {
        private volatile Action<TimeSpan>? _tick;
        private volatile bool _stopped = true;
        private bool _threadStarted;
        private readonly AutoResetEvent _wakeEvent = new(false);
        private readonly Stopwatch _st = Stopwatch.StartNew();
        private volatile int _desiredFps;

        public SleepLoopRenderTimer(int fps)
        {
            DesiredFps = fps;
        }

        public int DesiredFps
        {
            get => _desiredFps;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
                _desiredFps = value;
            }
        }

        public Action<TimeSpan>? Tick
        {
            get => _tick;
            set
            {
                if (value != null)
                {
                    _tick = value;
                    _stopped = false;
                    if (!_threadStarted)
                    {
                        _threadStarted = true;
                        new Thread(LoopProc) { IsBackground = true }.Start();
                    }
                    else
                    {
                        _wakeEvent.Set();
                    }
                }
                else
                {
                    _stopped = true;
                    _tick = null;
                }
            }
        }

        public bool RunsInBackground => true;

        void LoopProc()
        {
            var lastTick = _st.Elapsed;
            while (true)
            {
                if (_stopped)
                    _wakeEvent.WaitOne();

                var now = _st.Elapsed;
                var tickInterval = TimeSpan.FromSeconds(1d / _desiredFps);
                var timeTillNextTick = lastTick + tickInterval - now;
                if (timeTillNextTick.TotalMilliseconds > 1)
                    _wakeEvent.WaitOne(timeTillNextTick);
                lastTick = now = _st.Elapsed;

                _tick?.Invoke(now);
            }
        }
    }
}
