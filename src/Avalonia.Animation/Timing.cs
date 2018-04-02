// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.Threading;

namespace Avalonia.Animation
{
    /// <summary>
    /// Provides global timing functions for animations.
    /// </summary>
    public static class Timing
    {
        /// <summary>
        /// The number of frames per second.
        /// </summary>
        public const int FramesPerSecond = 60;

        /// <summary>
        /// The time span of each frame.
        /// </summary>
        private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1.0 / FramesPerSecond);

        static ulong _frameCount = 0;

        /// <summary>
        /// Initializes static members of the <see cref="Timing"/> class.
        /// </summary>
        static Timing()
        {
            Stopwatch = new Stopwatch();
            Stopwatch.Start();
            Timer = Observable.Interval(Tick, AvaloniaScheduler.Instance)
                .Select(_ =>
                {
                    _frameCount += 1;
                    return _frameCount;
                })
                .Publish()
                .RefCount();
        }

        /// <summary>
        /// The stopwatch used to track time.
        /// </summary>
        /// <value>
        /// The stopwatch used to track time.
        /// </value>
        public static Stopwatch Stopwatch
        {
            get;
        }

        /// <summary>
        /// Gets the animation timer.
        /// </summary>
        /// <remarks>
        /// The animation timer ticks <see cref="FramesPerSecond"/> times per second. The
        /// parameter passed to a subsciber is the number of frames since the animation system was
        /// initialized.
        /// </remarks>
        /// <value>
        /// The animation timer.
        /// </value>
        public static IObservable<ulong> Timer
        {
            get;
        }


        /// <summary>
        /// Gets a timer that fires every frame for the specified duration with delay.
        /// </summary>
        /// <returns>
        /// An observable that notifies the subscriber of the progress along the animation.
        /// </returns>
        /// <remarks>
        /// The parameter passed to the subscriber is the progress along the animation, with
        /// 0 being the start and 1 being the end. The observable is guaranteed to fire 0
        /// immediately on subscribe and 1 at the end of the duration.
        /// </remarks>
        public static IObservable<double> GetTimer(Animatable control, TimeSpan duration, TimeSpan delay)
        {
            var startTime = _frameCount;
            var _duration = (ulong)(duration.Ticks/Tick.Ticks);
            var endTime = startTime + _duration;
            return Timer
                .TakeWhile(x => x < endTime)
                .Select(x => (double)(x - startTime) / _duration)
                .StartWith(0.0)
                .Concat(Observable.Return(1.0));
        }

    }
}
