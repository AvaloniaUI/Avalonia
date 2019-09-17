using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.FreeDesktop;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.X11.Glx;
using Avalonia.X11.NativeDialogs;
using static Avalonia.X11.XLib;

namespace Avalonia.X11
{
    internal class LinuxHighResolutionTimer : IDisposable
    {
        [DllImport("libc", SetLastError = true)]
        static extern int nanosleep(ref TimeSpec duration, ref TimeSpec remaining);

        [DllImport("libc", SetLastError = true)]
        static extern int clock_gettime(uint clk_id, ref TimeSpec tp);

        const uint CLOCK_MONOTONIC = 1;

        struct TimeSpec
        {
            public long tv_sec;
            public long tv_nsec;

            public TimeSpec(int tv_sec, long tv_nsec) : this()
            {
                this.tv_sec = tv_sec;
                this.tv_nsec = tv_nsec;
            }

            public TimeSpan ToTimeSpan()
                => TimeSpan.FromSeconds(tv_sec) + TimeSpan.FromTicks(tv_nsec / 100);

            public static TimeSpec ConvertToTimeSpec(TimeSpan timeSpan)
                => new TimeSpec(timeSpan.Seconds, timeSpan.Ticks * 100);
        };

        TimeSpec GetElapsedTimeSpec(ref TimeSpec start, ref TimeSpec stop)
        {
            var elapsed_time = new TimeSpec();
            if ((stop.tv_nsec - start.tv_nsec) < 0)
            {
                elapsed_time.tv_sec = stop.tv_sec - start.tv_sec - 1;
                elapsed_time.tv_nsec = stop.tv_nsec - start.tv_nsec + 1000000000;
            }
            else
            {
                elapsed_time.tv_sec = stop.tv_sec - start.tv_sec;
                elapsed_time.tv_nsec = stop.tv_nsec - start.tv_nsec;
            }
            return elapsed_time;
        }

        public LinuxHighResolutionTimer(double seconds)
        {
            totalInterval = TimeSpan.FromSeconds(seconds);
        }

        private volatile bool shouldStop = false;
        private TimeSpan elapsedTotal = TimeSpan.Zero;
        private IDisposable _diposable1;
        private TimeSpan totalInterval;

        private void TickTock(Action<TimeSpan> observer)
        {
            var start = new TimeSpec();
            var frameStop = new TimeSpec();
            var sleepStop = new TimeSpec();
            var remaining = new TimeSpec();

            TimeSpan frameTime, totalTime;

            while (!shouldStop)
            {
                clock_gettime(CLOCK_MONOTONIC, ref start);

                observer(elapsedTotal);

                clock_gettime(CLOCK_MONOTONIC, ref frameStop);

                frameTime = GetElapsedTimeSpec(ref start, ref frameStop).ToTimeSpan();

                var calc = (totalInterval - frameTime);

                if (calc < TimeSpan.Zero)
                    calc = totalInterval;

                var finDur = TimeSpec.ConvertToTimeSpec(calc);

                nanosleep(ref finDur, ref remaining);

                clock_gettime(CLOCK_MONOTONIC, ref sleepStop);

                totalTime = GetElapsedTimeSpec(ref start, ref sleepStop).ToTimeSpan();

                elapsedTotal += totalTime;
            }
        }

        public void Dispose()
        {
            shouldStop = true;
            _diposable1?.Dispose();
        }

        public IDisposable Subscribe(Action<TimeSpan> observer)
        {
            _diposable1 = Task.Run(() => TickTock(observer));
            return this;
        }
    }


    internal class X11HighResRenderTimer : DefaultRenderTimer
    {
        private double intervalMills;

        public X11HighResRenderTimer(int framesPerSecond) : base(framesPerSecond)
        {
            this.intervalMills = 1d / (double)framesPerSecond;
        }


        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        protected override IDisposable StartCore(Action<TimeSpan> tick)
        {
            return new LinuxHighResolutionTimer(intervalMills).Subscribe(tick);
        }
    }
}
