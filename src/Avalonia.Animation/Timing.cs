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
        internal static readonly TimeSpan FrameTick = TimeSpan.FromSeconds(1.0 / FramesPerSecond);

        /// <summary>
        /// Initializes static members of the <see cref="Timing"/> class.
        /// </summary>
        static Timing()
        { 
            var globalTimer = Observable.Interval(FrameTick, AvaloniaScheduler.Instance);

            AnimationsTimer = globalTimer
                .Select(_ => GetTickCount())
                .Publish()
                .RefCount();
        }

        internal static TimeSpan GetTickCount() => TimeSpan.FromMilliseconds(Environment.TickCount);

        /// <summary>
        /// Gets the animation timer.
        /// </summary>
        /// <remarks>
        /// The animation timer triggers usually at 60 times per second or as
        /// defined in <see cref="FramesPerSecond"/>.
        /// The parameter passed to a subsciber is the current playstate of the animation.
        /// </remarks>
        internal static IObservable<TimeSpan> AnimationsTimer
        {
            get;
        }
    }
}