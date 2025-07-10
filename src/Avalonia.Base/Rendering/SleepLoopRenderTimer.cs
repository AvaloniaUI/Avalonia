using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Metadata;

namespace Avalonia.Rendering
{
    [PrivateApi]
    public class SleepLoopRenderTimer : IRenderTimer
    {
        private Action<TimeSpan>? _tick;
        private int _count;
        private readonly object _lock = new object();
        private bool _running;
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
                if (value < 1)
                    throw new ArgumentOutOfRangeException();
                _desiredFps = value;
            }
        }

        public event Action<TimeSpan> Tick
        {
            add
            {
                lock (_lock)
                {
                    _tick += value;
                    _count++;
                    if (_running)
                        return;
                    _running = true;
                    new Thread(LoopProc) { IsBackground = true }.Start();
                }

            }
            remove
            {
                lock (_lock)
                {
                    _tick -= value;
                    _count--;
                }
            }
        }

        public bool RunsInBackground => true;

        void LoopProc()
        {
            var lastTick = _st.Elapsed;
            while (true)
            {
                var now = _st.Elapsed;
                var tickInterval = TimeSpan.FromSeconds(1d / _desiredFps);
                var timeTillNextTick = lastTick + tickInterval - now;
                if (timeTillNextTick.TotalMilliseconds > 1) Thread.Sleep(timeTillNextTick);
                lastTick = now = _st.Elapsed;
                lock (_lock)
                {
                    if (_count == 0)
                    {
                        _running = false;
                        return;
                    }
                }

                _tick?.Invoke(now);
                
            }
        }


    }
}
