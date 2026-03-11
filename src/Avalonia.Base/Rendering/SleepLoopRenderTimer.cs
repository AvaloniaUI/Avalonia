using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Metadata;

namespace Avalonia.Rendering
{
    [PrivateApi]
    public class SleepLoopRenderTimer : IRenderTimer
    {
        private volatile bool _stopped = true;
        private bool _threadStarted;
        private readonly AutoResetEvent _wakeEvent = new(false);
        private readonly Stopwatch _st = Stopwatch.StartNew();
        private readonly TimeSpan _timeBetweenTicks;

        public SleepLoopRenderTimer(int fps)
        {
            _timeBetweenTicks = TimeSpan.FromSeconds(1d / fps);
        }

        public Action<TimeSpan>? Tick { get; set; }

        public bool RunsInBackground => true;

        public void Start()
        {
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

        public void Stop()
        {
            _stopped = true;
        }

        void LoopProc()
        {
            var lastTick = _st.Elapsed;
            while (true)
            {
                if (_stopped)
                    _wakeEvent.WaitOne();

                var now = _st.Elapsed;
                var timeTillNextTick = lastTick + _timeBetweenTicks - now;
                if (timeTillNextTick.TotalMilliseconds > 1)
                    _wakeEvent.WaitOne(timeTillNextTick);
                lastTick = now = _st.Elapsed;

                if (_stopped)
                    continue;

                Tick?.Invoke(now);
            }
        }
    }
}
