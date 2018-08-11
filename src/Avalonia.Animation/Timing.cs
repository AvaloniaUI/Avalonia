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
        static long _tickStartTimeStamp;
        static long TicksPerFrame = Stopwatch.Frequency / FramesPerSecond;

        /// <summary>
        /// Gets or sets the animation play state for all animations
        /// </summary> 
        public static PlayState GlobalPlayState { get; set; } = PlayState.Run;

        /// <summary>
        /// The number of frames per second.
        /// </summary>
        public const int FramesPerSecond = 60;

        /// <summary>
        /// The time span of each frame.
        /// </summary>
        internal static readonly TimeSpan FrameTick = TimeSpan.FromSeconds(1.0 / FramesPerSecond);

        /// <summary>
        /// Initializes static members of the <see cref="Timing"/> class.
        /// </summary>
        static Timing()
        {
            _tickStartTimeStamp = Stopwatch.GetTimestamp();

            var globalTimer = Observable.Interval(FrameTick, AvaloniaScheduler.Instance);

            AnimationsTimer = globalTimer
                .Select(_ =>
                {
                    return (Stopwatch.GetTimestamp() - _tickStartTimeStamp)
                      / TicksPerFrame * 2;
                })
                .Publish()
                .RefCount();
        }


        /// <summary>
        /// Gets the animation timer.
        /// </summary>
        /// <remarks>
        /// The animation timer triggers usually at 60 times per second or as
        /// defined in <see cref="FramesPerSecond"/>.
        /// The parameter passed to a subsciber is the current playstate of the animation.
        /// </remarks>
        internal static IObservable<long> AnimationsTimer
        {
            get;
        }

        /// <summary>
        /// Gets a timer that fires every frame for the specified duration with delay.
        /// </summary>
        /// <returns>
        /// An observable that notifies the subscriber of the progress along the transition.
        /// </returns>
        /// <remarks>
        /// The parameter passed to the subscriber is the progress along the transition, with
        /// 0 being the start and 1 being the end. The observable is guaranteed to fire 0
        /// immediately on subscribe and 1 at the end of the duration.
        /// </remarks>
        public static IObservable<double> GetTransitionsTimer(Animatable control, TimeSpan duration, TimeSpan delay = default(TimeSpan))
        {
            // TODO: Fix this mess.
            var _duration = (duration.Ticks / FrameTick.Ticks);
            long? endTime = ((Stopwatch.GetTimestamp() - _tickStartTimeStamp)
                      / TicksPerFrame) + _duration;

            return AnimationsTimer
                .TakeWhile(x => x < endTime)
                .Select(x => (double)x / _duration)
                .StartWith(0.0)
                .Concat(Observable.Return(1.0));
        }
    }
}