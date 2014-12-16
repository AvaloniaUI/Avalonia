// -----------------------------------------------------------------------
// <copyright file="Animate.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Animation
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Threading;

    public static class Animate
    {
        private const int FramesPerSecond = 30;

        private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1.0 / FramesPerSecond);

        public static IDisposable Property(
            PerspexObject target,
            PerspexProperty property,
            object start,
            object finish,
            IEasing easing,
            TimeSpan duration,
            double repeats = 1)
        {
            var startTime = Environment.TickCount;
            var runningTime = (double)duration.TotalMilliseconds;
            var endTime = Environment.TickCount + (runningTime * repeats);

            var o = Observable.Interval(Tick, PerspexScheduler.Instance)
                .Select(_ => Environment.TickCount)
                .TakeWhile(tick => tick < endTime)
                .Select(tick => easing.Ease((tick - startTime) / runningTime, start, finish))
                .StartWith(start)
                .Concat(Observable.Return(finish));

            return target.Bind(property, o, BindingPriority.Animation);
        }

        public static IDisposable Property<T>(
            PerspexObject target,
            PerspexProperty<T> property, 
            T start, 
            T finish,
            IEasing easing,
            TimeSpan duration)
        {
            return Property(target, (PerspexProperty)property, start, finish, easing, duration);
        }
    }
}
