using System;
using System.Diagnostics;
using System.Threading;

namespace Avalonia.Rendering
{
    public class SleepLoopRenderTimer : IRenderTimer
    {
        public event Action<TimeSpan> Tick;

        public SleepLoopRenderTimer(int fps)
        {
            var timeBetweenTicks = TimeSpan.FromSeconds(1d / fps);
            new Thread(() =>
            {
                var st = Stopwatch.StartNew();
                var now = st.Elapsed;
                var lastTick = now;

                while (true)
                {
                    var timeTillNextTick = lastTick + timeBetweenTicks - now;
                    if (timeTillNextTick.TotalMilliseconds > 1)
                        Thread.Sleep(timeTillNextTick);


                    Tick?.Invoke(now);
                    now = st.Elapsed;
                }
            }) { IsBackground = true }.Start();
        }
    }
}
